namespace ConnectedOps.Application.Vehicles;

public sealed record VehicleCategoryDto(
    Guid Id,
    Guid TenantId,
    string Name,
    string Code,
    string? Description,
    bool IsMotorized,
    bool IsActive,
    int VehicleCount);

public sealed record CreateVehicleCategoryRequest(
    string Name,
    string Code,
    string? Description,
    bool IsMotorized);

public sealed record UpdateVehicleCategoryRequest(
    string Name,
    string? Description,
    bool IsMotorized);
