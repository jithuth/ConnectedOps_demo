using ConnectedOps.Domain.Authorization;
using Microsoft.EntityFrameworkCore;

namespace ConnectedOps.Infrastructure.Persistence.Seed;

public static class PermissionSeeder
{
    private static readonly PermissionSeed[] Permissions =
    [
        new(
            PermissionKeys.Tenants.View,
            "View Tenant",
            "Tenants"),

        new(
            PermissionKeys.Tenants.Manage,
            "Manage Tenant",
            "Tenants"),

        new(
            PermissionKeys.Users.View,
            "View Users",
            "Users"),

        new(
            PermissionKeys.Users.Create,
            "Create Users",
            "Users"),

        new(
            PermissionKeys.Users.Edit,
            "Edit Users",
            "Users"),

        new(
            PermissionKeys.Users.Delete,
            "Delete Users",
            "Users"),

        new(
            PermissionKeys.Users.ManageRoles,
            "Manage User Roles",
            "Users"),

        new(
            PermissionKeys.Vehicles.View,
            "View Vehicles",
            "Vehicles"),

        new(
            PermissionKeys.Vehicles.Create,
            "Create Vehicles",
            "Vehicles"),

        new(
            PermissionKeys.Vehicles.Edit,
            "Edit Vehicles",
            "Vehicles"),

        new(
            PermissionKeys.Vehicles.Delete,
            "Delete Vehicles",
            "Vehicles"),

        new(
            PermissionKeys.Vehicles.AssignDriver,
            "Assign Driver",
            "Vehicles"),

        new(
            PermissionKeys.Drivers.View,
            "View Drivers",
            "Drivers"),

        new(
            PermissionKeys.Drivers.Create,
            "Create Drivers",
            "Drivers"),

        new(
            PermissionKeys.Drivers.Edit,
            "Edit Drivers",
            "Drivers"),

        new(
            PermissionKeys.Drivers.Delete,
            "Delete Drivers",
            "Drivers"),

        new(
            PermissionKeys.Drivers.AssignVehicle,
            "Assign Vehicle",
            "Drivers"),

        new(
            PermissionKeys.Assets.View,
            "View Assets",
            "Assets"),

        new(
            PermissionKeys.Assets.Create,
            "Create Assets",
            "Assets"),

        new(
            PermissionKeys.Assets.Edit,
            "Edit Assets",
            "Assets"),

        new(
            PermissionKeys.Assets.Delete,
            "Delete Assets",
            "Assets"),

        new(
            PermissionKeys.Reports.View,
            "View Reports",
            "Reports"),

        new(
            PermissionKeys.Reports.Export,
            "Export Reports",
            "Reports")
    ];

    public static async Task SeedAsync(
        ConnectedOpsDbContext dbContext,
        CancellationToken cancellationToken = default)
    {
        foreach (var item in Permissions)
        {
            var exists =
                await dbContext.Permissions
                    .IgnoreQueryFilters()
                    .AnyAsync(
                        x => x.Key == item.Key,
                        cancellationToken);

            if (exists)
                continue;

            dbContext.Permissions.Add(
                new Permission(
                    item.Key,
                    item.Name,
                    item.Module));
        }

        await dbContext.SaveChangesAsync(
            cancellationToken);
    }

    private sealed record PermissionSeed(
        string Key,
        string Name,
        string Module);
}