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

        // PHASE 10 - ASSET & EQUIPMENT MANAGEMENT
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
            PermissionKeys.Assets.ChangeStatus,
            "Change Asset Status",
            "Assets"),

        new(
            PermissionKeys.Assets.ManageLocation,
            "Manage Asset Location",
            "Assets"),

        new(
            PermissionKeys.AssetCategories.View,
            "View Asset Categories",
            "Assets"),

        new(
            PermissionKeys.AssetCategories.Manage,
            "Manage Asset Categories",
            "Assets"),

        new(
            PermissionKeys.AssetTypes.View,
            "View Asset Types",
            "Assets"),

        new(
            PermissionKeys.AssetTypes.Manage,
            "Manage Asset Types",
            "Assets"),

        new(
            PermissionKeys.AssetCustody.View,
            "View Asset Custody",
            "Assets"),

        new(
            PermissionKeys.AssetCustody.Manage,
            "Manage Asset Custody",
            "Assets"),

        new(
            PermissionKeys.AssetCustody.CheckOut,
            "Check Out Asset",
            "Assets"),

        new(
            PermissionKeys.AssetCustody.CheckIn,
            "Check In Asset",
            "Assets"),

        new(
            PermissionKeys.AssetTransfers.View,
            "View Asset Transfers",
            "Assets"),

        new(
            PermissionKeys.AssetTransfers.Create,
            "Create Asset Transfer",
            "Assets"),

        new(
            PermissionKeys.AssetTransfers.Complete,
            "Complete Asset Transfer",
            "Assets"),

        new(
            PermissionKeys.AssetTransfers.Cancel,
            "Cancel Asset Transfer",
            "Assets"),

        new(
            PermissionKeys.AssetInspections.View,
            "View Asset Inspections",
            "Assets"),

        new(
            PermissionKeys.AssetInspections.Manage,
            "Manage Asset Inspections",
            "Assets"),

        new(
            PermissionKeys.AssetConditions.View,
            "View Asset Conditions",
            "Assets"),

        new(
            PermissionKeys.AssetConditions.Manage,
            "Manage Asset Conditions",
            "Assets"),

        new(
            PermissionKeys.AssetDocuments.View,
            "View Asset Documents",
            "Assets"),

        new(
            PermissionKeys.AssetDocuments.Manage,
            "Manage Asset Documents",
            "Assets"),

        new(
            PermissionKeys.AssetIdentifiers.View,
            "View Asset Identifiers & QR Codes",
            "Assets"),

        new(
            PermissionKeys.AssetIdentifiers.Manage,
            "Manage Asset Identifiers & QR Codes",
            "Assets"),

        new(
            PermissionKeys.AssetDashboard.View,
            "View Asset Dashboard",
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
            "Maintenance"),

        // PHASE 9 - FUEL MANAGEMENT
        new(
            PermissionKeys.Fuel.View,
            "View Fuel Subsystem",
            "Fuel"),

        new(
            PermissionKeys.FuelTransactions.View,
            "View Fuel Transactions",
            "Fuel"),

        new(
            PermissionKeys.FuelTransactions.Create,
            "Create Fuel Transactions",
            "Fuel"),

        new(
            PermissionKeys.FuelTransactions.Edit,
            "Edit Fuel Transactions",
            "Fuel"),

        new(
            PermissionKeys.FuelTransactions.Cancel,
            "Cancel Fuel Transactions",
            "Fuel"),

        new(
            PermissionKeys.FuelTransactions.Import,
            "Import Fuel Transactions",
            "Fuel"),

        new(
            PermissionKeys.FuelStations.View,
            "View Fuel Stations",
            "Fuel"),

        new(
            PermissionKeys.FuelStations.Manage,
            "Manage Fuel Stations",
            "Fuel"),

        new(
            PermissionKeys.FuelCards.View,
            "View Fuel Cards",
            "Fuel"),

        new(
            PermissionKeys.FuelCards.Manage,
            "Manage Fuel Cards",
            "Fuel"),

        new(
            PermissionKeys.FuelAnomalies.View,
            "View Fuel Anomalies",
            "Fuel"),

        new(
            PermissionKeys.FuelAnomalies.Resolve,
            "Resolve Fuel Anomalies",
            "Fuel"),

        new(
            PermissionKeys.FuelAnalytics.View,
            "View Fuel Analytics",
            "Fuel"),

        new(
            PermissionKeys.FuelDashboard.View,
            "View Fuel Dashboard",
            "Fuel"),

        new(
            PermissionKeys.FuelDocuments.View,
            "View Fuel Documents",
            "Fuel"),

        new(
            PermissionKeys.FuelDocuments.Manage,
            "Manage Fuel Documents",
            "Fuel"),

        // PHASE 10 - ASSET MANAGEMENT
        new(PermissionKeys.Assets.View, "View Assets", "Assets"),
        new(PermissionKeys.Assets.Create, "Create Assets", "Assets"),
        new(PermissionKeys.Assets.Edit, "Edit Assets", "Assets"),
        new(PermissionKeys.Assets.Delete, "Delete Assets", "Assets"),
        new(PermissionKeys.Assets.ChangeStatus, "Change Asset Status", "Assets"),
        new(PermissionKeys.Assets.ManageLocation, "Manage Asset Location", "Assets"),
        new(PermissionKeys.AssetCategories.View, "View Asset Categories", "Assets"),
        new(PermissionKeys.AssetCategories.Manage, "Manage Asset Categories", "Assets"),
        new(PermissionKeys.AssetTypes.View, "View Asset Types", "Assets"),
        new(PermissionKeys.AssetTypes.Manage, "Manage Asset Types", "Assets"),
        new(PermissionKeys.AssetCustody.View, "View Asset Custody", "Assets"),
        new(PermissionKeys.AssetCustody.Manage, "Manage Asset Custody", "Assets"),
        new(PermissionKeys.AssetCustody.CheckOut, "Check Out Assets", "Assets"),
        new(PermissionKeys.AssetCustody.CheckIn, "Check In Assets", "Assets"),
        new(PermissionKeys.AssetTransfers.View, "View Asset Transfers", "Assets"),
        new(PermissionKeys.AssetTransfers.Create, "Create Asset Transfers", "Assets"),
        new(PermissionKeys.AssetTransfers.Complete, "Complete Asset Transfers", "Assets"),
        new(PermissionKeys.AssetTransfers.Cancel, "Cancel Asset Transfers", "Assets"),
        new(PermissionKeys.AssetInspections.View, "View Asset Inspections", "Assets"),
        new(PermissionKeys.AssetInspections.Manage, "Manage Asset Inspections", "Assets"),
        new(PermissionKeys.AssetConditions.View, "View Asset Conditions", "Assets"),
        new(PermissionKeys.AssetConditions.Manage, "Manage Asset Conditions", "Assets"),
        new(PermissionKeys.AssetDocuments.View, "View Asset Documents", "Assets"),
        new(PermissionKeys.AssetDocuments.Manage, "Manage Asset Documents", "Assets"),
        new(PermissionKeys.AssetIdentifiers.View, "View Asset Identifiers", "Assets"),
        new(PermissionKeys.AssetIdentifiers.Manage, "Manage Asset Identifiers", "Assets"),
        new(PermissionKeys.AssetDashboard.View, "View Asset Dashboard", "Assets"),

        // PHASE 11 - COMPLIANCE MANAGEMENT
        new(PermissionKeys.Compliance.View, "View Compliance Subsystem", "Compliance"),
        new(PermissionKeys.ComplianceRequirements.View, "View Compliance Requirements", "Compliance"),
        new(PermissionKeys.ComplianceRequirements.Manage, "Manage Compliance Requirements", "Compliance"),
        new(PermissionKeys.ComplianceRecords.View, "View Compliance Records", "Compliance"),
        new(PermissionKeys.ComplianceRecords.Create, "Create Compliance Records", "Compliance"),
        new(PermissionKeys.ComplianceRecords.Edit, "Edit Compliance Records", "Compliance"),
        new(PermissionKeys.ComplianceRecords.Verify, "Verify Compliance Records", "Compliance"),
        new(PermissionKeys.ComplianceRecords.Delete, "Delete Compliance Records", "Compliance"),
        new(PermissionKeys.ComplianceExceptions.View, "View Compliance Exceptions", "Compliance"),
        new(PermissionKeys.ComplianceExceptions.Manage, "Manage Compliance Exceptions", "Compliance"),
        new(PermissionKeys.ComplianceDashboard.View, "View Compliance Dashboard", "Compliance"),

        // PHASE 11 - SAFETY MANAGEMENT
        new(PermissionKeys.Safety.View, "View Safety Subsystem", "Safety"),
        new(PermissionKeys.SafetyIncidents.View, "View Safety Incidents", "Safety"),
        new(PermissionKeys.SafetyIncidents.Create, "Create Safety Incidents", "Safety"),
        new(PermissionKeys.SafetyIncidents.Edit, "Edit Safety Incidents", "Safety"),
        new(PermissionKeys.SafetyIncidents.Close, "Close Safety Incidents", "Safety"),
        new(PermissionKeys.SafetyIncidents.Cancel, "Cancel Safety Incidents", "Safety"),
        new(PermissionKeys.SafetyInvestigations.View, "View Safety Investigations", "Safety"),
        new(PermissionKeys.SafetyInvestigations.Manage, "Manage Safety Investigations", "Safety"),
        new(PermissionKeys.SafetyViolations.View, "View Safety Violations", "Safety"),
        new(PermissionKeys.SafetyViolations.Create, "Create Safety Violations", "Safety"),
        new(PermissionKeys.SafetyViolations.Edit, "Edit Safety Violations", "Safety"),
        new(PermissionKeys.SafetyViolations.Resolve, "Resolve Safety Violations", "Safety"),
        new(PermissionKeys.CorrectiveActions.View, "View Corrective Actions", "Safety"),
        new(PermissionKeys.CorrectiveActions.Manage, "Manage Corrective Actions", "Safety"),
        new(PermissionKeys.CorrectiveActions.Verify, "Verify Corrective Actions", "Safety"),
        new(PermissionKeys.SafetyDashboard.View, "View Safety Dashboard", "Safety")
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

        // Seed default global fuel type definitions if not present
        if (!await dbContext.FuelTypeDefinitions.IgnoreQueryFilters().AnyAsync(cancellationToken))
        {
            dbContext.FuelTypeDefinitions.AddRange(
                new ConnectedOps.Domain.Fuel.FuelTypeDefinition(null, "DIESEL", "Diesel", ConnectedOps.Domain.Vehicles.FuelType.Diesel, "Liquid", ConnectedOps.Domain.Fuel.FuelUnit.Liter, 0.832m),
                new ConnectedOps.Domain.Fuel.FuelTypeDefinition(null, "GAS_REG", "Gasoline Regular (87/91)", ConnectedOps.Domain.Vehicles.FuelType.Gasoline, "Liquid", ConnectedOps.Domain.Fuel.FuelUnit.Liter, 0.745m),
                new ConnectedOps.Domain.Fuel.FuelTypeDefinition(null, "GAS_PREM", "Gasoline Premium (93/98)", ConnectedOps.Domain.Vehicles.FuelType.Gasoline, "Liquid", ConnectedOps.Domain.Fuel.FuelUnit.Liter, 0.755m),
                new ConnectedOps.Domain.Fuel.FuelTypeDefinition(null, "BIODIESEL", "Biodiesel (B20)", ConnectedOps.Domain.Vehicles.FuelType.Biodiesel, "Liquid", ConnectedOps.Domain.Fuel.FuelUnit.Liter, 0.880m),
                new ConnectedOps.Domain.Fuel.FuelTypeDefinition(null, "ETHANOL_E85", "Ethanol E85", ConnectedOps.Domain.Vehicles.FuelType.Ethanol, "Liquid", ConnectedOps.Domain.Fuel.FuelUnit.Liter, 0.789m),
                new ConnectedOps.Domain.Fuel.FuelTypeDefinition(null, "CNG", "Compressed Natural Gas", ConnectedOps.Domain.Vehicles.FuelType.Cng, "Gas", ConnectedOps.Domain.Fuel.FuelUnit.CubicMeter, 0.128m),
                new ConnectedOps.Domain.Fuel.FuelTypeDefinition(null, "LPG", "Liquefied Petroleum Gas", ConnectedOps.Domain.Vehicles.FuelType.Lpg, "Gas", ConnectedOps.Domain.Fuel.FuelUnit.Liter, 0.540m),
                new ConnectedOps.Domain.Fuel.FuelTypeDefinition(null, "ELECTRIC", "Electricity (EV)", ConnectedOps.Domain.Vehicles.FuelType.Electric, "Electric", ConnectedOps.Domain.Fuel.FuelUnit.KilowattHour, null),
                new ConnectedOps.Domain.Fuel.FuelTypeDefinition(null, "HYDROGEN", "Hydrogen (H2 Fuel Cell)", ConnectedOps.Domain.Vehicles.FuelType.Hydrogen, "Gas", ConnectedOps.Domain.Fuel.FuelUnit.Kilogram, 0.089m));
        }

        await dbContext.SaveChangesAsync(
            cancellationToken);
    }

    private sealed record PermissionSeed(
        string Key,
        string Name,
        string Module);
}