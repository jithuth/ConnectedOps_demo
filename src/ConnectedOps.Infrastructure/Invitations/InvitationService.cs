using ConnectedOps.Application.Common.Interfaces;
using ConnectedOps.Application.Invitations;
using ConnectedOps.Domain.Authorization;
using ConnectedOps.Domain.Tenancy;
using ConnectedOps.Infrastructure.Auth;
using ConnectedOps.Infrastructure.Identity;
using ConnectedOps.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ConnectedOps.Infrastructure.Invitations;

public sealed class InvitationService
    : IInvitationService
{
    private readonly ConnectedOpsDbContext _dbContext;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IInvitationTokenService _tokenService;

    public InvitationService(
        ConnectedOpsDbContext dbContext,
        UserManager<ApplicationUser> userManager,
        ICurrentUserContext currentUserContext,
        IInvitationTokenService tokenService)
    {
        _dbContext = dbContext;
        _userManager = userManager;
        _currentUserContext = currentUserContext;
        _tokenService = tokenService;
    }

    public async Task<CreateInvitationResult> CreateAsync(
        CreateInvitationRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request is null)
            throw new ArgumentNullException(
                nameof(request));

        if (!_currentUserContext.IsAuthenticated)
            throw new UnauthorizedAccessException(
                "User is not authenticated.");

        if (_currentUserContext.UserId
            is not Guid currentUserId)
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

        if (string.IsNullOrWhiteSpace(request.Email))
            throw new ArgumentException(
                "Email is required.");

        if (request.TenantRoleId == Guid.Empty)
            throw new ArgumentException(
                "TenantRoleId is required.");

        var email =
            request.Email
                .Trim()
                .ToLowerInvariant();

        var tenant =
            await _dbContext.Tenants
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    x =>
                        x.Id == tenantId &&
                        x.Status == TenantStatus.Active,
                    cancellationToken);

        if (tenant is null)
            throw new InvalidOperationException(
                "Tenant is not available.");

        var role =
            await _dbContext.TenantRoles
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    x =>
                        x.Id == request.TenantRoleId &&
                        x.TenantId == tenantId,
                    cancellationToken);

        if (role is null)
            throw new ArgumentException(
                "The selected tenant role does not exist.");

        var existingIdentityUser =
            await _userManager.FindByEmailAsync(email);

        if (existingIdentityUser is not null)
        {
            var alreadyMember =
                await _dbContext.TenantUsers
                    .AnyAsync(
                        x =>
                            x.TenantId == tenantId &&
                            x.UserId == existingIdentityUser.Id &&
                            x.IsActive,
                        cancellationToken);

            if (alreadyMember)
            {
                throw new InvalidOperationException(
                    "This user is already a member of the tenant.");
            }
        }

        var now =
            DateTime.UtcNow;

        var pendingInvitation =
            await _dbContext.TenantInvitations
                .FirstOrDefaultAsync(
                    x =>
                        x.TenantId == tenantId &&
                        x.Email == email &&
                        x.AcceptedAtUtc == null &&
                        x.RevokedAtUtc == null &&
                        x.ExpiresAtUtc > now,
                    cancellationToken);

        if (pendingInvitation is not null)
        {
            throw new InvalidOperationException(
                "An active invitation already exists for this email address.");
        }

        var rawToken =
            _tokenService.GenerateToken();

        var tokenHash =
            _tokenService.HashToken(rawToken);

        var expiresAtUtc =
            DateTime.UtcNow.AddDays(7);

        var invitation =
            new TenantInvitation(
                tenantId,
                email,
                role.Id,
                currentUserId,
                tokenHash,
                expiresAtUtc);

        _dbContext.TenantInvitations
            .Add(invitation);

        await _dbContext.SaveChangesAsync(
            cancellationToken);

        return new CreateInvitationResult(
            InvitationId: invitation.Id,
            Email: invitation.Email,
            TenantId: invitation.TenantId,
            TenantRoleId: role.Id,
            RoleName: role.Name,
            ExpiresAtUtc: invitation.ExpiresAtUtc,
            InvitationToken: rawToken);
    }

    public async Task<AcceptInvitationResult> AcceptAsync(
        AcceptInvitationRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request is null)
            throw new ArgumentNullException(
                nameof(request));

        if (string.IsNullOrWhiteSpace(request.Token))
            throw new ArgumentException(
                "Invitation token is required.");

        var tokenHash =
            _tokenService.HashToken(
                request.Token);

        var invitation =
            await _dbContext.TenantInvitations
                .Include(x => x.Tenant)
                .Include(x => x.TenantRole)
                .SingleOrDefaultAsync(
                    x => x.TokenHash == tokenHash,
                    cancellationToken);

        if (invitation is null)
        {
            throw new UnauthorizedAccessException(
                "Invalid invitation token.");
        }

        if (invitation.AcceptedAtUtc.HasValue)
        {
            throw new InvalidOperationException(
                "Invitation has already been accepted.");
        }

        if (invitation.RevokedAtUtc.HasValue)
        {
            throw new UnauthorizedAccessException(
                "Invitation has been revoked.");
        }

        if (invitation.ExpiresAtUtc <= DateTime.UtcNow)
        {
            throw new UnauthorizedAccessException(
                "Invitation has expired.");
        }

        if (invitation.Tenant.Status
            != TenantStatus.Active)
        {
            throw new InvalidOperationException(
                "Tenant is not active.");
        }

        await using var transaction =
            await _dbContext.Database
                .BeginTransactionAsync(
                    cancellationToken);

        var user =
            await _userManager.FindByEmailAsync(
                invitation.Email);

        if (user is null)
        {
            ValidateNewUserRequest(request);

            user =
                new ApplicationUser
                {
                    UserName =
                        invitation.Email,

                    Email =
                        invitation.Email,

                    FirstName =
                        request.FirstName.Trim(),

                    LastName =
                        request.LastName.Trim(),

                    IsActive =
                        true
                };

            var createResult =
                await _userManager.CreateAsync(
                    user,
                    request.Password);

            if (!createResult.Succeeded)
            {
                var errors =
                    string.Join(
                        "; ",
                        createResult.Errors
                            .Select(x =>
                                x.Description));

                throw new InvalidOperationException(
                    $"Unable to create user: {errors}");
            }
        }
        else if (!user.IsActive)
        {
            throw new UnauthorizedAccessException(
                "User account is disabled.");
        }

        var existingMembership =
            await _dbContext.TenantUsers
                .SingleOrDefaultAsync(
                    x =>
                        x.TenantId == invitation.TenantId &&
                        x.UserId == user.Id,
                    cancellationToken);

        TenantUser tenantUser;

        if (existingMembership is null)
        {
            tenantUser =
                new TenantUser(
                    invitation.TenantId,
                    user.Id,
                    isDefaultTenant: false);

            _dbContext.TenantUsers
                .Add(tenantUser);
        }
        else
        {
            if (existingMembership.IsActive)
            {
                throw new InvalidOperationException(
                    "User is already a member of this tenant.");
            }

            existingMembership.Activate();
            tenantUser =
                existingMembership;
        }

        await _dbContext.SaveChangesAsync(
            cancellationToken);

        var alreadyHasRole =
            await _dbContext.TenantUserRoles
                .AnyAsync(
                    x =>
                        x.TenantUserId == tenantUser.Id &&
                        x.TenantRoleId == invitation.TenantRoleId,
                    cancellationToken);

        if (!alreadyHasRole)
        {
            var tenantUserRole =
                new TenantUserRole(
                    tenantUser.Id,
                    invitation.TenantRoleId);

            _dbContext.TenantUserRoles
                .Add(tenantUserRole);
        }

        invitation.Accept(
            user.Id);

        await _dbContext.SaveChangesAsync(
            cancellationToken);

        await transaction.CommitAsync(
            cancellationToken);

        return new AcceptInvitationResult(
            UserId: user.Id,
            TenantId: invitation.TenantId,
            TenantUserId: tenantUser.Id,
            TenantName: invitation.Tenant.Name,
            RoleCode: invitation.TenantRole.Code);
    }

    public async Task RevokeAsync(
        Guid invitationId,
        CancellationToken cancellationToken = default)
    {
        if (!_currentUserContext.IsAuthenticated)
        {
            throw new UnauthorizedAccessException(
                "User is not authenticated.");
        }

        if (_currentUserContext.UserId
            is not Guid currentUserId)
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

        if (invitationId == Guid.Empty)
        {
            throw new ArgumentException(
                "InvitationId is required.",
                nameof(invitationId));
        }

        var invitation =
            await _dbContext.TenantInvitations
                .SingleOrDefaultAsync(
                    x =>
                        x.Id == invitationId &&
                        x.TenantId == tenantId,
                    cancellationToken);

        if (invitation is null)
        {
            throw new KeyNotFoundException(
                "Invitation was not found.");
        }

        invitation.Revoke(
            currentUserId);

        await _dbContext.SaveChangesAsync(
            cancellationToken);
    }

    private static void ValidateNewUserRequest(
        AcceptInvitationRequest request)
    {
        if (string.IsNullOrWhiteSpace(
                request.FirstName))
        {
            throw new ArgumentException(
                "First name is required for a new user.");
        }

        if (string.IsNullOrWhiteSpace(
                request.LastName))
        {
            throw new ArgumentException(
                "Last name is required for a new user.");
        }

        if (string.IsNullOrWhiteSpace(
                request.Password))
        {
            throw new ArgumentException(
                "Password is required for a new user.");
        }
    }
}