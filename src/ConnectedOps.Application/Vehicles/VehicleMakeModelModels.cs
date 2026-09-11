namespace ConnectedOps.Application.Vehicles;

public sealed record VehicleMakeDto(
    Guid Id,
    Guid TenantId,
    string Name,
    string? CountryCode,
    bool IsActive,
    int ModelCount,
    int VehicleCount);

public sealed record VehicleModelDto(
    Guid Id,
    Guid TenantId,
    Guid VehicleMakeId,
    string MakeName,
    string Name,
    Guid? DefaultCategoryId,
    string? DefaultCategoryName,
    bool IsActive,
    int VehicleCount);

public sealed record CreateVehicleMakeRequest(
    string Name,
    string? CountryCode);

public sealed record UpdateVehicleMakeRequest(
    string Name,
    string? CountryCode);

public sealed record CreateVehicleModelRequest(
    Guid VehicleMakeId,
    string Name,
    Guid? DefaultCategoryId);

public sealed record UpdateVehicleModelRequest(
    string Name,
    Guid? DefaultCategoryId);
