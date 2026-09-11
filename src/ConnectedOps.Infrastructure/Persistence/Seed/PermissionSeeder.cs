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
            PermissionKeys.Organization.View,
            "View Organization",
            "Organization"),

        new(
            PermissionKeys.Organization.Manage,
            "Manage Organization",
            "Organization"),

        new(
            PermissionKeys.Branches.View,
            "View Branches",
            "Branches"),

        new(
            PermissionKeys.Branches.Manage,
            "Manage Branches",
            "Branches"),

        new(
            PermissionKeys.Locations.View,
            "View Locations",
            "Locations"),

        new(
            PermissionKeys.Locations.Manage,
            "Manage Locations",
            "Locations"),

        new(
            PermissionKeys.Departments.View,
            "View Departments",
            "Departments"),

        new(
            PermissionKeys.Departments.Manage,
            "Manage Departments",
            "Departments"),

        new(
            PermissionKeys.Teams.View,
            "View Teams",
            "Teams"),

        new(
            PermissionKeys.Teams.Manage,
            "Manage Teams",
            "Teams"),

        new(
            PermissionKeys.Employees.View,
            "View Employees",
            "Employees"),

        new(
            PermissionKeys.Employees.Create,
            "Create Employees",
            "Employees"),

        new(
            PermissionKeys.Employees.Edit,
            "Edit Employees",
            "Employees"),

        new(
            PermissionKeys.Employees.Delete,
            "Delete Employees",
            "Employees"),

        new(
            PermissionKeys.OrganizationDashboard.View,
            "View Organization Dashboard",
            "Organization"),

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
            PermissionKeys.Vehicles.ManageStatus,
            "Manage Vehicle Status",
            "Vehicles"),

        new(
            PermissionKeys.Vehicles.ManageOdometer,
            "Manage Vehicle Odometer",
            "Vehicles"),

        new(
            PermissionKeys.Vehicles.ManageDocuments,
            "Manage Vehicle Documents",
            "Vehicles"),

        new(
            PermissionKeys.Vehicles.ManageNotes,
            "Manage Vehicle Notes",
            "Vehicles"),

        new(
            PermissionKeys.VehicleCategories.View,
            "View Vehicle Categories",
            "Vehicles"),

        new(
            PermissionKeys.VehicleCategories.Manage,
            "Manage Vehicle Categories",
            "Vehicles"),

        new(
            PermissionKeys.VehicleMakes.View,
            "View Vehicle Makes",
            "Vehicles"),

        new(
            PermissionKeys.VehicleMakes.Manage,
            "Manage Vehicle Makes",
            "Vehicles"),

        new(
            PermissionKeys.VehicleModels.View,
            "View Vehicle Models",
            "Vehicles"),

        new(
            PermissionKeys.VehicleModels.Manage,
            "Manage Vehicle Models",
            "Vehicles"),

        new(
            PermissionKeys.VehicleDashboard.View,
            "View Fleet Dashboard",
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
            PermissionKeys.Drivers.ChangeStatus,
            "Change Driver Status",
            "Drivers"),

        new(
            PermissionKeys.Drivers.ManageOrganization,
            "Manage Driver Organization",
            "Drivers"),

        new(
            PermissionKeys.Drivers.LinkEmployee,
            "Link Driver Employee",
            "Drivers"),

        new(
            PermissionKeys.DriverLicenses.View,
            "View Driver Licenses",
            "Drivers"),

        new(
            PermissionKeys.DriverLicenses.Manage,
            "Manage Driver Licenses",
            "Drivers"),

        new(
            PermissionKeys.DriverCertifications.View,
            "View Driver Certifications",
            "Drivers"),

        new(
            PermissionKeys.DriverCertifications.Manage,
            "Manage Driver Certifications",
            "Drivers"),

        new(
            PermissionKeys.DriverDocuments.View,
            "View Driver Documents",
            "Drivers"),

        new(
            PermissionKeys.DriverDocuments.Manage,
            "Manage Driver Documents",
            "Drivers"),

        new(
            PermissionKeys.DriverAssignments.View,
            "View Driver Assignments",
            "Drivers"),

        new(
            PermissionKeys.DriverAssignments.Create,
            "Create Driver Assignments",
            "Drivers"),

        new(
            PermissionKeys.DriverAssignments.End,
            "End Driver Assignments",
            "Drivers"),

        new(
            PermissionKeys.DriverDashboard.View,
            "View Driver Dashboard",
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
            "Reports"),

        new(
            PermissionKeys.FleetOperations.View,
            "View Fleet Operations",
            "FleetOperations"),

        new(
            PermissionKeys.FleetOperations.ManageOperations,
            "Manage Fleet Operations",
            "FleetOperations"),

        new(
            PermissionKeys.FleetOperations.Checkout,
            "Vehicle Checkout",
            "FleetOperations"),

        new(
            PermissionKeys.FleetOperations.CheckIn,
            "Vehicle Check-In",
            "FleetOperations"),

        new(
            PermissionKeys.FleetOperations.Handover,
            "Vehicle Handover",
            "FleetOperations"),

        new(
            PermissionKeys.FleetOperations.ManageShifts,
            "Manage Shifts",
            "FleetOperations"),

        new(
            PermissionKeys.FleetOperations.AssignShifts,
            "Assign Shifts",
            "FleetOperations"),

        new(
            PermissionKeys.FleetOperations.ManageExceptions,
            "Manage Operational Exceptions",
            "FleetOperations"),

        new(
            PermissionKeys.FleetOperations.ViewBoard,
            "View Operations Board",
            "FleetOperations"),

        new(
            PermissionKeys.FleetOperations.ViewTimeline,
            "View Fleet Activity Timeline",
            "FleetOperations")
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