using ConnectedOps.Infrastructure.Persistence.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ConnectedOps.Infrastructure.Persistence;

public static class DatabaseInitializer
{
    public static async Task InitializeDatabaseAsync(
        this IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var configuration = scope.ServiceProvider.GetService<Microsoft.Extensions.Configuration.IConfiguration>();
        if (configuration?.GetValue<bool>("Database:AutoMigrate", true) == false)
        {
            return;
        }

        var logger = scope.ServiceProvider.GetService<ILoggerFactory>()?.CreateLogger(typeof(DatabaseInitializer));

        var dbContext = scope.ServiceProvider.GetRequiredService<ConnectedOpsDbContext>();

        if (dbContext.Database.IsRelational())
        {
            int retries = 10;
            while (retries > 0)
            {
                try
                {
                    logger?.LogInformation("Applying database migrations for ConnectedOpsDb...");
                    await dbContext.Database.MigrateAsync();
                    logger?.LogInformation("Database migrations applied successfully.");
                    break;
                }
                catch (Exception ex) when (retries > 1)
                {
                    retries--;
                    logger?.LogWarning("Database connection not ready yet, retrying in 2 seconds... ({RetriesLeft} attempts left). Error: {Message}", retries, ex.Message);
                    await Task.Delay(2000);
                }
            }
        }
        else
        {
            await dbContext.Database.EnsureCreatedAsync();
        }

        await PermissionSeeder.SeedAsync(dbContext);
    }
}