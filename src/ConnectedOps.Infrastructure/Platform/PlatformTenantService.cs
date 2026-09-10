using ConnectedOps.Application.Platform;
using ConnectedOps.Domain.Tenancy;
using ConnectedOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ConnectedOps.Infrastructure.Platform;

public sealed class PlatformTenantService : IPlatformTenantService
{
    private readonly ConnectedOpsDbContext _dbContext;

    public PlatformTenantService(ConnectedOpsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PlatformTenantPage> GetTenantsAsync(
        PlatformTenantQuery query,
        CancellationToken cancellationToken = default)
    {
        var page = Math.Max(query.Page, 1);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);

        var dbQuery = _dbContext.Tenants
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            dbQuery = dbQuery.Where(t =>
                t.Name.Contains(search) ||
                t.Code.Contains(search) ||
                (t.Email != null && t.Email.Contains(search)));
        }

        if (query.Status.HasValue)
        {
            dbQuery = dbQuery.Where(t => t.Status == query.Status.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Country))
        {
            var country = query.Country.Trim().ToUpperInvariant();
            dbQuery = dbQuery.Where(t => t.CountryCode == country);
        }

        var totalCount = await dbQuery.CountAsync(cancellationToken);

        dbQuery = query.SortBy?.ToLowerInvariant() switch
        {
            "name" => query.SortDescending ? dbQuery.OrderByDescending(t => t.Name) : dbQuery.OrderBy(t => t.Name),
            "code" => query.SortDescending ? dbQuery.OrderByDescending(t => t.Code) : dbQuery.OrderBy(t => t.Code),
            "status" => query.SortDescending ? dbQuery.OrderByDescending(t => t.Status) : dbQuery.OrderBy(t => t.Status),
            _ => query.SortDescending ? dbQuery.OrderBy(t => t.CreatedAtUtc) : dbQuery.OrderByDescending(t => t.CreatedAtUtc)
        };

        var items = await dbQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new PlatformTenantDto(
                t.Id,
                t.Name,
                t.Code,
                t.Email,
                t.Phone,
                t.CountryCode,
                t.Status,
                _dbContext.TenantUsers.Count(u => u.TenantId == t.Id && !u.IsDeleted),
                t.CreatedAtUtc,
                t.UpdatedAtUtc))
            .ToListAsync(cancellationToken);

        return new PlatformTenantPage(items, page, pageSize, totalCount);
    }

    public async Task<PlatformTenantDetailDto?> GetTenantByIdAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var tenant = await _dbContext.Tenants
            .AsNoTracking()
            .Include(t => t.Settings)
            .SingleOrDefaultAsync(t => t.Id == tenantId, cancellationToken);

        if (tenant is null)
            return null;

        var userCount = await _dbContext.TenantUsers
            .CountAsync(u => u.TenantId == tenantId && !u.IsDeleted, cancellationToken);

        PlatformTenantSettingsDto? settingsDto = null;
        if (tenant.Settings is not null)
        {
            settingsDto = new PlatformTenantSettingsDto(
                tenant.Settings.TimeZone,
                tenant.Settings.CurrencyCode,
                tenant.Settings.DistanceUnit,
                tenant.Settings.FuelUnit,
                tenant.Settings.DateFormat,
                tenant.Settings.TimeFormat,
                tenant.Settings.LanguageCode);
        }

        return new PlatformTenantDetailDto(
            tenant.Id,
            tenant.Name,
            tenant.Code,
            tenant.Email,
            tenant.Phone,
            tenant.CountryCode,
            tenant.Status,
            userCount,
            tenant.CreatedAtUtc,
            tenant.UpdatedAtUtc,
            settingsDto);
    }

    public async Task<PlatformTenantDto> CreateTenantAsync(
        CreatePlatformTenantRequest request,
        CancellationToken cancellationToken = default)
    {
        var normalizedCode = request.Code.Trim().ToUpperInvariant();

        var codeExists = await _dbContext.Tenants
            .AnyAsync(t => t.Code == normalizedCode, cancellationToken);

        if (codeExists)
        {
            throw new InvalidOperationException($"Tenant with code '{normalizedCode}' already exists.");
        }

        var tenant = new Tenant(request.Name, normalizedCode, request.Email);

        if (!string.IsNullOrWhiteSpace(request.Phone) || !string.IsNullOrWhiteSpace(request.CountryCode))
        {
            tenant.UpdateContact(request.Email, request.Phone, request.CountryCode);
        }

        _dbContext.Tenants.Add(tenant);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new PlatformTenantDto(
            tenant.Id,
            tenant.Name,
            tenant.Code,
            tenant.Email,
            tenant.Phone,
            tenant.CountryCode,
            tenant.Status,
            0,
            tenant.CreatedAtUtc,
            tenant.UpdatedAtUtc);
    }

    public async Task<PlatformTenantDto> UpdateTenantAsync(
        Guid tenantId,
        UpdatePlatformTenantRequest request,
        CancellationToken cancellationToken = default)
    {
        var tenant = await _dbContext.Tenants
            .SingleOrDefaultAsync(t => t.Id == tenantId, cancellationToken);

        if (tenant is null)
            throw new KeyNotFoundException($"Tenant {tenantId} not found.");

        tenant.SetName(request.Name);
        tenant.UpdateContact(request.Email, request.Phone, request.CountryCode);

        await _dbContext.SaveChangesAsync(cancellationToken);

        var userCount = await _dbContext.TenantUsers
            .CountAsync(u => u.TenantId == tenantId && !u.IsDeleted, cancellationToken);

        return new PlatformTenantDto(
            tenant.Id,
            tenant.Name,
            tenant.Code,
            tenant.Email,
            tenant.Phone,
            tenant.CountryCode,
            tenant.Status,
            userCount,
            tenant.CreatedAtUtc,
            tenant.UpdatedAtUtc);
    }

    public async Task ActivateTenantAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var tenant = await _dbContext.Tenants
            .SingleOrDefaultAsync(t => t.Id == tenantId, cancellationToken);

        if (tenant is null)
            throw new KeyNotFoundException($"Tenant {tenantId} not found.");

        tenant.Activate();
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task SuspendTenantAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var tenant = await _dbContext.Tenants
            .SingleOrDefaultAsync(t => t.Id == tenantId, cancellationToken);

        if (tenant is null)
            throw new KeyNotFoundException($"Tenant {tenantId} not found.");

        tenant.Suspend();
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DisableTenantAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var tenant = await _dbContext.Tenants
            .SingleOrDefaultAsync(t => t.Id == tenantId, cancellationToken);

        if (tenant is null)
            throw new KeyNotFoundException($"Tenant {tenantId} not found.");

        tenant.Disable();
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<PlatformTenantUserDto>> GetTenantUsersAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var users = await _dbContext.TenantUsers
            .AsNoTracking()
            .Where(tu => tu.TenantId == tenantId && !tu.IsDeleted)
            .ToListAsync(cancellationToken);

        var tenantUserIds = users.Select(u => u.Id).ToList();
        var userIds = users.Select(u => u.UserId).Distinct().ToList();

        var appUsers = await _dbContext.Users
            .AsNoTracking()
            .Where(u => userIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, cancellationToken);

        var rolesByTenantUser = await _dbContext.TenantUserRoles
            .AsNoTracking()
            .Where(tur => !tur.IsDeleted && tenantUserIds.Contains(tur.TenantUserId))
            .Include(tur => tur.TenantRole)
            .Where(tur => tur.TenantRole != null && !tur.TenantRole.IsDeleted)
            .GroupBy(tur => tur.TenantUserId)
            .ToDictionaryAsync(
                g => g.Key,
                g => g.Select(tur => tur.TenantRole.Name).ToList(),
                cancellationToken);

        var result = new List<PlatformTenantUserDto>();

        foreach (var tu in users)
        {
            appUsers.TryGetValue(tu.UserId, out var appUser);
            rolesByTenantUser.TryGetValue(tu.Id, out var roles);

            result.Add(new PlatformTenantUserDto(
                tu.Id,
                tu.UserId,
                appUser?.Email ?? string.Empty,
                appUser?.FullName ?? string.Empty,
                tu.IsActive,
                tu.IsDefaultTenant,
                tu.CreatedAtUtc,
                roles ?? []));
        }

        return result;
    }

    public async Task<PlatformTenantStatsDto> GetTenantStatisticsAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var totalUsers = await _dbContext.TenantUsers
            .CountAsync(u => u.TenantId == tenantId && !u.IsDeleted, cancellationToken);

        var activeUsers = await _dbContext.TenantUsers
            .CountAsync(u => u.TenantId == tenantId && u.IsActive && !u.IsDeleted, cancellationToken);

        var roleCount = await _dbContext.TenantRoles
            .CountAsync(r => r.TenantId == tenantId && !r.IsDeleted, cancellationToken);

        var invitationCount = await _dbContext.TenantInvitations
            .CountAsync(i => i.TenantId == tenantId && !i.IsDeleted, cancellationToken);

        return new PlatformTenantStatsDto(
            tenantId,
            totalUsers,
            activeUsers,
            roleCount,
            invitationCount);
    }
}
