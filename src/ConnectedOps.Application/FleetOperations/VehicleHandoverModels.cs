using ConnectedOps.Domain.FleetOperations;
using ConnectedOps.Domain.Vehicles;

namespace ConnectedOps.Application.FleetOperations;

public sealed record VehicleHandoverDto(
    Guid Id,
    Guid TenantId,
    Guid VehicleId,
    string VehicleNumber,
    string VehicleDisplayName,
    Guid? FromDriverId,
    string? FromDriverName,
    Guid ToDriverId,
    string ToDriverName,
    Guid? FromUsageSessionId,
    Guid? ToUsageSessionId,
    DateTime HandoverAtUtc,
    decimal Odometer,
    OdometerUnit OdometerUnit,
    Guid? LocationId,
    VehicleCondition Condition,
    string? Notes,
    bool AcknowledgedByFromDriver,
    bool AcknowledgedByToDriver,
    DateTime CreatedAtUtc);

public sealed record CreateVehicleHandoverRequest(
    Guid VehicleId,
    Guid ToDriverId,
    decimal Odometer,
    OdometerUnit OdometerUnit = OdometerUnit.Kilometers,
    Guid? FromDriverId = null,
    DateTime? HandoverAtUtc = null,
    Guid? LocationId = null,
    VehicleCondition Condition = VehicleCondition.Good,
    string? Notes = null,
    bool AcknowledgedByFromDriver = true,
    bool AcknowledgedByToDriver = true);
