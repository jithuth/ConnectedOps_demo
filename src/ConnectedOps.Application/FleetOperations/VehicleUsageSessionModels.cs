using ConnectedOps.Domain.FleetOperations;
using ConnectedOps.Domain.Vehicles;

namespace ConnectedOps.Application.FleetOperations;

public sealed record VehicleUsageSessionDto(
    Guid Id,
    Guid TenantId,
    Guid VehicleId,
    string VehicleNumber,
    string VehicleDisplayName,
    Guid DriverId,
    string DriverNumber,
    string DriverDisplayName,
    Guid? DriverVehicleAssignmentId,
    Guid? FleetShiftAssignmentId,
    DateTime CheckedOutAtUtc,
    Guid? CheckedOutByUserId,
    decimal StartOdometer,
    OdometerUnit OdometerUnit,
    Guid? StartLocationId,
    string? Purpose,
    string? Reference,
    UsageSessionStatus Status,
    string StatusName,
    DateTime? CheckedInAtUtc,
    Guid? CheckedInByUserId,
    decimal? EndOdometer,
    decimal? DistanceTraveled,
    Guid? EndLocationId,
    VehicleCondition CheckoutCondition,
    VehicleCondition? CheckInCondition,
    string? Notes,
    DateTime CreatedAtUtc);

public sealed record CreateCheckoutRequest(
    Guid VehicleId,
    Guid DriverId,
    decimal StartOdometer,
    OdometerUnit OdometerUnit = OdometerUnit.Kilometers,
    DateTime? CheckedOutAtUtc = null,
    Guid? StartLocationId = null,
    Guid? DriverVehicleAssignmentId = null,
    Guid? FleetShiftAssignmentId = null,
    VehicleCondition CheckoutCondition = VehicleCondition.Good,
    string? Purpose = null,
    string? Reference = null,
    string? Notes = null);

public sealed record CheckInSessionRequest(
    decimal EndOdometer,
    Guid? EndLocationId = null,
    VehicleCondition Condition = VehicleCondition.Good,
    DateTime? CheckedInAtUtc = null,
    string? Notes = null);

public sealed class UsageSessionQueryParameters
{
    public Guid? VehicleId { get; set; }
    public Guid? DriverId { get; set; }
    public Guid? BranchId { get; set; }
    public UsageSessionStatus? Status { get; set; }
    public DateTime? FromUtc { get; set; }
    public DateTime? ToUtc { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
