using ConnectedOps.Application.Auth;
using ConnectedOps.Infrastructure.Identity;
using ConnectedOps.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace ConnectedOps.Infrastructure.Auth;

public sealed class AuthenticationService
    : IAuthenticationService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ConnectedOpsDbContext _dbContext;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly JwtSettings _jwtSettings;

    public AuthenticationService(
        UserManager<ApplicationUser> userManager,
        ConnectedOpsDbContext dbContext,
        IJwtTokenService jwtTokenService,
        IRefreshTokenService refreshTokenService,
        IOptions<JwtSettings> jwtOptions)
    {
        _userManager = userManager;
        _dbContext = dbContext;
        _jwtTokenService = jwtTokenService;
        _refreshTokenService = refreshTokenService;
        _jwtSettings = jwtOptions.Value;
    }

    public async Task<LoginResult> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateLoginRequest(request);

        var normalizedEmail =
            request.Email
                .Trim()
                .ToLowerInvariant();

        var user =
            await _userManager.FindByEmailAsync(
                normalizedEmail);

        if (user is null || !user.IsActive)
        {
            throw new UnauthorizedAccessException(
                "Invalid email or password.");
        }

        var passwordValid =
            await _userManager.CheckPasswordAsync(
                user,
                request.Password);

        if (!passwordValid)
        {
            throw new UnauthorizedAccessException(
                "Invalid email or password.");
        }

        if (user.PlatformRole != Domain.Platform.PlatformRole.None)
        {
            user.LastLoginAtUtc = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);

            var platformTokens = await IssuePlatformTokenPairAsync(
                user,
                familyId: null,
                ipAddress: null,
                cancellationToken);

            return new LoginResult(
                RequiresTenantSelection: false,
                AccessToken: platformTokens.AccessToken,
                ExpiresAtUtc: platformTokens.AccessTokenExpiresAtUtc,
                RefreshToken: platformTokens.RefreshToken,
                RefreshTokenExpiresAtUtc: platformTokens.RefreshTokenExpiresAtUtc,
                TenantSelectionToken: null,
                TenantSelectionTokenExpiresAtUtc: null,
                Tenants: []);
        }

        var memberships =
            await _dbContext.TenantUsers
                .AsNoTracking()
                .Include(x => x.Tenant)
                .Where(x =>
                    x.UserId == user.Id &&
                    x.IsActive &&
                    x.Tenant.Status ==
                        Domain.Tenancy.TenantStatus.Active)
                .OrderByDescending(x =>
                    x.IsDefaultTenant)
                .ThenBy(x =>
                    x.Tenant.Name)
                .ToListAsync(
                    cancellationToken);

        if (memberships.Count == 0)
        {
            throw new UnauthorizedAccessException(
                "No active tenant membership found.");
        }

        //
        // Single tenant:
        // immediately issue access + refresh token pair.
        //

        if (memberships.Count == 1)
        {
            var membership =
                memberships[0];

            var tokens =
                await IssueTokenPairAsync(
                    user,
                    membership.TenantId,
                    membership.Id,
                    familyId: null,
                    ipAddress: null,
                    cancellationToken);

            return new LoginResult(
                RequiresTenantSelection: false,
                AccessToken:
                    tokens.AccessToken,
                ExpiresAtUtc:
                    tokens.AccessTokenExpiresAtUtc,
                RefreshToken:
                    tokens.RefreshToken,
                RefreshTokenExpiresAtUtc:
                    tokens.RefreshTokenExpiresAtUtc,
                TenantSelectionToken: null,
                TenantSelectionTokenExpiresAtUtc: null,
                Tenants: []);
        }

        //
        // Multiple tenants:
        // issue only a temporary tenant-selection token.
        //

        var tenantSelectionToken =
            _jwtTokenService
                .CreateTenantSelectionToken(
                    user);

        var tenants =
            memberships
                .Select(x =>
                    new LoginTenantResult(
                        TenantId:
                            x.TenantId,
                        TenantUserId:
                            x.Id,
                        TenantName:
                            x.Tenant.Name,
                        TenantCode:
                            x.Tenant.Code))
                .ToList();

        return new LoginResult(
            RequiresTenantSelection: true,
            AccessToken: null,
            ExpiresAtUtc: null,
            RefreshToken: null,
            RefreshTokenExpiresAtUtc: null,
            TenantSelectionToken:
                tenantSelectionToken.Token,
            TenantSelectionTokenExpiresAtUtc:
                tenantSelectionToken.ExpiresAtUtc,
            Tenants:
                tenants);
    }

    public async Task<LoginResult> SelectTenantAsync(
        SelectTenantRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateSelectTenantRequest(
            request);

        Guid userId;

        try
        {
            userId =
                _jwtTokenService
                    .ValidateTenantSelectionToken(
                        request.TenantSelectionToken);
        }
        catch (SecurityTokenException)
        {
            throw new UnauthorizedAccessException(
                "Invalid or expired tenant selection token.");
        }
        catch (ArgumentException)
        {
            throw new UnauthorizedAccessException(
                "Invalid or expired tenant selection token.");
        }

        var user =
            await _userManager.FindByIdAsync(
                userId.ToString());

        if (user is null || !user.IsActive)
        {
            throw new UnauthorizedAccessException(
                "User account is not available.");
        }

        //
        // Never trust tenant ID from client without
        // re-checking membership in the database.
        //

        var membership =
            await _dbContext.TenantUsers
                .AsNoTracking()
                .Include(x => x.Tenant)
                .SingleOrDefaultAsync(
                    x =>
                        x.UserId == user.Id &&
                        x.TenantId ==
                            request.TenantId &&
                        x.IsActive &&
                        x.Tenant.Status ==
                            Domain.Tenancy.TenantStatus.Active,
                    cancellationToken);

        if (membership is null)
        {
            throw new UnauthorizedAccessException(
                "The selected tenant is not available.");
        }

        var tokens =
            await IssueTokenPairAsync(
                user,
                membership.TenantId,
                membership.Id,
                familyId: null,
                ipAddress: null,
                cancellationToken);

        return new LoginResult(
            RequiresTenantSelection: false,
            AccessToken:
                tokens.AccessToken,
            ExpiresAtUtc:
                tokens.AccessTokenExpiresAtUtc,
            RefreshToken:
                tokens.RefreshToken,
            RefreshTokenExpiresAtUtc:
                tokens.RefreshTokenExpiresAtUtc,
            TenantSelectionToken: null,
            TenantSelectionTokenExpiresAtUtc: null,
            Tenants: []);
    }

    public async Task<AuthTokenResult> RefreshAsync(
        RefreshTokenRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            throw new ArgumentNullException(
                nameof(request));
        }

        if (string.IsNullOrWhiteSpace(
                request.RefreshToken))
        {
            throw new ArgumentException(
                "Refresh token is required.");
        }

        var tokenHash =
            _refreshTokenService.HashToken(
                request.RefreshToken);

        var storedToken =
            await _dbContext.RefreshTokens
                .SingleOrDefaultAsync(
                    x =>
                        x.TokenHash == tokenHash,
                    cancellationToken);

        if (storedToken is null)
        {
            throw new UnauthorizedAccessException(
                "Invalid refresh token.");
        }

        //
        // If an already-used or revoked refresh token
        // appears again, treat it as compromise.
        //

        if (storedToken.IsUsed ||
            storedToken.IsRevoked)
        {
            await RevokeTokenFamilyAsync(
                storedToken.FamilyId,
                ipAddress,
                cancellationToken);

            throw new UnauthorizedAccessException(
                "Refresh token reuse detected.");
        }

        if (storedToken.IsExpired)
        {
            throw new UnauthorizedAccessException(
                "Refresh token has expired.");
        }

        var user =
            await _userManager.FindByIdAsync(
                storedToken.UserId.ToString());

        if (user is null || !user.IsActive)
        {
            await RevokeTokenFamilyAsync(
                storedToken.FamilyId,
                ipAddress,
                cancellationToken);

            throw new UnauthorizedAccessException(
                "User account is not available.");
        }

        (string Token, DateTime ExpiresAtUtc) accessToken;

        if (storedToken.TenantId.HasValue && storedToken.TenantUserId.HasValue)
        {
            //
            // Revalidate tenant membership.
            // A refresh token must not survive removal
            // from a tenant.
            //
            var membership =
                await _dbContext.TenantUsers
                    .Include(x => x.Tenant)
                    .SingleOrDefaultAsync(
                        x =>
                            x.Id ==
                                storedToken.TenantUserId.Value &&
                            x.UserId ==
                                storedToken.UserId &&
                            x.TenantId ==
                                storedToken.TenantId.Value &&
                            x.IsActive &&
                            x.Tenant.Status ==
                                Domain.Tenancy.TenantStatus.Active,
                        cancellationToken);

            if (membership is null)
            {
                await RevokeTokenFamilyAsync(
                    storedToken.FamilyId,
                    ipAddress,
                    cancellationToken);

                throw new UnauthorizedAccessException(
                    "Tenant membership is no longer active.");
            }

            accessToken =
                _jwtTokenService
                    .CreateAccessToken(
                        user,
                        storedToken.TenantId.Value,
                        storedToken.TenantUserId.Value);
        }
        else
        {
            //
            // Platform session refresh
            //
            if (user.PlatformRole == Domain.Platform.PlatformRole.None)
            {
                await RevokeTokenFamilyAsync(
                    storedToken.FamilyId,
                    ipAddress,
                    cancellationToken);

                throw new UnauthorizedAccessException(
                    "Platform administrator access is no longer active.");
            }

            accessToken =
                _jwtTokenService
                    .CreatePlatformAccessToken(user);
        }

        //
        // Rotate refresh token.
        //

        storedToken.MarkUsed();

        var newRawRefreshToken =
            _refreshTokenService
                .GenerateToken();

        var newTokenHash =
            _refreshTokenService
                .HashToken(
                    newRawRefreshToken);

        var newExpiry =
            DateTime.UtcNow.AddDays(
                _jwtSettings.RefreshTokenDays);

        var replacementToken =
            new Domain.Auth.RefreshToken(
                user.Id,
                storedToken.TenantId,
                storedToken.TenantUserId,
                newTokenHash,
                storedToken.FamilyId,
                newExpiry,
                ipAddress);

        _dbContext.RefreshTokens.Add(
            replacementToken);

        storedToken.ReplaceWith(
            replacementToken.Id);

        await _dbContext.SaveChangesAsync(
            cancellationToken);

        return new AuthTokenResult(
            AccessToken:
                accessToken.Token,
            AccessTokenExpiresAtUtc:
                accessToken.ExpiresAtUtc,
            RefreshToken:
                newRawRefreshToken,
            RefreshTokenExpiresAtUtc:
                newExpiry);
    }

    private async Task<AuthTokenResult>
        IssuePlatformTokenPairAsync(
            ApplicationUser user,
            Guid? familyId,
            string? ipAddress,
            CancellationToken cancellationToken)
    {
        var accessToken =
            _jwtTokenService
                .CreatePlatformAccessToken(user);

        var rawRefreshToken =
            _refreshTokenService
                .GenerateToken();

        var tokenHash =
            _refreshTokenService
                .HashToken(rawRefreshToken);

        var expiresAtUtc =
            DateTime.UtcNow.AddDays(
                _jwtSettings.RefreshTokenDays);

        var tokenFamilyId =
            familyId ?? Guid.NewGuid();

        var refreshToken =
            new Domain.Auth.RefreshToken(
                user.Id,
                tenantId: null,
                tenantUserId: null,
                tokenHash,
                tokenFamilyId,
                expiresAtUtc,
                ipAddress);

        _dbContext.RefreshTokens.Add(
            refreshToken);

        await _dbContext.SaveChangesAsync(
            cancellationToken);

        return new AuthTokenResult(
            AccessToken:
                accessToken.Token,
            AccessTokenExpiresAtUtc:
                accessToken.ExpiresAtUtc,
            RefreshToken:
                rawRefreshToken,
            RefreshTokenExpiresAtUtc:
                expiresAtUtc);
    }

    private async Task<AuthTokenResult>
        IssueTokenPairAsync(
            ApplicationUser user,
            Guid tenantId,
            Guid tenantUserId,
            Guid? familyId,
            string? ipAddress,
            CancellationToken cancellationToken)
    {
        var accessToken =
            _jwtTokenService
                .CreateAccessToken(
                    user,
                    tenantId,
                    tenantUserId);

        var rawRefreshToken =
            _refreshTokenService
                .GenerateToken();

        var tokenHash =
            _refreshTokenService
                .HashToken(
                    rawRefreshToken);

        var expiresAtUtc =
            DateTime.UtcNow.AddDays(
                _jwtSettings.RefreshTokenDays);

        var tokenFamilyId =
            familyId ?? Guid.NewGuid();

        var refreshToken =
            new Domain.Auth.RefreshToken(
                user.Id,
                tenantId,
                tenantUserId,
                tokenHash,
                tokenFamilyId,
                expiresAtUtc,
                ipAddress);

        _dbContext.RefreshTokens.Add(
            refreshToken);

        await _dbContext.SaveChangesAsync(
            cancellationToken);

        return new AuthTokenResult(
            AccessToken:
                accessToken.Token,
            AccessTokenExpiresAtUtc:
                accessToken.ExpiresAtUtc,
            RefreshToken:
                rawRefreshToken,
            RefreshTokenExpiresAtUtc:
                expiresAtUtc);
    }

    private async Task RevokeTokenFamilyAsync(
    Guid familyId,
    string? ipAddress,
    CancellationToken cancellationToken)
    {
        var tokens =
            await _dbContext.RefreshTokens
                .Where(x =>
                    x.FamilyId == familyId &&
                    x.RevokedAtUtc == null)
                .ToListAsync(
                    cancellationToken);

        foreach (var token in tokens)
        {
            token.Revoke(ipAddress);
        }

        await _dbContext.SaveChangesAsync(
            cancellationToken);
    }

    public async Task LogoutAsync(
    LogoutRequest request,
    string? ipAddress,
    CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            throw new ArgumentNullException(
                nameof(request));
        }

        if (string.IsNullOrWhiteSpace(
                request.RefreshToken))
        {
            throw new ArgumentException(
                "Refresh token is required.");
        }

        var tokenHash =
            _refreshTokenService.HashToken(
                request.RefreshToken);

        var storedToken =
            await _dbContext.RefreshTokens
                .SingleOrDefaultAsync(
                    x => x.TokenHash == tokenHash,
                    cancellationToken);

        //
        // Logout should be idempotent.
        //
        // If the token does not exist, simply return.
        // We do not reveal whether a token was valid.
        //

        if (storedToken is null)
        {
            return;
        }

        await RevokeTokenFamilyAsync(
            storedToken.FamilyId,
            ipAddress,
            cancellationToken);
    }

    public async Task LogoutAllAsync(
    Guid userId,
    string? ipAddress,
    CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException(
                "UserId is required.",
                nameof(userId));
        }

        var tokens =
            await _dbContext.RefreshTokens
                .Where(x =>
                    x.UserId == userId &&
                    x.RevokedAtUtc == null &&
                    x.ExpiresAtUtc > DateTime.UtcNow)
                .ToListAsync(
                    cancellationToken);

        if (tokens.Count == 0)
        {
            return;
        }

        foreach (var token in tokens)
        {
            token.Revoke(
                ipAddress);
        }

        await _dbContext.SaveChangesAsync(
            cancellationToken);
    }

    private static void ValidateLoginRequest(
        LoginRequest request)
    {
        if (request is null)
        {
            throw new ArgumentNullException(
                nameof(request));
        }

        if (string.IsNullOrWhiteSpace(
                request.Email))
        {
            throw new ArgumentException(
                "Email is required.");
        }

        if (string.IsNullOrWhiteSpace(
                request.Password))
        {
            throw new ArgumentException(
                "Password is required.");
        }
    }

    private static void ValidateSelectTenantRequest(
        SelectTenantRequest request)
    {
        if (request is null)
        {
            throw new ArgumentNullException(
                nameof(request));
        }

        if (string.IsNullOrWhiteSpace(
                request.TenantSelectionToken))
        {
            throw new ArgumentException(
                "Tenant selection token is required.");
        }

        if (request.TenantId ==
            Guid.Empty)
        {
            throw new ArgumentException(
                "TenantId is required.");
        }
    }
}