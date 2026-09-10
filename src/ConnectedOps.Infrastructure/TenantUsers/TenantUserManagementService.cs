using ConnectedOps.Application.Common.Interfaces;
using ConnectedOps.Application.TenantUsers;
using ConnectedOps.Domain.Authorization;
using ConnectedOps.Domain.Tenancy;
using ConnectedOps.Infrastructure.Identity;
using ConnectedOps.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ConnectedOps.Infrastructure.TenantUsers;

public sealed class TenantUserManagementService
    : ITenantUserManagementService
{
    private readonly ConnectedOpsDbContext _dbContext;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ICurrentUserContext _currentUserContext;

    public TenantUserManagementService(
        ConnectedOpsDbContext dbContext,
        UserManager<ApplicationUser> userManager,
        ICurrentUserContext currentUserContext)
    {
        _dbContext = dbContext;
        _userManager = userManager;
        _currentUserContext = currentUserContext;
    }

    public async Task<IReadOnlyCollection<TenantUserListItem>> GetUsersAsync(
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();

        var memberships =
            await _dbContext.TenantUsers
                .AsNoTracking()
                .Where(x => x.TenantId == tenantId)
                .OrderByDescending(x => x.IsActive)
                .ThenBy(x => x.JoinedAtUtc)
                .ToListAsync(cancellationToken);

        if (memberships.Count == 0)
            return Array.Empty<TenantUserListItem>();

        var userIds =
            memberships
                .Select(x => x.UserId)
                .Distinct()
                .ToList();

        var users =
            await _userManager.Users
                .AsNoTracking()
                .Where(x => userIds.Contains(x.Id))
                .ToDictionaryAsync(
                    x => x.Id,
                    cancellationToken);

        var tenantUserIds =
            memberships
                .Select(x => x.Id)
                .ToList();

        var roleRows =
            await _dbContext.TenantUserRoles
                .AsNoTracking()
                .Where(x =>
                    tenantUserIds.Contains(x.TenantUserId))
                .Select(x => new
                {
                    x.TenantUserId,
                    RoleCode = x.TenantRole.Code
                })
                .ToListAsync(cancellationToken);

        var rolesByMembership =
            roleRows
                .GroupBy(x => x.TenantUserId)
                .ToDictionary(
                    x => x.Key,
                    x => (IReadOnlyCollection<string>)x
                        .Select(r => r.RoleCode)
                        .Distinct()
                        .OrderBy(r => r)
                        .ToList());

        var result =
            new List<TenantUserListItem>();

        foreach (var membership in memberships)
        {
            if (!users.TryGetValue(
                    membership.UserId,
                    out var user))
            {
                continue;
            }

            rolesByMembership.TryGetValue(
                membership.Id,
                out var roles);

            result.Add(
                new TenantUserListItem(
                    TenantUserId: membership.Id,
                    UserId: user.Id,
                    Email: user.Email ?? string.Empty,
                    FirstName: user.FirstName,
                    LastName: user.LastName,
                    FullName: user.FullName,
                    IsActive: membership.IsActive,
                    IsDefaultTenant: membership.IsDefaultTenant,
                    JoinedAtUtc: membership.JoinedAtUtc,
                    Roles: roles ?? Array.Empty<string>()));
        }

        return result;
    }

    public async Task<TenantUserDetailsResult> GetUserAsync(
        Guid tenantUserId,
        CancellationToken cancellationToken = default)
    {
        if (tenantUserId == Guid.Empty)
            throw new ArgumentException(
                "TenantUserId is required.",
                nameof(tenantUserId));

        var tenantId = GetCurrentTenantId();

        var membership =
            await _dbContext.TenantUsers
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    x =>
                        x.Id == tenantUserId &&
                        x.TenantId == tenantId,
                    cancellationToken);

        if (membership is null)
            throw new KeyNotFoundException(
                "Tenant user was not found.");

        var user =
            await _userManager.FindByIdAsync(
                membership.UserId.ToString());

        if (user is null)
            throw new KeyNotFoundException(
                "User account was not found.");

        var roles =
            await _dbContext.TenantUserRoles
                .AsNoTracking()
                .Where(x =>
                    x.TenantUserId == tenantUserId)
                .Select(x =>
                    new TenantUserRoleResult(
                        x.TenantRole.Id,
                        x.TenantRole.Name,
                        x.TenantRole.Code))
                .OrderBy(x => x.Name)
                .ToListAsync(cancellationToken);

        var permissions =
            await _dbContext.TenantUserRoles
                .AsNoTracking()
                .Where(x =>
                    x.TenantUserId == tenantUserId)
                .SelectMany(x =>
                    x.TenantRole.Permissions)
                .Select(x =>
                    x.Permission.Key)
                .Distinct()
                .OrderBy(x => x)
                .ToListAsync(cancellationToken);

        return new TenantUserDetailsResult(
            TenantUserId: membership.Id,
            UserId: user.Id,
            TenantId: membership.TenantId,
            Email: user.Email ?? string.Empty,
            FirstName: user.FirstName,
            LastName: user.LastName,
            FullName: user.FullName,
            IsActive: membership.IsActive,
            IsDefaultTenant: membership.IsDefaultTenant,
            JoinedAtUtc: membership.JoinedAtUtc,
            Roles: roles,
            Permissions: permissions);
    }

    public async Task UpdateRolesAsync(
        Guid tenantUserId,
        UpdateTenantUserRolesRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request is null)
            throw new ArgumentNullException(
                nameof(request));

        if (tenantUserId == Guid.Empty)
            throw new ArgumentException(
                "TenantUserId is required.",
                nameof(tenantUserId));

        var tenantId = GetCurrentTenantId();
        var currentUserId = GetCurrentUserId();

        var membership =
            await _dbContext.TenantUsers
                .SingleOrDefaultAsync(
                    x =>
                        x.Id == tenantUserId &&
                        x.TenantId == tenantId,
                    cancellationToken);

        if (membership is null)
            throw new KeyNotFoundException(
                "Tenant user was not found.");

        if (!membership.IsActive)
            throw new InvalidOperationException(
                "Roles cannot be changed for an inactive tenant user.");

        var requestedRoleIds =
            request.RoleIds
                .Where(x => x != Guid.Empty)
                .Distinct()
                .ToList();

        if (requestedRoleIds.Count == 0)
            throw new ArgumentException(
                "At least one tenant role is required.");

        var validRoles =
            await _dbContext.TenantRoles
                .Where(x =>
                    x.TenantId == tenantId &&
                    requestedRoleIds.Contains(x.Id))
                .ToListAsync(cancellationToken);

        if (validRoles.Count != requestedRoleIds.Count)
        {
            throw new ArgumentException(
                "One or more selected roles do not belong to this tenant.");
        }

        await EnsureOwnerSafetyAsync(
            membership,
            validRoles,
            currentUserId,
            cancellationToken);

        var existingAssignments =
            await _dbContext.TenantUserRoles
                .Where(x =>
                    x.TenantUserId == tenantUserId)
                .ToListAsync(cancellationToken);

        var assignmentsToRemove =
            existingAssignments
                .Where(x =>
                    !requestedRoleIds.Contains(
                        x.TenantRoleId))
                .ToList();

        if (assignmentsToRemove.Count > 0)
        {
            _dbContext.TenantUserRoles
                .RemoveRange(assignmentsToRemove);
        }

        var existingRoleIds =
            existingAssignments
                .Select(x => x.TenantRoleId)
                .ToHashSet();

        foreach (var roleId in requestedRoleIds)
        {
            if (existingRoleIds.Contains(roleId))
                continue;

            var assignment =
                new TenantUserRole(
                    tenantUserId,
                    roleId);

            _dbContext.TenantUserRoles
                .Add(assignment);
        }

        await _dbContext.SaveChangesAsync(
            cancellationToken);
    }

    public async Task ActivateAsync(
        Guid tenantUserId,
        CancellationToken cancellationToken = default)
    {
        var membership =
            await GetMembershipForWriteAsync(
                tenantUserId,
                cancellationToken);

        if (membership.IsActive)
            return;

        membership.Activate();

        await _dbContext.SaveChangesAsync(
            cancellationToken);
    }

    public async Task DeactivateAsync(
        Guid tenantUserId,
        CancellationToken cancellationToken = default)
    {
        var membership =
            await GetMembershipForWriteAsync(
                tenantUserId,
                cancellationToken);

        var currentUserId =
            GetCurrentUserId();

        if (membership.UserId == currentUserId)
        {
            throw new InvalidOperationException(
                "You cannot deactivate your own tenant membership.");
        }

        if (!membership.IsActive)
            return;

        await EnsureCanDisableMembershipAsync(
            membership,
            cancellationToken);

        membership.Deactivate();

        await _dbContext.SaveChangesAsync(
            cancellationToken);
    }

    public async Task RemoveAsync(
        Guid tenantUserId,
        CancellationToken cancellationToken = default)
    {
        var membership =
            await GetMembershipForWriteAsync(
                tenantUserId,
                cancellationToken);

        var currentUserId =
            GetCurrentUserId();

        if (membership.UserId == currentUserId)
        {
            throw new InvalidOperationException(
                "You cannot remove your own tenant membership.");
        }

        await EnsureCanDisableMembershipAsync(
            membership,
            cancellationToken);

        var roleAssignments =
            await _dbContext.TenantUserRoles
                .Where(x =>
                    x.TenantUserId == membership.Id)
                .ToListAsync(cancellationToken);

        if (roleAssignments.Count > 0)
        {
            _dbContext.TenantUserRoles
                .RemoveRange(roleAssignments);
        }

        membership.Deactivate();
        membership.SoftDelete(currentUserId);

        await _dbContext.SaveChangesAsync(
            cancellationToken);
    }

    private Guid GetCurrentTenantId()
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

    private Guid GetCurrentUserId()
    {
        if (_currentUserContext.UserId
            is not Guid userId)
        {
            throw new UnauthorizedAccessException(
                "Authenticated user ID is missing.");
        }

        return userId;
    }

    private async Task<TenantUser> GetMembershipForWriteAsync(
        Guid tenantUserId,
        CancellationToken cancellationToken)
    {
        if (tenantUserId == Guid.Empty)
            throw new ArgumentException(
                "TenantUserId is required.",
                nameof(tenantUserId));

        var tenantId =
            GetCurrentTenantId();

        var membership =
            await _dbContext.TenantUsers
                .SingleOrDefaultAsync(
                    x =>
                        x.Id == tenantUserId &&
                        x.TenantId == tenantId,
                    cancellationToken);

        if (membership is null)
        {
            throw new KeyNotFoundException(
                "Tenant user was not found.");
        }

        return membership;
    }

    private async Task EnsureCanDisableMembershipAsync(
        TenantUser membership,
        CancellationToken cancellationToken)
    {
        var ownerRole =
            await _dbContext.TenantRoles
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    x =>
                        x.TenantId == membership.TenantId &&
                        x.Code == TenantRoleCodes.TenantOwner,
                    cancellationToken);

        if (ownerRole is null)
            return;

        var isOwner =
            await _dbContext.TenantUserRoles
                .AnyAsync(
                    x =>
                        x.TenantUserId == membership.Id &&
                        x.TenantRoleId == ownerRole.Id,
                    cancellationToken);

        if (!isOwner)
            return;

        var activeOwnerCount =
            await _dbContext.TenantUserRoles
                .CountAsync(
                    x =>
                        x.TenantRoleId == ownerRole.Id &&
                        x.TenantUser.IsActive,
                    cancellationToken);

        if (activeOwnerCount <= 1)
        {
            throw new InvalidOperationException(
                "The last active tenant owner cannot be deactivated or removed.");
        }
    }

    private async Task EnsureOwnerSafetyAsync(
        TenantUser membership,
        IReadOnlyCollection<TenantRole> requestedRoles,
        Guid currentUserId,
        CancellationToken cancellationToken)
    {
        var ownerRole =
            await _dbContext.TenantRoles
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    x =>
                        x.TenantId == membership.TenantId &&
                        x.Code == TenantRoleCodes.TenantOwner,
                    cancellationToken);

        if (ownerRole is null)
            return;

        var currentlyOwner =
            await _dbContext.TenantUserRoles
                .AnyAsync(
                    x =>
                        x.TenantUserId == membership.Id &&
                        x.TenantRoleId == ownerRole.Id,
                    cancellationToken);

        if (!currentlyOwner)
            return;

        var keepingOwnerRole =
            requestedRoles.Any(
                x => x.Id == ownerRole.Id);

        if (keepingOwnerRole)
            return;

        var otherActiveOwners =
            await _dbContext.TenantUserRoles
                .CountAsync(
                    x =>
                        x.TenantRoleId == ownerRole.Id &&
                        x.TenantUserId != membership.Id &&
                        x.TenantUser.IsActive,
                    cancellationToken);

        if (otherActiveOwners == 0)
        {
            throw new InvalidOperationException(
                "The last active tenant owner cannot lose the Tenant Owner role.");
        }
    }
}