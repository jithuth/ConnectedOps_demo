using ConnectedOps.Domain.Common;

namespace ConnectedOps.Domain.Integrations;

public sealed class WebhookDeliveryAttempt : BaseEntity
{
    private WebhookDeliveryAttempt()
    {
    }

    public WebhookDeliveryAttempt(
        Guid tenantId,
        Guid subscriptionId,
        WebhookEventType eventType,
        string payloadJson,
        int responseStatusCode,
        long durationMs,
        bool success,
        string? errorMessage = null)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        if (subscriptionId == Guid.Empty)
            throw new ArgumentException("SubscriptionId is required.", nameof(subscriptionId));

        TenantId = tenantId;
        SubscriptionId = subscriptionId;
        EventType = eventType;
        PayloadJson = payloadJson ?? "{}";
        ResponseStatusCode = responseStatusCode;
        DurationMs = durationMs;
        Success = success;
        ErrorMessage = errorMessage?.Trim();
        AttemptedAtUtc = DateTime.UtcNow;
    }

    public Guid TenantId { get; private set; }
    public Guid SubscriptionId { get; private set; }
    public WebhookSubscription Subscription { get; set; } = null!;

    public WebhookEventType EventType { get; private set; }
    public string PayloadJson { get; private set; } = string.Empty;
    public int ResponseStatusCode { get; private set; }
    public long DurationMs { get; private set; }
    public bool Success { get; private set; }
    public string? ErrorMessage { get; private set; }
    public DateTime AttemptedAtUtc { get; private set; }
}
