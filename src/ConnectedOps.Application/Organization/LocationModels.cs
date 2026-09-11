using ConnectedOps.Domain.Organization;

namespace ConnectedOps.Application.Organization;

public sealed record LocationListItemDto(
    Guid Id,
    Guid BranchId,
    string BranchName,
    string Name,
    string Code,
    LocationType Type,
    string TypeName,
    double? Latitude,
    double? Longitude,
    string? City,
    string? CountryCode,
    bool IsActive);

public sealed record LocationDto(
    Guid Id,
    Guid TenantId,
    Guid BranchId,
    string BranchName,
    string Name,
    string Code,
    LocationType Type,
    string TypeName,
    double? Latitude,
    double? Longitude,
    double? GeofenceRadiusMeters,
    string? AddressLine1,
    string? AddressLine2,
    string? City,
    string? StateOrProvince,
    string? PostalCode,
    string? CountryCode,
    bool IsActive,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);

public sealed record CreateLocationRequest(
    Guid BranchId,
    string Name,
    string Code,
    LocationType Type,
    double? Latitude,
    double? Longitude,
    double? GeofenceRadiusMeters,
    string? AddressLine1,
    string? AddressLine2,
    string? City,
    string? StateOrProvince,
    string? PostalCode,
    string? CountryCode);

public sealed record UpdateLocationRequest(
    Guid BranchId,
    string Name,
    string Code,
    LocationType Type,
    double? Latitude,
    double? Longitude,
    double? GeofenceRadiusMeters,
    string? AddressLine1,
    string? AddressLine2,
    string? City,
    string? StateOrProvince,
    string? PostalCode,
    string? CountryCode);
