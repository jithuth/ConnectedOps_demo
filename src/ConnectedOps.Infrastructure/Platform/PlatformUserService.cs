using System.Security.Cryptography;
using ConnectedOps.Application.Platform;
using ConnectedOps.Domain.Platform;
using ConnectedOps.Infrastructure.Identity;
using ConnectedOps.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ConnectedOps.Infrastructure.Platform;

public sealed class PlatformUserService : IPlatformUserService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ConnectedOpsDbContext _dbContext;

    public PlatformUserService(
        UserManager<ApplicationUser> userManager,
        ConnectedOpsDbContext dbContext)
    {
        _userManager = userManager;
        _dbContext = dbContext;
    }

    public async Task<PlatformUserPage> GetUsersAsync(
        PlatformUserQuery query,
        CancellationToken cancellationToken = default)
    {
        var page = Math.Max(query.Page, 1);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);

        var dbQuery = _userManager.Users
            .AsNoTracking()
            .AsQueryable();

        if (query.Role.HasValue)
        {
            dbQuery = dbQuery.Where(u => u.PlatformRole == query.Role.Value);
        }
        else
        {
            // By default list platform administrators
            dbQuery = dbQuery.Where(u => u.PlatformRole != PlatformRole.None);
        }

        if (query.IsActive.HasValue)
        {
            dbQuery = dbQuery.Where(u => u.IsActive == query.IsActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            dbQuery = dbQuery.Where(u =>
                (u.Email != null && u.Email.Contains(search)) ||
                u.FirstName.Contains(search) ||
                u.LastName.Contains(search));
        }

        var totalCount = await dbQuery.CountAsync(cancellationToken);

        var items = await dbQuery
            .OrderByDescending(u => u.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new PlatformUserDto(
                u.Id,
                u.Email ?? string.Empty,
                u.FullName,
                u.FirstName,
                u.LastName,
                u.PlatformRole,
                u.IsActive,
                u.CreatedAtUtc,
                u.LastLoginAtUtc))
            .ToListAsync(cancellationToken);

        return new PlatformUserPage(items, page, pageSize, totalCount);
    }

    public async Task<PlatformUserDetailDto?> GetUserByIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var user = await _userManager.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user is null)
            return null;

        var membershipCount = await _dbContext.TenantUsers
            .CountAsync(tu => tu.UserId == userId && !tu.IsDeleted, cancellationToken);

        return new PlatformUserDetailDto(
            user.Id,
            user.Email ?? string.Empty,
            user.FullName,
            user.FirstName,
            user.LastName,
            user.PlatformRole,
            user.IsActive,
            user.CreatedAtUtc,
            user.LastLoginAtUtc,
            membershipCount);
    }

    public async Task<CreatePlatformUserResult> CreateUserAsync(
        CreatePlatformUserRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.Role == PlatformRole.None)
        {
            throw new ArgumentException("Platform user must have a valid platform role.", nameof(request.Role));
        }

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var existing = await _userManager.FindByEmailAsync(normalizedEmail);
        if (existing != null)
        {
            throw new InvalidOperationException($"User with email '{normalizedEmail}' already exists.");
        }

        var initialPassword = string.IsNullOrWhiteSpace(request.InitialPassword)
            ? GenerateSecurePassword()
            : request.InitialPassword;

        var user = new ApplicationUser
        {
            UserName = normalizedEmail,
            Email = normalizedEmail,
            EmailConfirmed = true,
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            PlatformRole = request.Role,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, initialPassword);

        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Failed to create user: {errors}");
        }

        var dto = new PlatformUserDto(
            user.Id,
            user.Email ?? string.Empty,
            user.FullName,
            user.FirstName,
            user.LastName,
            user.PlatformRole,
            user.IsActive,
            user.CreatedAtUtc,
            user.LastLoginAtUtc);

        return new CreatePlatformUserResult(dto, initialPassword);
    }

    public async Task UpdateUserRoleAsync(
        Guid userId,
        PlatformRole role,
        CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null)
            throw new KeyNotFoundException($"User {userId} not found.");

        user.PlatformRole = role;
        var result = await _userManager.UpdateAsync(user);

        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Failed to update user role: {errors}");
        }
    }

    public async Task ActivateUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null)
            throw new KeyNotFoundException($"User {userId} not found.");

        user.IsActive = true;
        await _userManager.UpdateAsync(user);
    }

    public async Task DeactivateUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null)
            throw new KeyNotFoundException($"User {userId} not found.");

        user.IsActive = false;
        await _userManager.UpdateAsync(user);
    }

    private static string GenerateSecurePassword()
    {
        const string upper = "ABCDEFGHJKLMNOPQRSTUVWXYZ";
        const string lower = "abcdefghijkmnopqrstuvwxyz";
        const string digits = "0123456789";
        const string specials = "!@$?_-";

        var chars = new char[16];
        chars[0] = upper[RandomNumberGenerator.GetInt32(upper.Length)];
        chars[1] = lower[RandomNumberGenerator.GetInt32(lower.Length)];
        chars[2] = digits[RandomNumberGenerator.GetInt32(digits.Length)];
        chars[3] = specials[RandomNumberGenerator.GetInt32(specials.Length)];

        const string all = upper + lower + digits + specials;
        for (var i = 4; i < chars.Length; i++)
        {
            chars[i] = all[RandomNumberGenerator.GetInt32(all.Length)];
        }

        RandomNumberGenerator.Shuffle(chars.AsSpan());
        return new string(chars);
    }
}
