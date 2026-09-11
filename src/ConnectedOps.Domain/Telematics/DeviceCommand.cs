using ConnectedOps.Domain.Common;

namespace ConnectedOps.Domain.Telematics;

public sealed class DeviceCommand : BaseEntity
{
    private DeviceCommand()
    {
    }

    public DeviceCommand(
        Guid tenantId,
        Guid trackingDeviceId,
        DeviceCommandType commandType,
        string? parametersJson = null,
        Guid? createdByUserId = null)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        if (trackingDeviceId == Guid.Empty)
            throw new ArgumentException("TrackingDeviceId is required.", nameof(trackingDeviceId));

        TenantId = tenantId;
        TrackingDeviceId = trackingDeviceId;
        CommandType = commandType;
        ParametersJson = parametersJson;
        Status = DeviceCommandStatus.Pending;
        CreatedBy = createdByUserId;
    }

    public Guid TenantId { get; private set; }
    public Guid TrackingDeviceId { get; private set; }
    public TrackingDevice TrackingDevice { get; private set; } = null!;

    public DeviceCommandType CommandType { get; private set; }
    public string? ParametersJson { get; private set; }
    public DeviceCommandStatus Status { get; private set; } = DeviceCommandStatus.Pending;

    public DateTime? SentAtUtc { get; private set; }
    public DateTime? AcknowledgedAtUtc { get; private set; }
    public DateTime? FailedAtUtc { get; private set; }

    public string? ResponsePayload { get; private set; }
    public string? FailureReason { get; private set; }

    public void MarkSent(Guid? userId = null)
    {
        Status = DeviceCommandStatus.Sent;
        SentAtUtc = DateTime.UtcNow;
        MarkUpdated(userId);
    }

    public void MarkAcknowledged(string? responsePayload = null, Guid? userId = null)
    {
        Status = DeviceCommandStatus.Acknowledged;
        AcknowledgedAtUtc = DateTime.UtcNow;
        ResponsePayload = responsePayload?.Trim();
        MarkUpdated(userId);
    }

    public void MarkFailed(string failureReason, Guid? userId = null)
    {
        Status = DeviceCommandStatus.Failed;
        FailedAtUtc = DateTime.UtcNow;
        FailureReason = failureReason.Trim();
        MarkUpdated(userId);
    }

    public void Cancel(Guid? userId = null)
    {
        if (Status == DeviceCommandStatus.Acknowledged || Status == DeviceCommandStatus.Failed)
            return;

        Status = DeviceCommandStatus.Cancelled;
        MarkUpdated(userId);
    }
}
