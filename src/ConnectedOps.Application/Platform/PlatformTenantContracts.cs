using ConnectedOps.Domain.Tenancy;

namespace ConnectedOps.Application.Platform;

public sealed record PlatformTenantDto(
    Guid Id,
    string Name,
    string Code,
    string? Email,
    string? Phone,
    string? CountryCode,
    TenantStatus Status,
    int UserCount,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);

public sealed record PlatformTenantDetailDto(
    Guid Id,
    string Name,
    string Code,
    string? Email,
    string? Phone,
    string? CountryCode,
    TenantStatus Status,
    int UserCount,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc,
    PlatformTenantSettingsDto? Settings);

public sealed record PlatformTenantSettingsDto(
    string TimeZone,
    string CurrencyCode,
    string DistanceUnit,
    string FuelUnit,
    string DateFormat,
    string TimeFormat,
    string LanguageCode);

public sealed record PlatformTenantUserDto(
    Guid TenantUserId,
    Guid UserId,
    string Email,
    string FullName,
    bool IsActive,
    bool IsDefaultTenant,
    DateTime JoinedAtUtc,
    IReadOnlyCollection<string> Roles);

public sealed record PlatformTenantStatsDto(
    Guid TenantId,
    int TotalUsers,
    int ActiveUsers,
    int RoleCount,
    int InvitationCount);

public sealed record PlatformTenantQuery(
    string? Search = null,
    TenantStatus? Status = null,
    string? Country = null,
    int Page = 1,
    int PageSize = 10,
    string? SortBy = null,
    bool SortDescending = false);

public sealed record PlatformTenantPage(
    IReadOnlyCollection<PlatformTenantDto> Items,
    int Page,
    int PageSize,
    int TotalCount)
{
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasNextPage => Page < TotalPages;
    public bool HasPreviousPage => Page > 1;
}

public sealed record CreatePlatformTenantRequest(
    string Name,
    string Code,
    string? Email = null,
    string? Phone = null,
    string? CountryCode = null);

public sealed record UpdatePlatformTenantRequest(
    string Name,
    string? Email = null,
    string? Phone = null,
    string? CountryCode = null);

public interface IPlatformTenantService
{
    Task<PlatformTenantPage> GetTenantsAsync(
        PlatformTenantQuery query,
        CancellationToken cancellationToken = default);

    Task<PlatformTenantDetailDto?> GetTenantByIdAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default);

    Task<PlatformTenantDto> CreateTenantAsync(
        CreatePlatformTenantRequest request,
        CancellationToken cancellationToken = default);

    Task<PlatformTenantDto> UpdateTenantAsync(
        Guid tenantId,
        UpdatePlatformTenantRequest request,
        CancellationToken cancellationToken = default);

    Task ActivateTenantAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default);

    Task SuspendTenantAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default);

    Task DisableTenantAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<PlatformTenantUserDto>> GetTenantUsersAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default);

    Task<PlatformTenantStatsDto> GetTenantStatisticsAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default);
}
