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
            "FleetOperations"),

        // PHASE 6 - TRACKING DEVICES & TELEMATICS
        new(
            PermissionKeys.TrackingDevices.View,
            "View Tracking Devices",
            "TrackingDevices"),

        new(
            PermissionKeys.TrackingDevices.Create,
            "Create Tracking Devices",
            "TrackingDevices"),

        new(
            PermissionKeys.TrackingDevices.Edit,
            "Edit Tracking Devices",
            "TrackingDevices"),

        new(
            PermissionKeys.TrackingDevices.Delete,
            "Delete Tracking Devices",
            "TrackingDevices"),

        new(
            PermissionKeys.TrackingDevices.Provision,
            "Provision Tracking Devices",
            "TrackingDevices"),

        new(
            PermissionKeys.TrackingDevices.AssignVehicle,
            "Assign Tracking Device to Vehicle",
            "TrackingDevices"),

        new(
            PermissionKeys.Telematics.View,
            "View Telematics",
            "Telematics"),

        new(
            PermissionKeys.Telematics.ViewLive,
            "View Live Fleet Tracking",
            "Telematics"),

        new(
            PermissionKeys.Telematics.ViewHistory,
            "View Telemetry History",
            "Telematics"),

        new(
            PermissionKeys.Telematics.ViewDeviceHealth,
            "View Device Health",
            "Telematics"),

        new(
            PermissionKeys.Telematics.ViewDashboard,
            "View Telematics Dashboard",
            "Telematics"),

        new(
            PermissionKeys.Telematics.SendCommands,
            "Send Device Commands",
            "Telematics"),

        // Phase 7: Maps
        new(
            PermissionKeys.Maps.View,
            "View Maps",
            "Maps"),

        new(
            PermissionKeys.Maps.ViewLive,
            "View Live Fleet Map",
            "Maps"),

        new(
            PermissionKeys.Maps.ViewHistory,
            "View Historical Map Trails",
            "Maps"),

        // Phase 7: Geofences
        new(
            PermissionKeys.Geofences.View,
            "View Geofences",
            "Geofences"),

        new(
            PermissionKeys.Geofences.Create,
            "Create Geofences",
            "Geofences"),

        new(
            PermissionKeys.Geofences.Edit,
            "Edit Geofences",
            "Geofences"),

        new(
            PermissionKeys.Geofences.Delete,
            "Delete Geofences",
            "Geofences"),

        new(
            PermissionKeys.Geofences.ViewEvents,
            "View Geofence Events",
            "Geofences"),

        // Phase 7: Demo Fleet
        new(
            PermissionKeys.DemoFleet.View,
            "View Demo Fleet Simulator",
            "DemoFleet"),

        new(
            PermissionKeys.DemoFleet.Manage,
            "Manage Demo Fleet Simulator",
            "DemoFleet"),

        // PHASE 8 - VEHICLE MAINTENANCE
        new(
            PermissionKeys.Maintenance.View,
            "View Maintenance Subsystem",
            "Maintenance"),

        new(
            PermissionKeys.MaintenancePlans.View,
            "View Maintenance Plans",
            "Maintenance"),

        new(
            PermissionKeys.MaintenancePlans.Create,
            "Create Maintenance Plans",
            "Maintenance"),

        new(
            PermissionKeys.MaintenancePlans.Edit,
            "Edit Maintenance Plans",
            "Maintenance"),

        new(
            PermissionKeys.MaintenancePlans.Delete,
            "Delete Maintenance Plans",
            "Maintenance"),

        new(
            PermissionKeys.MaintenanceRecords.View,
            "View Maintenance Records",
            "Maintenance"),

        new(
            PermissionKeys.MaintenanceRecords.Create,
            "Create Maintenance Records",
            "Maintenance"),

        new(
            PermissionKeys.MaintenanceRecords.Edit,
            "Edit Maintenance Records",
            "Maintenance"),

        new(
            PermissionKeys.MaintenanceRecords.Complete,
            "Complete Maintenance Records",
            "Maintenance"),

        new(
            PermissionKeys.MaintenanceRecords.Cancel,
            "Cancel Maintenance Records",
            "Maintenance"),

        new(
            PermissionKeys.MaintenanceTasks.Manage,
            "Manage Maintenance Tasks",
            "Maintenance"),

        new(
            PermissionKeys.MaintenanceParts.Manage,
            "Manage Maintenance Parts",
            "Maintenance"),

        new(
            PermissionKeys.MaintenanceLabour.Manage,
            "Manage Maintenance Labour",
            "Maintenance"),

        new(
            PermissionKeys.MaintenanceExpenses.Manage,
            "Manage Maintenance Expenses",
            "Maintenance"),

        new(
            PermissionKeys.MaintenanceDocuments.View,
            "View Maintenance Documents",
            "Maintenance"),

        new(
            PermissionKeys.MaintenanceDocuments.Manage,
            "Manage Maintenance Documents",
            "Maintenance"),

        new(
            PermissionKeys.MaintenanceDue.View,
            "View Due Maintenance",
            "Maintenance"),

        new(
            PermissionKeys.MaintenanceDashboard.View,
            "View Maintenance Dashboard",
            "Maintenance"),

        new(
            PermissionKeys.MaintenanceProviders.View,
            "View Maintenance Providers",
            "Maintenance"),

        new(
            PermissionKeys.MaintenanceProviders.Manage,
            "Manage Maintenance Providers",
            "Maintenance"),

        new(
            PermissionKeys.MaintenanceServiceTypes.View,
            "View Maintenance Service Types",
            "Maintenance"),

        new(
            PermissionKeys.MaintenanceServiceTypes.Manage,
            "Manage Maintenance Service Types",
            "Maintenance")
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

        // Seed default global tracking providers if not present
        if (!await dbContext.TrackingProviders.IgnoreQueryFilters().AnyAsync(cancellationToken))
        {
            dbContext.TrackingProviders.AddRange(
                new ConnectedOps.Domain.Telematics.TrackingProvider("Teltonika", "TELTONIKA", ConnectedOps.Domain.Telematics.ProviderType.Teltonika, "Teltonika Telematics hardware and Codec 8 protocol"),
                new ConnectedOps.Domain.Telematics.TrackingProvider("Traccar", "TRACCAR", ConnectedOps.Domain.Telematics.ProviderType.Traccar, "Traccar open-source GPS tracking system"),
                new ConnectedOps.Domain.Telematics.TrackingProvider("Queclink", "QUECLINK", ConnectedOps.Domain.Telematics.ProviderType.Queclink, "Queclink wireless tracking devices"),
                new ConnectedOps.Domain.Telematics.TrackingProvider("Ruptela", "RUPTELA", ConnectedOps.Domain.Telematics.ProviderType.Ruptela, "Ruptela fleet management hardware"),
                new ConnectedOps.Domain.Telematics.TrackingProvider("Custom HTTP", "CUSTOM_HTTP", ConnectedOps.Domain.Telematics.ProviderType.Http, "Generic HTTP webhook and REST telematics ingestion"));
        }

        // Seed default global tracking device types if not present
        if (!await dbContext.TrackingDeviceTypes.IgnoreQueryFilters().AnyAsync(cancellationToken))
        {
            dbContext.TrackingDeviceTypes.AddRange(
                new ConnectedOps.Domain.Telematics.TrackingDeviceType("GPS Tracker", "GPS_TRACKER", "Standard hardwired GPS and vehicle tracking unit", supportsGps: true, supportsIgnition: true, supportsBattery: true, supportsCommands: true),
                new ConnectedOps.Domain.Telematics.TrackingDeviceType("OBD Tracker", "OBD_TRACKER", "Plug-and-play OBD-II diagnostic port tracking unit", supportsGps: true, supportsIgnition: true, supportsObd: true, supportsBattery: true),
                new ConnectedOps.Domain.Telematics.TrackingDeviceType("CAN Tracker", "CAN_TRACKER", "Advanced CAN-bus integrated heavy vehicle telemetry tracker", supportsGps: true, supportsIgnition: true, supportsCanBus: true, supportsFuel: true, supportsBattery: true),
                new ConnectedOps.Domain.Telematics.TrackingDeviceType("Asset Tracker", "ASSET_TRACKER", "Autonomous battery-powered asset and trailer tracker", supportsGps: true, supportsIgnition: false, supportsBattery: true),
                new ConnectedOps.Domain.Telematics.TrackingDeviceType("BLE Gateway", "BLE_GATEWAY", "Bluetooth Low Energy sensor and beacon gateway", supportsGps: true, supportsIgnition: true, supportsBle: true, supportsTemperature: true));
        }

        await dbContext.SaveChangesAsync(
            cancellationToken);
    }

    private sealed record PermissionSeed(
        string Key,
        string Name,
        string Module);
}