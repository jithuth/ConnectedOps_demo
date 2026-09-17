using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ConnectedOps.Application.Common.Interfaces;
using ConnectedOps.Application.Integrations;
using ConnectedOps.Application.Vehicles;
using ConnectedOps.Domain.Integrations;
using ConnectedOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ConnectedOps.Infrastructure.Integrations;

public sealed class WebhookService : IWebhookService
{
    private readonly ConnectedOpsDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly HttpClient _httpClient;
    private readonly ILogger<WebhookService> _logger;

    public WebhookService(
        ConnectedOpsDbContext dbContext,
        ICurrentUserContext currentUserContext,
        HttpClient httpClient,
        ILogger<WebhookService> logger)
    {
        _dbContext = dbContext;
        _currentUserContext = currentUserContext;
        _httpClient = httpClient;
        _logger = logger;
    }

    private Guid RequireTenantId()
    {
        return _currentUserContext.TenantId
            ?? throw new InvalidOperationException("Tenant context is required for webhook operations.");
    }

    public async Task<IReadOnlyCollection<WebhookSubscriptionDto>> GetSubscriptionsAsync(CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();

        var list = await _dbContext.WebhookSubscriptions
            .AsNoTracking()
            .Where(w => w.TenantId == tenantId)
            .OrderByDescending(w => w.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return list.Select(MapToDto).ToList();
    }

    public async Task<WebhookSubscriptionDto> CreateSubscriptionAsync(CreateWebhookRequest request, CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();

        var sub = new WebhookSubscription(
            tenantId,
            request.Url,
            request.Description,
            request.Events);

        _dbContext.WebhookSubscriptions.Add(sub);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created webhook subscription {Id} for URL {Url} with {Count} events", sub.Id, sub.Url, request.Events.Count);

        return MapToDto(sub);
    }

    public async Task<WebhookSubscriptionDto> UpdateSubscriptionAsync(Guid id, UpdateWebhookRequest request, CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();

        var sub = await _dbContext.WebhookSubscriptions
            .FirstOrDefaultAsync(w => w.TenantId == tenantId && w.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException($"Webhook subscription {id} was not found.");

        sub.UpdateUrl(request.Url, request.Description);
        sub.SetEvents(request.Events);

        await _dbContext.SaveChangesAsync(cancellationToken);
        return MapToDto(sub);
    }

    public async Task<bool> ToggleSubscriptionAsync(Guid id, bool isEnabled, CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();

        var sub = await _dbContext.WebhookSubscriptions
            .FirstOrDefaultAsync(w => w.TenantId == tenantId && w.Id == id, cancellationToken);

        if (sub == null) return false;

        if (isEnabled) sub.Enable();
        else sub.Disable();

        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteSubscriptionAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();

        var sub = await _dbContext.WebhookSubscriptions
            .FirstOrDefaultAsync(w => w.TenantId == tenantId && w.Id == id, cancellationToken);

        if (sub == null) return false;

        _dbContext.WebhookSubscriptions.Remove(sub);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<WebhookDeliveryAttemptDto> TriggerTestPingAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();

        var sub = await _dbContext.WebhookSubscriptions
            .FirstOrDefaultAsync(w => w.TenantId == tenantId && w.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException($"Webhook subscription {id} was not found.");

        var pingPayload = new
        {
            @event = "webhook.test_ping",
            timestamp = DateTime.UtcNow,
            subscription_id = sub.Id,
            message = "This is a test notification from ConnectedOps Integration Gateway."
        };

        var json = JsonSerializer.Serialize(pingPayload, new JsonSerializerOptions { WriteIndented = true });
        var (statusCode, durationMs, success, error) = await DeliverPayloadAsync(sub.Url, sub.SecretKey, "webhook.test_ping", json, cancellationToken);

        sub.RecordDeliveryResult(statusCode, success);

        var attempt = new WebhookDeliveryAttempt(
            tenantId,
            sub.Id,
            WebhookEventType.VehicleStatusChanged,
            json,
            statusCode,
            durationMs,
            success,
            error);

        _dbContext.WebhookDeliveryAttempts.Add(attempt);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return MapAttemptToDto(attempt);
    }

    public async Task<PagedResult<WebhookDeliveryAttemptDto>> GetDeliveryAttemptsPagedAsync(
        Guid? subscriptionId = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();

        var query = _dbContext.WebhookDeliveryAttempts
            .AsNoTracking()
            .Where(a => a.TenantId == tenantId);

        if (subscriptionId.HasValue)
        {
            query = query.Where(a => a.SubscriptionId == subscriptionId.Value);
        }

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(a => a.AttemptedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => MapAttemptToDto(a))
            .ToListAsync(cancellationToken);

        return new PagedResult<WebhookDeliveryAttemptDto>(items, total, page, pageSize);
    }

    public async Task DispatchEventAsync(WebhookEventType eventType, object payload, CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();

        var subscriptions = await _dbContext.WebhookSubscriptions
            .Where(w => w.TenantId == tenantId && w.IsEnabled)
            .ToListAsync(cancellationToken);

        var eventString = eventType.ToString();
        var matchingSubs = subscriptions.Where(s => s.GetEvents().Contains(eventType)).ToList();

        if (matchingSubs.Count == 0) return;

        var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = false });

        foreach (var sub in matchingSubs)
        {
            try
            {
                var (statusCode, durationMs, success, error) = await DeliverPayloadAsync(sub.Url, sub.SecretKey, eventString, json, cancellationToken);
                sub.RecordDeliveryResult(statusCode, success);

                _dbContext.WebhookDeliveryAttempts.Add(new WebhookDeliveryAttempt(
                    tenantId,
                    sub.Id,
                    eventType,
                    json,
                    statusCode,
                    durationMs,
                    success,
                    error));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed delivering webhook event {EventType} to {Url}", eventType, sub.Url);
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<(int StatusCode, long DurationMs, bool Success, string? Error)> DeliverPayloadAsync(
        string url,
        string secretKey,
        string eventType,
        string payloadJson,
        CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Content = new StringContent(payloadJson, Encoding.UTF8, "application/json");

            // Compute HMAC-SHA256 signature
            var signature = ComputeHmacSha256(secretKey, payloadJson);
            request.Headers.Add("X-ConnectedOps-Signature", "sha256=" + signature);
            request.Headers.Add("X-ConnectedOps-Event", eventType);
            request.Headers.Add("X-ConnectedOps-Delivery", Guid.NewGuid().ToString("N"));

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(8));

            using var response = await _httpClient.SendAsync(request, cts.Token);
            sw.Stop();

            var success = response.IsSuccessStatusCode;
            return ((int)response.StatusCode, sw.ElapsedMilliseconds, success, success ? null : $"HTTP {(int)response.StatusCode}: {response.ReasonPhrase}");
        }
        catch (Exception ex)
        {
            sw.Stop();
            return (0, sw.ElapsedMilliseconds, false, ex.Message);
        }
    }

    private static string ComputeHmacSha256(string secret, string payload)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static WebhookSubscriptionDto MapToDto(WebhookSubscription s)
    {
        var masked = s.SecretKey.Length > 10
            ? s.SecretKey[..6] + "..." + s.SecretKey[^4..]
            : "whsec_***";

        return new WebhookSubscriptionDto(
            s.Id,
            s.Url,
            s.Description,
            masked,
            s.GetEvents().ToList(),
            s.IsEnabled,
            s.FailureCount,
            s.LastTriggeredAtUtc,
            s.LastResponseStatusCode,
            s.CreatedAtUtc);
    }

    private static WebhookDeliveryAttemptDto MapAttemptToDto(WebhookDeliveryAttempt a)
    {
        return new WebhookDeliveryAttemptDto(
            a.Id,
            a.SubscriptionId,
            a.EventType,
            a.EventType.ToString(),
            a.ResponseStatusCode,
            a.DurationMs,
            a.Success,
            a.ErrorMessage,
            a.AttemptedAtUtc,
            a.PayloadJson);
    }
}
