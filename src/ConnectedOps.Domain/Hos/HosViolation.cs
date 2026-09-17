using ConnectedOps.Domain.Common;
using ConnectedOps.Domain.Drivers;

namespace ConnectedOps.Domain.Hos;

public sealed class HosViolation : BaseEntity
{
    private HosViolation()
    {
    }

    public HosViolation(
        Guid tenantId,
        Guid driverId,
        HosViolationType violationType,
        DateTime occurredAtUtc,
        int durationMinutes = 0,
        string? notes = null)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        if (driverId == Guid.Empty)
            throw new ArgumentException("DriverId is required.", nameof(driverId));

        TenantId = tenantId;
        DriverId = driverId;
        ViolationType = violationType;
        OccurredAtUtc = occurredAtUtc;
        DurationMinutes = Math.Max(0, durationMinutes);
        Notes = notes?.Trim();
    }

    public Guid TenantId { get; private set; }

    public Guid DriverId { get; private set; }
    public Driver Driver { get; set; } = null!;

    public HosViolationType ViolationType { get; private set; }
    public DateTime OccurredAtUtc { get; private set; }
    public int DurationMinutes { get; private set; }
    public string? Notes { get; private set; }

    public bool IsAcknowledged { get; private set; }
    public DateTime? AcknowledgedAtUtc { get; private set; }
    public Guid? AcknowledgedByUserId { get; private set; }

    public void Acknowledge(Guid userId)
    {
        IsAcknowledged = true;
        AcknowledgedAtUtc = DateTime.UtcNow;
        AcknowledgedByUserId = userId;
        MarkUpdated(userId);
    }
}
