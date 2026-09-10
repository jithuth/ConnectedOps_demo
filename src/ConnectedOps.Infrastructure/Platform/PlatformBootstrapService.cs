using ConnectedOps.Domain.Platform;
using ConnectedOps.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ConnectedOps.Infrastructure.Platform;

public static class PlatformBootstrapService
{
    public static async Task BootstrapPlatformAsync(this IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>()
            .CreateLogger(typeof(PlatformBootstrapService));

        var options = scope.ServiceProvider.GetRequiredService<IOptions<PlatformBootstrapOptions>>().Value;

        if (!options.Enabled)
        {
            logger.LogInformation("Platform bootstrap is disabled.");
            return;
        }

        if (string.IsNullOrWhiteSpace(options.Email) || string.IsNullOrWhiteSpace(options.Password))
        {
            logger.LogWarning("Platform bootstrap is enabled but Email or Password is not provided.");
            return;
        }

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var email = options.Email.Trim();

        var existingUser = await userManager.FindByEmailAsync(email)
            ?? await userManager.FindByNameAsync(email);

        if (existingUser is null)
        {
            logger.LogInformation("Creating initial SuperAdmin user for {Email}.", email);

            var superAdmin = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                FirstName = string.IsNullOrWhiteSpace(options.FirstName) ? "Platform" : options.FirstName.Trim(),
                LastName = string.IsNullOrWhiteSpace(options.LastName) ? "SuperAdmin" : options.LastName.Trim(),
                PlatformRole = PlatformRole.SuperAdmin,
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow
            };

            try
            {
                var result = await userManager.CreateAsync(superAdmin, options.Password);

                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    logger.LogError("Failed to create SuperAdmin user: {Errors}", errors);
                    throw new InvalidOperationException($"Platform bootstrap failed: {errors}");
                }

                logger.LogInformation("SuperAdmin user created successfully.");
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateException)
            {
                // Handle concurrent bootstrap execution across multiple processes/nodes
                var concurrentUser = await userManager.FindByEmailAsync(email)
                    ?? await userManager.FindByNameAsync(email);

                if (concurrentUser is not null && concurrentUser.PlatformRole == PlatformRole.SuperAdmin)
                {
                    logger.LogInformation("SuperAdmin user {Email} already exists (concurrent bootstrap). Platform bootstrap skipped (idempotent).", email);
                    return;
                }

                throw;
            }
        }
        else if (existingUser.PlatformRole == PlatformRole.SuperAdmin)
        {
            logger.LogInformation("SuperAdmin user {Email} already exists. Platform bootstrap skipped (idempotent).", email);
        }
        else
        {
            logger.LogError("Email {Email} is already registered as a non-SuperAdmin user. Cannot promote to SuperAdmin via bootstrap.", email);
            throw new InvalidOperationException($"Platform bootstrap failed: User {email} exists and is not a SuperAdmin. Promotion is rejected.");
        }
    }
}
