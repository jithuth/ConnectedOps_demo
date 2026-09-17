using ConnectedOps.Domain.Common;
using ConnectedOps.Domain.Drivers;
using ConnectedOps.Domain.Vehicles;

namespace ConnectedOps.Domain.Hos;

public sealed class HosLogEntry : BaseEntity
{
    private HosLogEntry()
    {
    }

    public HosLogEntry(
        Guid tenantId,
        Guid driverId,
        DutyStatus status,
        DateTime startedAtUtc,
        Guid? vehicleId = null,
        decimal? startOdometer = null,
        string? locationName = null,
        double? latitude = null,
        double? longitude = null,
        string? notes = null,
        Guid? createdByUserId = null)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        if (driverId == Guid.Empty)
            throw new ArgumentException("DriverId is required.", nameof(driverId));

        TenantId = tenantId;
        DriverId = driverId;
        Status = status;
        StartedAtUtc = startedAtUtc;
        VehicleId = vehicleId;
        StartOdometer = startOdometer;
        LocationName = locationName?.Trim();
        Latitude = latitude;
        Longitude = longitude;
        Notes = notes?.Trim();
        CreatedBy = createdByUserId;
    }

    public Guid TenantId { get; private set; }

    public Guid DriverId { get; private set; }
    public Driver Driver { get; set; } = null!;

    public Guid? VehicleId { get; private set; }
    public Vehicle? Vehicle { get; set; }

    public DutyStatus Status { get; private set; }
    public DateTime StartedAtUtc { get; private set; }
    public DateTime? EndedAtUtc { get; private set; }

    public decimal? StartOdometer { get; private set; }
    public decimal? EndOdometer { get; private set; }

    public string? LocationName { get; private set; }
    public double? Latitude { get; private set; }
    public double? Longitude { get; private set; }
    public string? Notes { get; private set; }

    public bool IsCertified { get; private set; }
    public DateTime? CertifiedAtUtc { get; private set; }

    public double DurationHours => EndedAtUtc.HasValue
        ? Math.Max(0, (EndedAtUtc.Value - StartedAtUtc).TotalHours)
        : Math.Max(0, (DateTime.UtcNow - StartedAtUtc).TotalHours);

    public void EndLog(DateTime endedAtUtc, decimal? endOdometer = null, Guid? updatedBy = null)
    {
        EndedAtUtc = endedAtUtc;
        if (endOdometer.HasValue)
            EndOdometer = endOdometer;
        MarkUpdated(updatedBy);
    }

    public void Certify(Guid? userId = null)
    {
        IsCertified = true;
        CertifiedAtUtc = DateTime.UtcNow;
        MarkUpdated(userId);
    }
}
