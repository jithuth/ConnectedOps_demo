using ConnectedOps.Domain.Fuel;
using ConnectedOps.Domain.Vehicles;

namespace ConnectedOps.Application.Fuel;

public sealed record FuelTypeDefinitionDto(
    Guid Id,
    Guid? TenantId,
    string Code,
    string Name,
    FuelType FuelType,
    string FuelTypeName,
    string EnergyType,
    FuelUnit DefaultUnit,
    string DefaultUnitName,
    decimal? Density,
    bool IsActive,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);

public sealed record CreateFuelTypeDefinitionRequest(
    string Code,
    string Name,
    FuelType FuelType,
    string EnergyType = "Liquid",
    FuelUnit DefaultUnit = FuelUnit.Liter,
    decimal? Density = null,
    bool IsActive = true);

public sealed record UpdateFuelTypeDefinitionRequest(
    string Code,
    string Name,
    FuelType FuelType,
    string EnergyType,
    FuelUnit DefaultUnit,
    decimal? Density,
    bool IsActive);
