using ConnectedOps.Application.Auth;
using ConnectedOps.Application.Common.Interfaces;
using ConnectedOps.Infrastructure.Identity;
using ConnectedOps.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ConnectedOps.Infrastructure.Auth;

public sealed class CurrentUserService
    : ICurrentUserService
{
    private readonly ICurrentUserContext _currentUserContext;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ConnectedOpsDbContext _dbContext;

    public CurrentUserService(
        ICurrentUserContext currentUserContext,
        UserManager<ApplicationUser> userManager,
        ConnectedOpsDbContext dbContext)
    {
        _currentUserContext =
            currentUserContext;

        _userManager =
            userManager;

        _dbContext =
            dbContext;
    }

    public async Task<CurrentUserResult>
        GetCurrentUserAsync(
            CancellationToken cancellationToken = default)
    {
        if (!_currentUserContext.IsAuthenticated)
        {
            throw new UnauthorizedAccessException(
                "User is not authenticated.");
        }

        if (_currentUserContext.UserId
            is not Guid userId)
        {
            throw new UnauthorizedAccessException(
                "Authenticated user ID is missing.");
        }

        if (_currentUserContext.TenantId
            is not Guid tenantId)
        {
            throw new UnauthorizedAccessException(
                "Tenant ID is missing.");
        }

        if (_currentUserContext.TenantUserId
            is not Guid tenantUserId)
        {
            throw new UnauthorizedAccessException(
                "Tenant membership ID is missing.");
        }

        var user =
            await _userManager
                .FindByIdAsync(
                    userId.ToString());

        if (user is null ||
            !user.IsActive)
        {
            throw new UnauthorizedAccessException(
                "User account is not available.");
        }

        var membership =
            await _dbContext
                .TenantUsers
                .AsNoTracking()
                .Include(x => x.Tenant)
                .SingleOrDefaultAsync(
                    x =>
                        x.Id == tenantUserId &&
                        x.UserId == userId &&
                        x.TenantId == tenantId &&
                        x.IsActive &&
                        !x.IsDeleted,
                    cancellationToken);

        if (membership is null)
        {
            throw new UnauthorizedAccessException(
                "Tenant membership is not available.");
        }

        var roles =
            await _dbContext
                .TenantUserRoles
                .AsNoTracking()
                .Where(x =>
                    x.TenantUserId == tenantUserId &&
                    !x.IsDeleted)
                .Select(x =>
                    x.TenantRole.Code)
                .Distinct()
                .OrderBy(x => x)
                .ToListAsync(
                    cancellationToken);

        var permissions =
            await _dbContext
                .TenantUserRoles
                .AsNoTracking()
                .Where(x =>
                    x.TenantUserId == tenantUserId &&
                    !x.IsDeleted)
                .SelectMany(x =>
                    x.TenantRole.Permissions)
                .Where(x =>
                    !x.IsDeleted)
                .Select(x =>
                    x.Permission.Key)
                .Distinct()
                .OrderBy(x => x)
                .ToListAsync(
                    cancellationToken);

        return new CurrentUserResult(
            UserId: user.Id,

            Email:
                user.Email
                ?? string.Empty,

            FirstName:
                user.FirstName,

            LastName:
                user.LastName,

            FullName:
                user.FullName,

            TenantId:
                membership.TenantId,

            TenantUserId:
                membership.Id,

            TenantName:
                membership.Tenant.Name,

            TenantCode:
                membership.Tenant.Code,

            IsDefaultTenant:
                membership.IsDefaultTenant,

            Roles:
                roles,

            Permissions:
                permissions);
    }
}