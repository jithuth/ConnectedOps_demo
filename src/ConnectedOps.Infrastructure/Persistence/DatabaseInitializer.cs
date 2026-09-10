using ConnectedOps.Infrastructure.Persistence.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ConnectedOps.Infrastructure.Persistence;

public static class DatabaseInitializer
{
    public static async Task InitializeDatabaseAsync(
        this IServiceProvider serviceProvider)
    {
        using var scope =
            serviceProvider.CreateScope();

        var dbContext =
            scope.ServiceProvider
                .GetRequiredService<ConnectedOpsDbContext>();

        await dbContext.Database.MigrateAsync();

        await PermissionSeeder.SeedAsync(
            dbContext);
    }
}