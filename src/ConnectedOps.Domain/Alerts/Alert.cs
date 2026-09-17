using ConnectedOps.Domain.Common;
using ConnectedOps.Domain.Drivers;
using ConnectedOps.Domain.Vehicles;

namespace ConnectedOps.Domain.Alerts;

public sealed class Alert : BaseEntity
{
    private Alert()
    {
    }

    public Alert(
        Guid tenantId,
        string title,
        string message,
        AlertSeverity severity,
        AlertSourceType sourceType,
        Guid? alertRuleId = null,
        Guid? vehicleId = null,
        Guid? driverId = null,
        double? latitude = null,
        double? longitude = null,
        string? triggerDataJson = null,
        DateTime? triggeredAtUtc = null)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title is required.", nameof(title));

        TenantId = tenantId;
        Title = title.Trim();
        Message = message?.Trim() ?? string.Empty;
        Severity = severity;
        SourceType = sourceType;
        AlertRuleId = alertRuleId;
        VehicleId = vehicleId;
        DriverId = driverId;
        Latitude = latitude;
        Longitude = longitude;
        TriggerDataJson = triggerDataJson;
        TriggeredAtUtc = triggeredAtUtc ?? DateTime.UtcNow;
        Status = AlertStatus.Triggered;
    }

    public Guid TenantId { get; private set; }
    public Guid? AlertRuleId { get; private set; }
    public AlertRule? AlertRule { get; set; }

    public Guid? VehicleId { get; private set; }
    public Vehicle? Vehicle { get; set; }

    public Guid? DriverId { get; private set; }
    public Driver? Driver { get; set; }

    public string Title { get; private set; } = string.Empty;
    public string Message { get; private set; } = string.Empty;
    public AlertSeverity Severity { get; private set; }
    public AlertStatus Status { get; private set; }
    public AlertSourceType SourceType { get; private set; }

    public double? Latitude { get; private set; }
    public double? Longitude { get; private set; }
    public string? TriggerDataJson { get; private set; }

    public DateTime TriggeredAtUtc { get; private set; }
    public DateTime? AcknowledgedAtUtc { get; private set; }
    public Guid? AcknowledgedByUserId { get; private set; }
    public DateTime? ResolvedAtUtc { get; private set; }
    public Guid? ResolvedByUserId { get; private set; }
    public string? ResolutionNotes { get; private set; }

    public void Acknowledge(Guid userId)
    {
        if (Status != AlertStatus.Triggered) return;
        Status = AlertStatus.Acknowledged;
        AcknowledgedAtUtc = DateTime.UtcNow;
        AcknowledgedByUserId = userId;
        MarkUpdated(userId);
    }

    public void Resolve(Guid userId, string? notes = null)
    {
        Status = AlertStatus.Resolved;
        ResolvedAtUtc = DateTime.UtcNow;
        ResolvedByUserId = userId;
        ResolutionNotes = notes?.Trim();
        MarkUpdated(userId);
    }

    public void Dismiss(Guid userId, string? reason = null)
    {
        Status = AlertStatus.Dismissed;
        ResolvedAtUtc = DateTime.UtcNow;
        ResolvedByUserId = userId;
        ResolutionNotes = reason != null ? $"Dismissed: {reason.Trim()}" : "Dismissed";
        MarkUpdated(userId);
    }
}
