using System.Text;
using ConnectedOps.Application.Common.Interfaces;
using ConnectedOps.Application.TenantRoles;
using ConnectedOps.Domain.Authorization;
using ConnectedOps.Domain.Tenancy;
using ConnectedOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ConnectedOps.Infrastructure.TenantRoles;

public sealed class TenantRoleManagementService
    : ITenantRoleManagementService
{
    private readonly ConnectedOpsDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;

    public TenantRoleManagementService(
        ConnectedOpsDbContext dbContext,
        ICurrentUserContext currentUserContext)
    {
        _dbContext = dbContext;
        _currentUserContext = currentUserContext;
    }

    public async Task<IReadOnlyCollection<TenantRoleListItem>>
        GetRolesAsync(
            CancellationToken cancellationToken = default)
    {
        var tenantId = GetTenantId();

        var roles =
            await _dbContext.TenantRoles
                .AsNoTracking()
                .Where(x =>
                    x.TenantId == tenantId)
                .OrderByDescending(x =>
                    x.IsSystemRole)
                .ThenBy(x => x.Name)
                .Select(x =>
                    new TenantRoleListItem(
                        x.Id,
                        x.Name,
                        x.Code,
                        x.IsSystemRole,
                        x.Users.Count()))
                .ToListAsync(
                    cancellationToken);

        return roles;
    }

    public async Task<TenantRoleDetailsResult> GetRoleAsync(
        Guid roleId,
        CancellationToken cancellationToken = default)
    {
        ValidateRoleId(roleId);

        var tenantId = GetTenantId();

        var role =
            await _dbContext.TenantRoles
                .AsNoTracking()
                .Where(x =>
                    x.Id == roleId &&
                    x.TenantId == tenantId)
                .Select(x =>
                    new
                    {
                        x.Id,
                        x.TenantId,
                        x.Name,
                        x.Code,
                        x.IsSystemRole,

                        UserCount =
                            x.Users.Count(),

                        Permissions =
                            x.Permissions
                                .Select(p =>
                                    p.Permission.Key)
                                .Distinct()
                                .OrderBy(p => p)
                                .ToList()
                    })
                .SingleOrDefaultAsync(
                    cancellationToken);

        if (role is null)
        {
            throw new KeyNotFoundException(
                "Tenant role was not found.");
        }

        return new TenantRoleDetailsResult(
            role.Id,
            role.TenantId,
            role.Name,
            role.Code,
            role.IsSystemRole,
            role.UserCount,
            role.Permissions);
    }

    public async Task<CreateTenantRoleResult> CreateAsync(
        CreateTenantRoleRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var tenantId = GetTenantId();

        var name = NormalizeName(
            request.Name);

        var duplicateName =
            await _dbContext.TenantRoles
                .AnyAsync(
                    x =>
                        x.TenantId == tenantId &&
                        x.Name == name,
                    cancellationToken);

        if (duplicateName)
        {
            throw new InvalidOperationException(
                "A role with this name already exists.");
        }

        var code =
            await GenerateUniqueCodeAsync(
                tenantId,
                name,
                cancellationToken);

        var role =
            new TenantRole(
                tenantId,
                name,
                code,
                isSystemRole: false);

        _dbContext.TenantRoles.Add(role);

        await _dbContext.SaveChangesAsync(
            cancellationToken);

        return new CreateTenantRoleResult(
            role.Id,
            role.Name,
            role.Code,
            role.IsSystemRole);
    }

    public async Task UpdateAsync(
        Guid roleId,
        UpdateTenantRoleRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateRoleId(roleId);
        ArgumentNullException.ThrowIfNull(request);

        var tenantId = GetTenantId();

        var role =
            await _dbContext.TenantRoles
                .SingleOrDefaultAsync(
                    x =>
                        x.Id == roleId &&
                        x.TenantId == tenantId,
                    cancellationToken);

        if (role is null)
        {
            throw new KeyNotFoundException(
                "Tenant role was not found.");
        }

        if (role.IsSystemRole)
        {
            throw new InvalidOperationException(
                "System roles cannot be modified.");
        }

        var name =
            NormalizeName(request.Name);

        var duplicate =
            await _dbContext.TenantRoles
                .AnyAsync(
                    x =>
                        x.TenantId == tenantId &&
                        x.Id != roleId &&
                        x.Name == name,
                    cancellationToken);

        if (duplicate)
        {
            throw new InvalidOperationException(
                "A role with this name already exists.");
        }

        role.Rename(name);

        await _dbContext.SaveChangesAsync(
            cancellationToken);
    }

    public async Task DeleteAsync(
        Guid roleId,
        CancellationToken cancellationToken = default)
    {
        ValidateRoleId(roleId);

        var tenantId = GetTenantId();
        var currentUserId = GetUserId();

        var role =
            await _dbContext.TenantRoles
                .SingleOrDefaultAsync(
                    x =>
                        x.Id == roleId &&
                        x.TenantId == tenantId,
                    cancellationToken);

        if (role is null)
        {
            throw new KeyNotFoundException(
                "Tenant role was not found.");
        }

        if (role.IsSystemRole)
        {
            throw new InvalidOperationException(
                "System roles cannot be deleted.");
        }

        var assigned =
            await _dbContext.TenantUserRoles
                .AnyAsync(
                    x =>
                        x.TenantRoleId == roleId,
                    cancellationToken);

        if (assigned)
        {
            throw new InvalidOperationException(
                "This role is assigned to one or more users. Remove those assignments before deleting the role.");
        }

        var permissions =
            await _dbContext.RolePermissions
                .Where(x =>
                    x.TenantRoleId == roleId)
                .ToListAsync(
                    cancellationToken);

        if (permissions.Count > 0)
        {
            _dbContext.RolePermissions
                .RemoveRange(permissions);
        }

        role.SoftDelete(currentUserId);

        await _dbContext.SaveChangesAsync(
            cancellationToken);
    }

    private Guid GetTenantId()
    {
        if (!_currentUserContext.IsAuthenticated)
        {
            throw new UnauthorizedAccessException(
                "User is not authenticated.");
        }

        if (_currentUserContext.TenantId
            is not Guid tenantId)
        {
            throw new UnauthorizedAccessException(
                "Tenant ID is missing.");
        }

        return tenantId;
    }

    private Guid GetUserId()
    {
        if (_currentUserContext.UserId
            is not Guid userId)
        {
            throw new UnauthorizedAccessException(
                "Authenticated user ID is missing.");
        }

        return userId;
    }

    private static void ValidateRoleId(
        Guid roleId)
    {
        if (roleId == Guid.Empty)
        {
            throw new ArgumentException(
                "RoleId is required.",
                nameof(roleId));
        }
    }

    private static string NormalizeName(
        string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(
                "Role name is required.");
        }

        var normalized =
            name.Trim();

        if (normalized.Length > 100)
        {
            throw new ArgumentException(
                "Role name cannot exceed 100 characters.");
        }

        return normalized;
    }

    private async Task<string> GenerateUniqueCodeAsync(
        Guid tenantId,
        string name,
        CancellationToken cancellationToken)
    {
        var baseCode =
            GenerateCode(name);

        var code = baseCode;
        var suffix = 2;

        while (await _dbContext.TenantRoles
                   .AnyAsync(
                       x =>
                           x.TenantId == tenantId &&
                           x.Code == code,
                       cancellationToken))
        {
            code =
                $"{baseCode}_{suffix}";

            suffix++;
        }

        return code;
    }

    private static string GenerateCode(
        string name)
    {
        var builder =
            new StringBuilder();

        var previousUnderscore = false;

        foreach (var character in
                 name.Trim().ToUpperInvariant())
        {
            if (char.IsLetterOrDigit(character))
            {
                builder.Append(character);
                previousUnderscore = false;
                continue;
            }

            if (previousUnderscore)
                continue;

            builder.Append('_');
            previousUnderscore = true;
        }

        var code =
            builder
                .ToString()
                .Trim('_');

        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException(
                "Role name must contain letters or numbers.");
        }

        return $"CUSTOM_{code}";
    }
}