using ConnectedOps.Application.Auditing;
using ConnectedOps.Application.Common.Exceptions;
using ConnectedOps.Application.Common.Interfaces;
using ConnectedOps.Application.Organization;
using ConnectedOps.Domain.Auditing;
using ConnectedOps.Domain.Authorization;
using ConnectedOps.Domain.Organization;
using ConnectedOps.Domain.Tenancy;
using ConnectedOps.Infrastructure.Identity;
using ConnectedOps.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ConnectedOps.Infrastructure.Organization;

public sealed class TenantUserAdministrationService : ITenantUserAdministrationService
{
    private readonly ConnectedOpsDbContext _dbContext;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IAuditLogService _auditLogService;

    public TenantUserAdministrationService(
        ConnectedOpsDbContext dbContext,
        UserManager<ApplicationUser> userManager,
        ICurrentUserContext currentUserContext,
        IAuditLogService auditLogService)
    {
        _dbContext = dbContext;
        _userManager = userManager;
        _currentUserContext = currentUserContext;
        _auditLogService = auditLogService;
    }

    public async Task<IReadOnlyCollection<TenantUserAdminDto>> GetUsersAsync(
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();

        var memberships = await _dbContext.TenantUsers
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId)
            .OrderByDescending(x => x.IsActive)
            .ThenBy(x => x.JoinedAtUtc)
            .ToListAsync(cancellationToken);

        if (memberships.Count == 0)
            return Array.Empty<TenantUserAdminDto>();

        var userIds = memberships.Select(x => x.UserId).Distinct().ToList();
        var users = await _userManager.Users
            .AsNoTracking()
            .Where(x => userIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken);

        var tenantUserIds = memberships.Select(x => x.Id).ToList();

        var roleRows = await _dbContext.TenantUserRoles
            .AsNoTracking()
            .Where(x => tenantUserIds.Contains(x.TenantUserId))
            .Select(x => new
            {
                x.TenantUserId,
                RoleId = x.TenantRoleId,
                RoleName = x.TenantRole.Name
            })
            .ToListAsync(cancellationToken);

        var rolesByMembership = roleRows
            .GroupBy(x => x.TenantUserId)
            .ToDictionary(
                g => g.Key,
                g => new
                {
                    Names = g.Select(r => r.RoleName).Distinct().OrderBy(r => r).ToList(),
                    Ids = g.Select(r => r.RoleId).Distinct().ToList()
                });

        var employees = await _dbContext.Employees
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.UserId != null && userIds.Contains(x.UserId.Value))
            .ToDictionaryAsync(x => x.UserId!.Value, cancellationToken);

        var result = new List<TenantUserAdminDto>();

        foreach (var membership in memberships)
        {
            if (!users.TryGetValue(membership.UserId, out var user))
                continue;

            rolesByMembership.TryGetValue(membership.Id, out var userRoles);
            employees.TryGetValue(user.Id, out var employee);

            result.Add(new TenantUserAdminDto(
                membership.Id,
                user.Id,
                user.Email ?? string.Empty,
                user.FirstName,
                user.LastName,
                $"{user.FirstName} {user.LastName}".Trim(),
                user.PhoneNumber,
                membership.IsActive,
                membership.IsDefaultTenant,
                membership.JoinedAtUtc,
                userRoles?.Names ?? (IReadOnlyCollection<string>)Array.Empty<string>(),
                userRoles?.Ids ?? (IReadOnlyCollection<Guid>)Array.Empty<Guid>(),
                employee?.Id,
                employee?.EmployeeNumber));
        }

        return result;
    }

    public async Task<TenantUserAdminDto> GetUserByIdAsync(
        Guid tenantUserId,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();

        var membership = await _dbContext.TenantUsers
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == tenantUserId && x.TenantId == tenantId, cancellationToken);

        if (membership is null)
            throw new KeyNotFoundException($"Tenant user '{tenantUserId}' was not found.");

        var user = await _userManager.FindByIdAsync(membership.UserId.ToString());
        if (user is null)
            throw new KeyNotFoundException($"User account '{membership.UserId}' was not found.");

        var roleRows = await _dbContext.TenantUserRoles
            .AsNoTracking()
            .Where(x => x.TenantUserId == tenantUserId)
            .Select(x => new
            {
                RoleId = x.TenantRoleId,
                RoleName = x.TenantRole.Name
            })
            .ToListAsync(cancellationToken);

        var employee = await _dbContext.Employees
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.UserId == user.Id, cancellationToken);

        return new TenantUserAdminDto(
            membership.Id,
            user.Id,
            user.Email ?? string.Empty,
            user.FirstName,
            user.LastName,
            $"{user.FirstName} {user.LastName}".Trim(),
            user.PhoneNumber,
            membership.IsActive,
            membership.IsDefaultTenant,
            membership.JoinedAtUtc,
            roleRows.Select(r => r.RoleName).OrderBy(r => r).ToList(),
            roleRows.Select(r => r.RoleId).ToList(),
            employee?.Id,
            employee?.EmployeeNumber);
    }

    public async Task<TenantUserAdminDto> CreateUserAsync(
        CreateTenantUserAdminRequest request,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();
        var currentUserId = _currentUserContext.UserId;
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        var existingUser = await _userManager.FindByEmailAsync(normalizedEmail);
        ApplicationUser user;

        if (existingUser is not null)
        {
            user = existingUser;
            var alreadyInTenant = await _dbContext.TenantUsers
                .AnyAsync(x => x.TenantId == tenantId && x.UserId == user.Id, cancellationToken);

            if (alreadyInTenant)
                throw new ConflictException($"User '{normalizedEmail}' already belongs to this organization.");
        }
        else
        {
            user = new ApplicationUser
            {
                UserName = normalizedEmail,
                Email = normalizedEmail,
                FirstName = request.FirstName.Trim(),
                LastName = request.LastName.Trim(),
                PhoneNumber = request.PhoneNumber?.Trim(),
                EmailConfirmed = true,
                IsActive = true
            };

            var userResult = await _userManager.CreateAsync(user, request.Password);
            if (!userResult.Succeeded)
            {
                var errors = string.Join("; ", userResult.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"Failed to create user account: {errors}");
            }
        }

        var tenantUser = new TenantUser(tenantId, user.Id, isDefaultTenant: false);
        _dbContext.TenantUsers.Add(tenantUser);

        // Assign Roles
        var assignedRoleNames = new List<string>();
        var assignedRoleIds = new List<Guid>();

        if (request.RoleIds.Count > 0)
        {
            var validRoles = await _dbContext.TenantRoles
                .Where(r => r.TenantId == tenantId && request.RoleIds.Contains(r.Id))
                .ToListAsync(cancellationToken);

            foreach (var role in validRoles)
            {
                _dbContext.TenantUserRoles.Add(new TenantUserRole(tenantUser.Id, role.Id));
                assignedRoleNames.Add(role.Name);
                assignedRoleIds.Add(role.Id);
            }
        }

        // Handle Linked Employee
        Employee? createdEmployee = null;
        if (request.CreateLinkedEmployee)
        {
            var empCount = await _dbContext.Employees
                .CountAsync(x => x.TenantId == tenantId, cancellationToken);
            var empNumber = $"EMP-{(empCount + 1):D4}";

            // Ensure unique
            while (await _dbContext.Employees.AnyAsync(x => x.TenantId == tenantId && x.EmployeeNumber == empNumber, cancellationToken))
            {
                empCount++;
                empNumber = $"EMP-{(empCount + 1):D4}";
            }

            createdEmployee = new Employee(
                tenantId,
                empNumber,
                request.FirstName,
                request.LastName,
                normalizedEmail,
                request.PhoneNumber,
                request.JobTitle,
                branchId: request.BranchId,
                departmentId: request.DepartmentId,
                teamId: request.TeamId,
                userId: user.Id);

            _dbContext.Employees.Add(createdEmployee);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(
            new CreateAuditLogRequest(
                AuditAction.Created,
                "TenantUser",
                tenantUser.Id.ToString(),
                $"Created organization user: {user.Email}"),
            cancellationToken);

        return new TenantUserAdminDto(
            tenantUser.Id,
            user.Id,
            user.Email ?? string.Empty,
            user.FirstName,
            user.LastName,
            $"{user.FirstName} {user.LastName}".Trim(),
            user.PhoneNumber,
            tenantUser.IsActive,
            tenantUser.IsDefaultTenant,
            tenantUser.JoinedAtUtc,
            assignedRoleNames,
            assignedRoleIds,
            createdEmployee?.Id,
            createdEmployee?.EmployeeNumber);
    }

    public async Task<TenantUserAdminDto> UpdateUserAsync(
        Guid tenantUserId,
        UpdateTenantUserAdminRequest request,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();
        var currentUserId = _currentUserContext.UserId;

        var membership = await _dbContext.TenantUsers
            .FirstOrDefaultAsync(x => x.Id == tenantUserId && x.TenantId == tenantId, cancellationToken);

        if (membership is null)
            throw new KeyNotFoundException($"Tenant user '{tenantUserId}' was not found.");

        var user = await _userManager.FindByIdAsync(membership.UserId.ToString());
        if (user is null)
            throw new KeyNotFoundException($"User account '{membership.UserId}' was not found.");

        user.FirstName = request.FirstName.Trim();
        user.LastName = request.LastName.Trim();
        user.PhoneNumber = request.PhoneNumber?.Trim();
        await _userManager.UpdateAsync(user);

        // Update Roles
        var currentRoles = await _dbContext.TenantUserRoles
            .Include(x => x.TenantRole)
            .Where(x => x.TenantUserId == tenantUserId)
            .ToListAsync(cancellationToken);

        // Check TenantOwner safeguard
        var isOwner = currentRoles.Any(x => x.TenantRole.Code == TenantRoleCodes.TenantOwner);
        if (isOwner)
        {
            var requestedOwner = await _dbContext.TenantRoles
                .AnyAsync(r => request.RoleIds.Contains(r.Id) && r.Code == TenantRoleCodes.TenantOwner && r.TenantId == tenantId, cancellationToken);

            if (!requestedOwner)
            {
                var otherOwners = await _dbContext.TenantUserRoles
                    .CountAsync(x => x.TenantRole.Code == TenantRoleCodes.TenantOwner &&
                                     x.TenantRole.TenantId == tenantId &&
                                     x.TenantUserId != tenantUserId &&
                                     x.TenantUser.IsActive, cancellationToken);

                if (otherOwners == 0)
                    throw new InvalidOperationException("The last active Tenant Owner cannot lose the Tenant Owner role.");
            }
        }

        _dbContext.TenantUserRoles.RemoveRange(currentRoles);

        var validRoles = await _dbContext.TenantRoles
            .Where(r => r.TenantId == tenantId && request.RoleIds.Contains(r.Id))
            .ToListAsync(cancellationToken);

        foreach (var role in validRoles)
        {
            _dbContext.TenantUserRoles.Add(new TenantUserRole(membership.Id, role.Id));
        }

        // Link/Unlink Employee
        if (request.EmployeeId.HasValue && request.EmployeeId.Value != Guid.Empty)
        {
            var employee = await _dbContext.Employees
                .FirstOrDefaultAsync(x => x.Id == request.EmployeeId.Value && x.TenantId == tenantId, cancellationToken);

            if (employee is not null && employee.UserId != user.Id)
            {
                // Unlink old employee if linked
                var oldEmployee = await _dbContext.Employees
                    .FirstOrDefaultAsync(x => x.UserId == user.Id && x.TenantId == tenantId, cancellationToken);
                oldEmployee?.UnlinkUser();

                employee.LinkUser(user.Id);
            }
        }

        membership.MarkUpdated(currentUserId);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(
            new CreateAuditLogRequest(
                AuditAction.Updated,
                "TenantUser",
                membership.Id.ToString(),
                $"Updated organization user: {user.Email}"),
            cancellationToken);

        var currentEmployee = await _dbContext.Employees
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.UserId == user.Id, cancellationToken);

        return new TenantUserAdminDto(
            membership.Id,
            user.Id,
            user.Email ?? string.Empty,
            user.FirstName,
            user.LastName,
            $"{user.FirstName} {user.LastName}".Trim(),
            user.PhoneNumber,
            membership.IsActive,
            membership.IsDefaultTenant,
            membership.JoinedAtUtc,
            validRoles.Select(r => r.Name).OrderBy(r => r).ToList(),
            validRoles.Select(r => r.Id).ToList(),
            currentEmployee?.Id,
            currentEmployee?.EmployeeNumber);
    }

    public async Task ActivateAsync(
        Guid tenantUserId,
        CancellationToken cancellationToken = default)
    {
        await ActivateUserAsync(tenantUserId, cancellationToken);
    }

    public async Task ActivateUserAsync(
        Guid tenantUserId,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();
        var currentUserId = _currentUserContext.UserId;

        var membership = await _dbContext.TenantUsers
            .FirstOrDefaultAsync(x => x.Id == tenantUserId && x.TenantId == tenantId, cancellationToken);

        if (membership is null)
            throw new KeyNotFoundException($"Tenant user '{tenantUserId}' was not found.");

        membership.Activate();
        membership.MarkUpdated(currentUserId);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(
            new CreateAuditLogRequest(
                AuditAction.Activated,
                "TenantUser",
                membership.Id.ToString(),
                $"Activated organization user '{tenantUserId}'"),
            cancellationToken);
    }

    public async Task DeactivateUserAsync(
        Guid tenantUserId,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();
        var currentUserId = _currentUserContext.UserId;

        var membership = await _dbContext.TenantUsers
            .FirstOrDefaultAsync(x => x.Id == tenantUserId && x.TenantId == tenantId, cancellationToken);

        if (membership is null)
            throw new KeyNotFoundException($"Tenant user '{tenantUserId}' was not found.");

        var isOwner = await _dbContext.TenantUserRoles
            .AnyAsync(x => x.TenantUserId == tenantUserId && x.TenantRole.Code == TenantRoleCodes.TenantOwner, cancellationToken);

        if (isOwner)
        {
            var otherOwners = await _dbContext.TenantUserRoles
                .CountAsync(x => x.TenantRole.Code == TenantRoleCodes.TenantOwner &&
                                 x.TenantRole.TenantId == tenantId &&
                                 x.TenantUserId != tenantUserId &&
                                 x.TenantUser.IsActive, cancellationToken);

            if (otherOwners == 0)
                throw new InvalidOperationException("Cannot deactivate the last active Tenant Owner.");
        }

        membership.Deactivate();
        membership.MarkUpdated(currentUserId);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(
            new CreateAuditLogRequest(
                AuditAction.Deactivated,
                "TenantUser",
                membership.Id.ToString(),
                $"Deactivated organization user '{tenantUserId}'"),
            cancellationToken);
    }

    public async Task RemoveUserAsync(
        Guid tenantUserId,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();
        var currentUserId = _currentUserContext.UserId;

        var membership = await _dbContext.TenantUsers
            .FirstOrDefaultAsync(x => x.Id == tenantUserId && x.TenantId == tenantId, cancellationToken);

        if (membership is null)
            throw new KeyNotFoundException($"Tenant user '{tenantUserId}' was not found.");

        var isOwner = await _dbContext.TenantUserRoles
            .AnyAsync(x => x.TenantUserId == tenantUserId && x.TenantRole.Code == TenantRoleCodes.TenantOwner, cancellationToken);

        if (isOwner)
        {
            var otherOwners = await _dbContext.TenantUserRoles
                .CountAsync(x => x.TenantRole.Code == TenantRoleCodes.TenantOwner &&
                                 x.TenantRole.TenantId == tenantId &&
                                 x.TenantUserId != tenantUserId &&
                                 x.TenantUser.IsActive, cancellationToken);

            if (otherOwners == 0)
                throw new InvalidOperationException("Cannot remove the last active Tenant Owner.");
        }

        // Unlink employee if linked
        var employee = await _dbContext.Employees
            .FirstOrDefaultAsync(x => x.UserId == membership.UserId && x.TenantId == tenantId, cancellationToken);
        employee?.UnlinkUser();

        membership.SoftDelete(currentUserId);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(
            new CreateAuditLogRequest(
                AuditAction.Deleted,
                "TenantUser",
                membership.Id.ToString(),
                $"Removed organization user '{tenantUserId}'"),
            cancellationToken);
    }

    private Guid GetCurrentTenantId()
    {
        if (!_currentUserContext.IsAuthenticated)
            throw new UnauthorizedAccessException("User is not authenticated.");

        if (_currentUserContext.TenantId is not Guid tenantId)
            throw new UnauthorizedAccessException("Tenant ID is missing.");

        return tenantId;
    }
}
