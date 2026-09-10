using ConnectedOps.Application.Common.Interfaces;
using ConnectedOps.Application.Permissions;
using ConnectedOps.Domain.Authorization;
using ConnectedOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ConnectedOps.Infrastructure.Permissions;

public sealed class PermissionManagementService
    : IPermissionManagementService
{
    private readonly ConnectedOpsDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;

    public PermissionManagementService(
        ConnectedOpsDbContext dbContext,
        ICurrentUserContext currentUserContext)
    {
        _dbContext = dbContext;
        _currentUserContext = currentUserContext;
    }

    public async Task<IReadOnlyCollection<PermissionListItem>>
        GetPermissionsAsync(
            CancellationToken cancellationToken = default)
    {
        EnsureAuthenticatedTenant();

        var permissions =
            await _dbContext.Permissions
                .AsNoTracking()
                .OrderBy(x => x.Key)
                .Select(x =>
                    new PermissionListItem(
                        x.Id,
                        x.Key,
                        x.Name,
                        x.Description))
                .ToListAsync(
                    cancellationToken);

        return permissions;
    }

    public async Task<RolePermissionResult>
        GetRolePermissionsAsync(
            Guid roleId,
            CancellationToken cancellationToken = default)
    {
        ValidateRoleId(roleId);

        var tenantId =
            GetTenantId();

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
                        x.Name,
                        x.Code,
                        x.IsSystemRole,

                        Permissions =
                            x.Permissions
                                .Select(rp =>
                                    new PermissionListItem(
                                        rp.Permission.Id,
                                        rp.Permission.Key,
                                        rp.Permission.Name,
                                        rp.Permission.Description))
                                .OrderBy(p => p.Key)
                                .ToList()
                    })
                .SingleOrDefaultAsync(
                    cancellationToken);

        if (role is null)
        {
            throw new KeyNotFoundException(
                "Tenant role was not found.");
        }

        return new RolePermissionResult(
            role.Id,
            role.Name,
            role.Code,
            role.IsSystemRole,
            role.Permissions);
    }

    public async Task UpdateRolePermissionsAsync(
        Guid roleId,
        UpdateRolePermissionsRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateRoleId(roleId);
        ArgumentNullException.ThrowIfNull(request);

        var tenantId =
            GetTenantId();

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
                "Permissions of system roles cannot be modified.");
        }

        var requestedPermissionIds =
            request.PermissionIds
                .Where(x => x != Guid.Empty)
                .Distinct()
                .ToHashSet();

        if (requestedPermissionIds.Count == 0)
        {
            await RemoveAllPermissionsAsync(
                roleId,
                cancellationToken);

            return;
        }

        var validPermissionIds =
            await _dbContext.Permissions
                .Where(x =>
                    requestedPermissionIds.Contains(x.Id))
                .Select(x => x.Id)
                .ToListAsync(
                    cancellationToken);

        if (validPermissionIds.Count !=
            requestedPermissionIds.Count)
        {
            throw new ArgumentException(
                "One or more permission IDs are invalid.");
        }

        var existingAssignments =
            await _dbContext.RolePermissions
                .Where(x =>
                    x.TenantRoleId == roleId)
                .ToListAsync(
                    cancellationToken);

        var existingPermissionIds =
            existingAssignments
                .Select(x => x.PermissionId)
                .ToHashSet();

        var assignmentsToRemove =
            existingAssignments
                .Where(x =>
                    !requestedPermissionIds.Contains(
                        x.PermissionId))
                .ToList();

        if (assignmentsToRemove.Count > 0)
        {
            _dbContext.RolePermissions
                .RemoveRange(assignmentsToRemove);
        }

        var permissionIdsToAdd =
            requestedPermissionIds
                .Where(x =>
                    !existingPermissionIds.Contains(x))
                .ToList();

        foreach (var permissionId
                 in permissionIdsToAdd)
        {
            var assignment =
                new RolePermission(
                    roleId,
                    permissionId);

            _dbContext.RolePermissions
                .Add(assignment);
        }

        await _dbContext.SaveChangesAsync(
            cancellationToken);
    }

    private async Task RemoveAllPermissionsAsync(
        Guid roleId,
        CancellationToken cancellationToken)
    {
        var assignments =
            await _dbContext.RolePermissions
                .Where(x =>
                    x.TenantRoleId == roleId)
                .ToListAsync(
                    cancellationToken);

        if (assignments.Count == 0)
            return;

        _dbContext.RolePermissions
            .RemoveRange(assignments);

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

    private void EnsureAuthenticatedTenant()
    {
        _ = GetTenantId();
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
}