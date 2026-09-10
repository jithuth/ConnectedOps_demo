using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

using ConnectedOps.Domain.Platform;
using ConnectedOps.Infrastructure.Identity;

using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace ConnectedOps.Infrastructure.Auth;

public sealed class JwtTokenService
    : IJwtTokenService
{
    private readonly JwtSettings _settings;

    private readonly JwtSecurityTokenHandler
        _tokenHandler;

    private readonly SymmetricSecurityKey
        _signingKey;

    private readonly SigningCredentials
        _signingCredentials;

    public JwtTokenService(
        IOptions<JwtSettings> options)
    {
        ArgumentNullException.ThrowIfNull(options);

        _settings = options.Value
            ?? throw new InvalidOperationException(
                "JWT settings are not configured.");

        ValidateSettings(_settings);

        _tokenHandler =
            new JwtSecurityTokenHandler();

        _signingKey =
            new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(
                    _settings.SigningKey));

        _signingCredentials =
            new SigningCredentials(
                _signingKey,
                SecurityAlgorithms.HmacSha256);
    }

    // ============================================================
    // TENANT ACCESS TOKEN
    // ============================================================

    public (
        string Token,
        DateTime ExpiresAtUtc)
        CreateAccessToken(
            ApplicationUser user,
            Guid tenantId,
            Guid tenantUserId)
    {
        ArgumentNullException.ThrowIfNull(user);

        if (user.Id == Guid.Empty)
        {
            throw new ArgumentException(
                "User Id is required.",
                nameof(user));
        }

        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException(
                "Tenant Id is required.",
                nameof(tenantId));
        }

        if (tenantUserId == Guid.Empty)
        {
            throw new ArgumentException(
                "Tenant user Id is required.",
                nameof(tenantUserId));
        }

        var now =
            DateTime.UtcNow;

        var expiresAtUtc =
            now.AddMinutes(
                _settings.AccessTokenMinutes);

        var claims =
            CreateBaseClaims(user);

        claims.Add(
            new Claim(
                ConnectedOpsClaimTypes.TenantId,
                tenantId.ToString()));

        claims.Add(
            new Claim(
                ConnectedOpsClaimTypes.TenantUserId,
                tenantUserId.ToString()));

        claims.Add(
            new Claim(
                ConnectedOpsClaimTypes.PlatformRole,
                PlatformRole.None.ToString()));

        claims.Add(
            new Claim(
                ConnectedOpsClaimTypes.TokenType,
                TokenTypes.Access));

        var token =
            CreateJwtToken(
                claims,
                now,
                expiresAtUtc);

        return (
            _tokenHandler.WriteToken(token),
            expiresAtUtc);
    }

    // ============================================================
    // PLATFORM ACCESS TOKEN
    // ============================================================

    public (
        string Token,
        DateTime ExpiresAtUtc)
        CreatePlatformAccessToken(
            ApplicationUser user)
    {
        ArgumentNullException.ThrowIfNull(user);

        if (user.Id == Guid.Empty)
        {
            throw new ArgumentException(
                "User Id is required.",
                nameof(user));
        }

        if (!user.IsActive)
        {
            throw new UnauthorizedAccessException(
                "Platform user account is inactive.");
        }

        if (user.PlatformRole ==
            PlatformRole.None)
        {
            throw new UnauthorizedAccessException(
                "The user is not a platform administrator.");
        }

        var now =
            DateTime.UtcNow;

        var expiresAtUtc =
            now.AddMinutes(
                _settings.AccessTokenMinutes);

        var claims =
            CreateBaseClaims(user);

        claims.Add(
            new Claim(
                ConnectedOpsClaimTypes.PlatformRole,
                user.PlatformRole.ToString()));

        claims.Add(
            new Claim(
                ConnectedOpsClaimTypes.TokenType,
                TokenTypes.Access));

        /*
         * IMPORTANT:
         *
         * Platform tokens intentionally DO NOT contain:
         *
         * tenant_id
         * tenant_user_id
         *
         * The platform administrator operates above the
         * tenant boundary.
         */

        var token =
            CreateJwtToken(
                claims,
                now,
                expiresAtUtc);

        return (
            _tokenHandler.WriteToken(token),
            expiresAtUtc);
    }

    // ============================================================
    // TENANT SELECTION TOKEN
    // ============================================================

    public (
        string Token,
        DateTime ExpiresAtUtc)
        CreateTenantSelectionToken(
            ApplicationUser user)
    {
        ArgumentNullException.ThrowIfNull(user);

        if (user.Id == Guid.Empty)
        {
            throw new ArgumentException(
                "User Id is required.",
                nameof(user));
        }

        var now =
            DateTime.UtcNow;

        /*
         * Keep tenant-selection tokens short-lived.
         *
         * This token exists only long enough for the user
         * to select which tenant they want to enter.
         */
        var expiresAtUtc =
            now.AddMinutes(5);

        var claims =
            CreateBaseClaims(user);

        claims.Add(
            new Claim(
                ConnectedOpsClaimTypes.TokenType,
                TokenTypes.TenantSelection));

        claims.Add(
            new Claim(
                ConnectedOpsClaimTypes.PlatformRole,
                PlatformRole.None.ToString()));

        var token =
            CreateJwtToken(
                claims,
                now,
                expiresAtUtc);

        return (
            _tokenHandler.WriteToken(token),
            expiresAtUtc);
    }

    // ============================================================
    // VALIDATE TENANT SELECTION TOKEN
    // ============================================================

    public Guid ValidateTenantSelectionToken(
        string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new SecurityTokenException(
                "Tenant selection token is required.");
        }

        var validationParameters =
            new TokenValidationParameters
            {
                ValidateIssuer = true,

                ValidIssuer =
                    _settings.Issuer,

                ValidateAudience = true,

                ValidAudience =
                    _settings.Audience,

                ValidateIssuerSigningKey = true,

                IssuerSigningKey =
                    _signingKey,

                ValidateLifetime = true,

                ClockSkew =
                    TimeSpan.FromSeconds(30)
            };

        ClaimsPrincipal principal;

        try
        {
            principal =
                _tokenHandler.ValidateToken(
                    token,
                    validationParameters,
                    out var validatedToken);

            if (validatedToken
                is not JwtSecurityToken jwtToken)
            {
                throw new SecurityTokenException(
                    "Invalid token.");
            }

            if (!string.Equals(
                    jwtToken.Header.Alg,
                    SecurityAlgorithms.HmacSha256,
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new SecurityTokenException(
                    "Invalid token signing algorithm.");
            }
        }
        catch (SecurityTokenException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw new SecurityTokenException(
                "Invalid tenant selection token.",
                exception);
        }

        var tokenType =
            principal.FindFirst(
                ConnectedOpsClaimTypes.TokenType)?
                .Value;

        if (!string.Equals(
                tokenType,
                TokenTypes.TenantSelection,
                StringComparison.Ordinal))
        {
            throw new SecurityTokenException(
                "The supplied token is not a tenant selection token.");
        }

        var userIdValue =
            principal.FindFirst(
                ConnectedOpsClaimTypes.UserId)?
                .Value;

        if (!Guid.TryParse(
                userIdValue,
                out var userId))
        {
            throw new SecurityTokenException(
                "The token does not contain a valid user identifier.");
        }

        return userId;
    }

    // ============================================================
    // COMMON CLAIMS
    // ============================================================

    private static List<Claim>
        CreateBaseClaims(
            ApplicationUser user)
    {
        var claims =
            new List<Claim>
            {
                new(
                    JwtRegisteredClaimNames.Sub,
                    user.Id.ToString()),

                new(
                    ConnectedOpsClaimTypes.UserId,
                    user.Id.ToString()),

                new(
                    JwtRegisteredClaimNames.Jti,
                    Guid.NewGuid().ToString())
            };

        if (!string.IsNullOrWhiteSpace(
                user.Email))
        {
            claims.Add(
                new Claim(
                    JwtRegisteredClaimNames.Email,
                    user.Email));

            claims.Add(
                new Claim(
                    ClaimTypes.Email,
                    user.Email));
        }

        if (!string.IsNullOrWhiteSpace(
                user.UserName))
        {
            claims.Add(
                new Claim(
                    ClaimTypes.Name,
                    user.UserName));
        }

        return claims;
    }

    // ============================================================
    // CREATE JWT
    // ============================================================

    private JwtSecurityToken CreateJwtToken(
        IEnumerable<Claim> claims,
        DateTime notBeforeUtc,
        DateTime expiresAtUtc)
    {
        return new JwtSecurityToken(
            issuer:
                _settings.Issuer,

            audience:
                _settings.Audience,

            claims:
                claims,

            notBefore:
                notBeforeUtc,

            expires:
                expiresAtUtc,

            signingCredentials:
                _signingCredentials);
    }

    // ============================================================
    // VALIDATION
    // ============================================================

    private static void ValidateSettings(
        JwtSettings settings)
    {
        if (string.IsNullOrWhiteSpace(
                settings.SigningKey))
        {
            throw new InvalidOperationException(
                "Jwt:SigningKey is not configured.");
        }

        if (settings.SigningKey.Length < 32)
        {
            throw new InvalidOperationException(
                "Jwt:SigningKey must contain at least 32 characters.");
        }

        if (string.IsNullOrWhiteSpace(
                settings.Issuer))
        {
            throw new InvalidOperationException(
                "Jwt:Issuer is not configured.");
        }

        if (string.IsNullOrWhiteSpace(
                settings.Audience))
        {
            throw new InvalidOperationException(
                "Jwt:Audience is not configured.");
        }

        if (_IsInvalidAccessTokenLifetime(
                settings.AccessTokenMinutes))
        {
            throw new InvalidOperationException(
                "Jwt:AccessTokenMinutes must be greater than zero.");
        }
    }

    private static bool
        _IsInvalidAccessTokenLifetime(
            int minutes)
    {
        return minutes <= 0;
    }
}