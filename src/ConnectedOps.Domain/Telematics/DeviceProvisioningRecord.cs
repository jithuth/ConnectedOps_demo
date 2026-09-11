using ConnectedOps.Domain.Common;

namespace ConnectedOps.Domain.Telematics;

public sealed class DeviceProvisioningRecord : BaseEntity
{
    private DeviceProvisioningRecord()
    {
    }

    public DeviceProvisioningRecord(
        Guid tenantId,
        Guid trackingDeviceId,
        ProvisioningStatus provisioningStatus,
        Guid? provisionedByUserId = null,
        DateTime? provisionedAtUtc = null,
        string? providerReference = null,
        string? notes = null)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        if (trackingDeviceId == Guid.Empty)
            throw new ArgumentException("TrackingDeviceId is required.", nameof(trackingDeviceId));

        TenantId = tenantId;
        TrackingDeviceId = trackingDeviceId;
        ProvisioningStatus = provisioningStatus;
        ProvisionedByUserId = provisionedByUserId;
        ProvisionedAtUtc = provisionedAtUtc ?? DateTime.UtcNow;
        ProviderReference = providerReference?.Trim();
        Notes = notes?.Trim();
    }

    public Guid TenantId { get; private set; }
    public Guid TrackingDeviceId { get; private set; }
    public TrackingDevice TrackingDevice { get; private set; } = null!;
    public ProvisioningStatus ProvisioningStatus { get; private set; }
    public DateTime? ProvisionedAtUtc { get; private set; }
    public Guid? ProvisionedByUserId { get; private set; }
    public string? ProviderReference { get; private set; }
    public string? Notes { get; private set; }

    public void UpdateStatus(ProvisioningStatus status, string? notes = null, Guid? userId = null)
    {
        ProvisioningStatus = status;
        if (!string.IsNullOrWhiteSpace(notes))
        {
            Notes = notes.Trim();
        }
        MarkUpdated(userId);
    }
}
