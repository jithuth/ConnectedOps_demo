using ConnectedOps.Application.Common.Interfaces;
using ConnectedOps.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace ConnectedOps.Infrastructure.Authorization;

public sealed class PermissionAuthorizationHandler
    : AuthorizationHandler<PermissionRequirement>
{
    private readonly ConnectedOpsDbContext _dbContext;
    private readonly ICurrentTenantContext _currentTenant;

    public PermissionAuthorizationHandler(
        ConnectedOpsDbContext dbContext,
        ICurrentTenantContext currentTenant)
    {
        _dbContext = dbContext;
        _currentTenant = currentTenant;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        if (!_currentTenant.IsAuthenticated)
            return;

        if (_currentTenant.UserId is not Guid userId)
            return;

        if (_currentTenant.TenantId is not Guid tenantId)
            return;

        if (_currentTenant.TenantUserId is not Guid tenantUserId)
            return;

        var hasPermission =
            await _dbContext.TenantUserRoles
                .AsNoTracking()
                .Where(x =>
                    !x.IsDeleted &&
                    x.TenantUserId == tenantUserId)
                .Where(x =>
                    x.TenantUser.UserId == userId &&
                    x.TenantUser.TenantId == tenantId &&
                    x.TenantUser.IsActive)
                .Where(x =>
                    x.TenantUser.Tenant.Status ==
                    Domain.Tenancy.TenantStatus.Active)
                .Where(x =>
                    x.TenantRole.TenantId == tenantId &&
                    x.TenantRole.IsActive)
                .AnyAsync(x =>
                    x.TenantRole.Permissions.Any(rp =>
                        !rp.IsDeleted &&
                        rp.Permission.IsActive &&
                        rp.Permission.Key ==
                            requirement.Permission));

        if (hasPermission)
        {
            context.Succeed(requirement);
        }
    }
}