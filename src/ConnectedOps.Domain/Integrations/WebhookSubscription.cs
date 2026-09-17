using System.Security.Cryptography;
using ConnectedOps.Domain.Common;

namespace ConnectedOps.Domain.Integrations;

public sealed class WebhookSubscription : BaseEntity
{
    private WebhookSubscription()
    {
    }

    public WebhookSubscription(
        Guid tenantId,
        string url,
        string description,
        IEnumerable<WebhookEventType> events,
        string? secretKey = null)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        if (string.IsNullOrWhiteSpace(url))
            throw new ArgumentException("Url is required.", nameof(url));
        if (!Uri.TryCreate(url, UriKind.Absolute, out _))
            throw new ArgumentException("Url must be a valid absolute URL.", nameof(url));

        TenantId = tenantId;
        Url = url.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? "Webhook Endpoint" : description.Trim();
        SecretKey = string.IsNullOrWhiteSpace(secretKey) ? GenerateSecret() : secretKey.Trim();
        IsEnabled = true;
        FailureCount = 0;

        SetEvents(events);
    }

    public Guid TenantId { get; private set; }
    public string Url { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public string SecretKey { get; private set; } = string.Empty;
    public string SubscribedEvents { get; private set; } = string.Empty;
    public bool IsEnabled { get; private set; }
    public int FailureCount { get; private set; }
    public DateTime? LastTriggeredAtUtc { get; private set; }
    public int? LastResponseStatusCode { get; private set; }

    public IReadOnlyCollection<WebhookEventType> GetEvents()
    {
        if (string.IsNullOrWhiteSpace(SubscribedEvents))
            return [];

        return SubscribedEvents
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(s => Enum.TryParse<WebhookEventType>(s, out var ev) ? ev : (WebhookEventType?)null)
            .Where(ev => ev.HasValue)
            .Select(ev => ev!.Value)
            .ToList();
    }

    public void SetEvents(IEnumerable<WebhookEventType> events)
    {
        var distinct = events.Distinct().ToList();
        SubscribedEvents = string.Join(",", distinct);
    }

    public void UpdateUrl(string url, string description)
    {
        if (string.IsNullOrWhiteSpace(url))
            throw new ArgumentException("Url is required.", nameof(url));
        if (!Uri.TryCreate(url, UriKind.Absolute, out _))
            throw new ArgumentException("Url must be a valid absolute URL.", nameof(url));

        Url = url.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? "Webhook Endpoint" : description.Trim();
    }

    public void Enable()
    {
        IsEnabled = true;
        FailureCount = 0;
    }

    public void Disable()
    {
        IsEnabled = false;
    }

    public void RecordDeliveryResult(int statusCode, bool success)
    {
        LastTriggeredAtUtc = DateTime.UtcNow;
        LastResponseStatusCode = statusCode;

        if (success)
        {
            FailureCount = 0;
        }
        else
        {
            FailureCount++;
            if (FailureCount >= 10)
            {
                IsEnabled = false; // Auto-disable faulty endpoints after 10 consecutive failures
            }
        }
    }

    private static string GenerateSecret()
    {
        var bytes = new byte[32];
        RandomNumberGenerator.Fill(bytes);
        return "whsec_" + Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
