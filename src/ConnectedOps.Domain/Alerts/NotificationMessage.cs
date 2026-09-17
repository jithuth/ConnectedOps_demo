using ConnectedOps.Domain.Common;

namespace ConnectedOps.Domain.Alerts;

public sealed class NotificationMessage : BaseEntity
{
    private NotificationMessage()
    {
    }

    public NotificationMessage(
        Guid tenantId,
        NotificationChannelType channel,
        string recipient,
        string title,
        string body,
        Guid? alertId = null,
        bool isDelivered = false,
        string? externalMessageId = null)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        if (string.IsNullOrWhiteSpace(recipient))
            throw new ArgumentException("Recipient is required.", nameof(recipient));
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title is required.", nameof(title));

        TenantId = tenantId;
        Channel = channel;
        Recipient = recipient.Trim();
        Title = title.Trim();
        Body = body?.Trim() ?? string.Empty;
        AlertId = alertId;
        IsDelivered = isDelivered;
        ExternalMessageId = externalMessageId?.Trim();
        SentAtUtc = DateTime.UtcNow;
    }

    public Guid TenantId { get; private set; }
    public Guid? AlertId { get; private set; }
    public Alert? Alert { get; set; }

    public NotificationChannelType Channel { get; private set; }
    public string Recipient { get; private set; } = string.Empty;
    public string Title { get; private set; } = string.Empty;
    public string Body { get; private set; } = string.Empty;
    public bool IsDelivered { get; private set; }
    public string? ExternalMessageId { get; private set; }
    public DateTime SentAtUtc { get; private set; }

    public void MarkDelivered(string? externalId = null)
    {
        IsDelivered = true;
        if (!string.IsNullOrWhiteSpace(externalId))
            ExternalMessageId = externalId.Trim();
        MarkUpdated();
    }
}
