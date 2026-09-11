using ConnectedOps.Domain.Fuel;

namespace ConnectedOps.Application.Fuel;

public sealed record FuelStationDto(
    Guid Id,
    Guid TenantId,
    string Code,
    string Name,
    string? VendorName,
    FuelStationType StationType,
    string StationTypeName,
    string? Address,
    double? Latitude,
    double? Longitude,
    Guid? BranchId,
    string? BranchName,
    string? ContactPhone,
    string? Notes,
    bool IsActive,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);

public sealed record FuelStationListItemDto(
    Guid Id,
    string Code,
    string Name,
    string? VendorName,
    FuelStationType StationType,
    string StationTypeName,
    string? Address,
    Guid? BranchId,
    string? BranchName,
    string? ContactPhone,
    bool IsActive,
    DateTime CreatedAtUtc);

public sealed record CreateFuelStationRequest(
    string Code,
    string Name,
    string? VendorName = null,
    FuelStationType StationType = FuelStationType.Internal,
    string? Address = null,
    double? Latitude = null,
    double? Longitude = null,
    Guid? BranchId = null,
    string? ContactPhone = null,
    string? Notes = null,
    bool IsActive = true);

public sealed record UpdateFuelStationRequest(
    string Code,
    string Name,
    string? VendorName,
    FuelStationType StationType,
    string? Address,
    double? Latitude,
    double? Longitude,
    Guid? BranchId,
    string? ContactPhone,
    string? Notes,
    bool IsActive);

public sealed record FuelStationQueryParameters
{
    public FuelStationType? StationType { get; init; }
    public Guid? BranchId { get; init; }
    public bool? ActiveOnly { get; init; }
    public string? SearchTerm { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}
