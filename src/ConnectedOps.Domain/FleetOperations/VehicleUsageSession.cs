using ConnectedOps.Domain.Common;
using ConnectedOps.Domain.Drivers;
using ConnectedOps.Domain.Vehicles;

namespace ConnectedOps.Domain.FleetOperations;

public sealed class VehicleUsageSession : BaseEntity
{
    private VehicleUsageSession()
    {
    }

    public VehicleUsageSession(
        Guid tenantId,
        Guid vehicleId,
        Guid driverId,
        decimal startOdometer,
        OdometerUnit odometerUnit = OdometerUnit.Kilometers,
        DateTime? checkedOutAtUtc = null,
        Guid? checkedOutByUserId = null,
        Guid? startLocationId = null,
        Guid? driverVehicleAssignmentId = null,
        Guid? fleetShiftAssignmentId = null,
        VehicleCondition checkoutCondition = VehicleCondition.Good,
        string? purpose = null,
        string? reference = null,
        string? notes = null)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        if (vehicleId == Guid.Empty)
            throw new ArgumentException("VehicleId is required.", nameof(vehicleId));
        if (driverId == Guid.Empty)
            throw new ArgumentException("DriverId is required.", nameof(driverId));
        if (startOdometer < 0)
            throw new ArgumentException("Start odometer cannot be negative.", nameof(startOdometer));

        TenantId = tenantId;
        VehicleId = vehicleId;
        DriverId = driverId;
        StartOdometer = startOdometer;
        OdometerUnit = odometerUnit;
        CheckedOutAtUtc = checkedOutAtUtc ?? DateTime.UtcNow;
        CheckedOutByUserId = checkedOutByUserId;
        StartLocationId = startLocationId;
        DriverVehicleAssignmentId = driverVehicleAssignmentId;
        FleetShiftAssignmentId = fleetShiftAssignmentId;
        CheckoutCondition = checkoutCondition;
        Purpose = purpose?.Trim();
        Reference = reference?.Trim();
        Notes = notes?.Trim();
        Status = UsageSessionStatus.Open;
    }

    public Guid TenantId { get; private set; }
    public Guid VehicleId { get; private set; }
    public Vehicle Vehicle { get; private set; } = null!;
    public Guid DriverId { get; private set; }
    public Driver Driver { get; private set; } = null!;
    public Guid? DriverVehicleAssignmentId { get; private set; }
    public DriverVehicleAssignment? DriverVehicleAssignment { get; private set; }
    public Guid? FleetShiftAssignmentId { get; private set; }
    public FleetShiftAssignment? FleetShiftAssignment { get; private set; }
    public DateTime CheckedOutAtUtc { get; private set; }
    public Guid? CheckedOutByUserId { get; private set; }
    public decimal StartOdometer { get; private set; }
    public OdometerUnit OdometerUnit { get; private set; }
    public Guid? StartLocationId { get; private set; }
    public VehicleCondition CheckoutCondition { get; private set; }
    public string? Purpose { get; private set; }
    public string? Reference { get; private set; }
    public UsageSessionStatus Status { get; private set; }
    public DateTime? CheckedInAtUtc { get; private set; }
    public Guid? CheckedInByUserId { get; private set; }
    public decimal? EndOdometer { get; private set; }
    public decimal? DistanceTraveled => EndOdometer.HasValue ? Math.Max(0, EndOdometer.Value - StartOdometer) : null;
    public Guid? EndLocationId { get; private set; }
    public VehicleCondition? CheckInCondition { get; private set; }
    public string? Notes { get; private set; }

    public void CheckIn(
        decimal endOdometer,
        Guid? endLocationId,
        VehicleCondition condition,
        Guid? checkedInByUserId,
        string? notes,
        DateTime? checkedInAtUtc = null)
    {
        if (Status != UsageSessionStatus.Open)
            throw new InvalidOperationException($"Cannot check in session with status '{Status}'. Only Open sessions can be checked in.");

        if (endOdometer < StartOdometer)
            throw new ArgumentException($"End odometer ({endOdometer}) cannot be less than start odometer ({StartOdometer}).", nameof(endOdometer));

        EndOdometer = endOdometer;
        EndLocationId = endLocationId;
        CheckInCondition = condition;
        CheckedInByUserId = checkedInByUserId;
        CheckedInAtUtc = checkedInAtUtc ?? DateTime.UtcNow;
        Status = UsageSessionStatus.Completed;

        if (!string.IsNullOrWhiteSpace(notes))
        {
            Notes = string.IsNullOrWhiteSpace(Notes) ? notes.Trim() : $"{Notes} | CheckIn: {notes.Trim()}";
        }

        MarkUpdated();
    }

    public void Cancel(Guid? cancelledByUserId, string? reason)
    {
        if (Status != UsageSessionStatus.Open)
            throw new InvalidOperationException($"Cannot cancel session with status '{Status}'.");

        Status = UsageSessionStatus.Cancelled;
        CheckedInByUserId = cancelledByUserId;
        CheckedInAtUtc = DateTime.UtcNow;

        if (!string.IsNullOrWhiteSpace(reason))
        {
            Notes = string.IsNullOrWhiteSpace(Notes) ? $"Cancelled: {reason.Trim()}" : $"{Notes} | Cancelled: {reason.Trim()}";
        }

        MarkUpdated();
    }
}
