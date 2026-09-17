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
            PermissionKeys.Reports.ViewDashboard,
            "View Executive BI Dashboard",
            "Reports"),

        new(
            PermissionKeys.Reports.ViewTco,
            "View Fleet Total Cost of Ownership",
            "Reports"),

        new(
            PermissionKeys.Reports.ViewUtilization,
            "View Asset & Fleet Utilization",
            "Reports"),

        new(
            PermissionKeys.Reports.ViewEsg,
            "View ESG Carbon Analytics",
            "Reports"),

        new(
            PermissionKeys.Reports.Export,
            "Export Reports",
            "Reports"),

        new(
            PermissionKeys.Dispatch.View,
            "View Dispatch",
            "Dispatch"),

        new(
            PermissionKeys.Dispatch.ViewDashboard,
            "View Dispatch Dashboard",
            "Dispatch"),

        new(
            PermissionKeys.Dispatch.ViewJobs,
            "View Dispatch Jobs",
            "Dispatch"),

        new(
            PermissionKeys.Dispatch.CreateJob,
            "Create Dispatch Job",
            "Dispatch"),

        new(
            PermissionKeys.Dispatch.EditJob,
            "Edit Dispatch Job",
            "Dispatch"),

        new(
            PermissionKeys.Dispatch.CancelJob,
            "Cancel Dispatch Job",
            "Dispatch"),

        new(
            PermissionKeys.Dispatch.ViewRoutes,
            "View Dispatch Routes",
            "Dispatch"),

        new(
            PermissionKeys.Dispatch.CreateRoute,
            "Create Dispatch Route",
            "Dispatch"),

        new(
            PermissionKeys.Dispatch.EditRoute,
            "Edit Dispatch Route",
            "Dispatch"),

        new(
            PermissionKeys.Dispatch.DispatchRoute,
            "Dispatch Route",
            "Dispatch"),

        new(
            PermissionKeys.Dispatch.CompletePod,
            "Complete Proof of Delivery",
            "Dispatch"),

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
        new(PermissionKeys.SafetyDashboard.View, "View Safety Dashboard", "Safety"),

        // PHASE 14 - OPERATIONAL INTELLIGENCE SUITE
        // Alerts & Escalations
        new(PermissionKeys.Alerts.View, "View Alerts", "Alerts"),
        new(PermissionKeys.Alerts.Acknowledge, "Acknowledge Alerts", "Alerts"),
        new(PermissionKeys.Alerts.Assign, "Assign Alerts", "Alerts"),
        new(PermissionKeys.Alerts.Resolve, "Resolve Alerts", "Alerts"),
        new(PermissionKeys.Alerts.Dismiss, "Dismiss Alerts", "Alerts"),
        new(PermissionKeys.AlertRules.View, "View Alert Rules", "Alerts"),
        new(PermissionKeys.AlertRules.Create, "Create Alert Rules", "Alerts"),
        new(PermissionKeys.AlertRules.Edit, "Edit Alert Rules", "Alerts"),
        new(PermissionKeys.AlertRules.EnableDisable, "Enable/Disable Alert Rules", "Alerts"),
        new(PermissionKeys.AlertDashboard.View, "View Alert Dashboard", "Alerts"),

        // DVIR (Driver Vehicle Inspection Reports)
        new(PermissionKeys.Dvir.View, "View DVIR Inspections", "Dvir"),
        new(PermissionKeys.Dvir.Create, "Create DVIR Inspections", "Dvir"),
        new(PermissionKeys.Dvir.Edit, "Edit DVIR Inspections", "Dvir"),
        new(PermissionKeys.Dvir.SignOff, "Mechanic Sign-Off DVIR", "Dvir"),
        new(PermissionKeys.Dvir.ManageTemplates, "Manage DVIR Templates", "Dvir"),

        // Tolls & Traffic Violations
        new(PermissionKeys.TollsAndFines.View, "View Tolls and Violations", "TollsAndFines"),
        new(PermissionKeys.TollsAndFines.Create, "Create Tolls and Violations", "TollsAndFines"),
        new(PermissionKeys.TollsAndFines.Edit, "Edit Tolls and Violations", "TollsAndFines"),
        new(PermissionKeys.TollsAndFines.AssignDriver, "Assign Driver Liability", "TollsAndFines"),
        new(PermissionKeys.TollsAndFines.ResolveDispute, "Resolve Violation Disputes", "TollsAndFines"),
        new(PermissionKeys.TollsAndFines.Export, "Export Tolls and Violations", "TollsAndFines"),

        // Cold Chain Environmental Monitoring
        new(PermissionKeys.ColdChain.View, "View Cold Chain Monitoring", "ColdChain"),
        new(PermissionKeys.ColdChain.ManageSensors, "Manage Cargo Sensors", "ColdChain"),
        new(PermissionKeys.ColdChain.ViewExcursions, "View Cold Chain Excursions", "ColdChain"),
        new(PermissionKeys.ColdChain.ConfigureThresholds, "Configure Reefer Thresholds", "ColdChain"),

        // Phase 15: Hours of Service (HOS) & ELD
        new(PermissionKeys.Hos.View, "View Hours of Service Logs", "Hos"),
        new(PermissionKeys.Hos.LogDuty, "Record Duty Status", "Hos"),
        new(PermissionKeys.Hos.ConfigurePolicy, "Configure HOS Policy", "Hos"),
        new(PermissionKeys.Hos.ViewViolations, "View HOS Violations", "Hos"),
        new(PermissionKeys.Hos.RoadsideInspection, "Roadside Inspection Export", "Hos"),

        // Phase 15: Driver Gamification & Scorecards
        new(PermissionKeys.Gamification.ViewLeaderboard, "View Gamification Leaderboard", "Gamification"),
        new(PermissionKeys.Gamification.ViewScorecards, "View Driver Scorecards", "Gamification"),
        new(PermissionKeys.Gamification.ManageBadges, "Manage Driver Badges", "Gamification"),

        // Phase 15: Driver Trip Expenses
        new(PermissionKeys.DriverExpenses.View, "View Driver Expenses", "DriverExpenses"),
        new(PermissionKeys.DriverExpenses.Submit, "Submit Driver Expenses", "DriverExpenses"),
        new(PermissionKeys.DriverExpenses.Approve, "Approve Driver Expenses", "DriverExpenses"),
        new(PermissionKeys.DriverExpenses.Reimburse, "Reimburse Driver Expenses", "DriverExpenses"),

        // PHASE 16 - EXTERNAL INTEGRATIONS, WEBHOOKS & OPEN API GATEWAY
        new(PermissionKeys.Integrations.View, "View External Integrations", "Integrations"),
        new(PermissionKeys.Integrations.ManageWebhooks, "Manage Webhooks", "Integrations"),
        new(PermissionKeys.Integrations.ManageApiKeys, "Manage API Keys", "Integrations"),
        new(PermissionKeys.Integrations.ExportErp, "Export ERP General Ledger", "Integrations"),
        new(PermissionKeys.Integrations.SyncFuelFeeds, "Sync Fuel Clearinghouse Feeds", "Integrations"),

        // PHASE 17 - AI PREDICTIVE FLEET MAINTENANCE & SUBSYSTEM HEALTH
        new(PermissionKeys.PredictiveMaintenance.View, "View Predictive Maintenance", "PredictiveMaintenance"),
        new(PermissionKeys.PredictiveMaintenance.RunDiagnostics, "Run Fleet AI Diagnostics", "PredictiveMaintenance"),
        new(PermissionKeys.PredictiveMaintenance.GenerateWorkOrder, "Promote to Work Order", "PredictiveMaintenance"),
        new(PermissionKeys.PredictiveMaintenance.Dismiss, "Dismiss Predictive Alerts", "PredictiveMaintenance")
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

        // Seed default global ESG emission factors if not present
        if (!await dbContext.EsgEmissionFactors.IgnoreQueryFilters().AnyAsync(cancellationToken))
        {
            dbContext.EsgEmissionFactors.AddRange(
                new ConnectedOps.Domain.Reports.EsgEmissionFactor(null, "DIESEL", "Diesel Fuel", ConnectedOps.Domain.Vehicles.FuelType.Diesel, 2.68m, "Liter", "EPA GHG Protocol"),
                new ConnectedOps.Domain.Reports.EsgEmissionFactor(null, "GAS_REG", "Gasoline Regular", ConnectedOps.Domain.Vehicles.FuelType.Gasoline, 2.31m, "Liter", "EPA GHG Protocol"),
                new ConnectedOps.Domain.Reports.EsgEmissionFactor(null, "GAS_PREM", "Gasoline Premium", ConnectedOps.Domain.Vehicles.FuelType.Gasoline, 2.35m, "Liter", "EPA GHG Protocol"),
                new ConnectedOps.Domain.Reports.EsgEmissionFactor(null, "BIODIESEL", "Biodiesel (B20)", ConnectedOps.Domain.Vehicles.FuelType.Biodiesel, 0.72m, "Liter", "EPA GHG Protocol"),
                new ConnectedOps.Domain.Reports.EsgEmissionFactor(null, "CNG", "Compressed Natural Gas", ConnectedOps.Domain.Vehicles.FuelType.Cng, 1.98m, "CubicMeter", "EPA GHG Protocol"),
                new ConnectedOps.Domain.Reports.EsgEmissionFactor(null, "LPG", "Liquefied Petroleum Gas", ConnectedOps.Domain.Vehicles.FuelType.Lpg, 1.51m, "Liter", "EPA GHG Protocol"),
                new ConnectedOps.Domain.Reports.EsgEmissionFactor(null, "ELECTRIC", "Grid Electricity (EV Scope 1)", ConnectedOps.Domain.Vehicles.FuelType.Electric, 0.00m, "kWh", "Zero Direct Tailpipe Emission"),
                new ConnectedOps.Domain.Reports.EsgEmissionFactor(null, "HYDROGEN", "Hydrogen Fuel Cell", ConnectedOps.Domain.Vehicles.FuelType.Hydrogen, 0.00m, "kg", "Zero Direct Tailpipe Emission"));
        }

        // Seed default report definitions if not present
        if (!await dbContext.ReportDefinitions.IgnoreQueryFilters().AnyAsync(cancellationToken))
        {
            dbContext.ReportDefinitions.AddRange(
                new ConnectedOps.Domain.Reports.ReportDefinition(null, "FLEET_TCO", "Fleet Total Cost of Ownership (TCO)", ConnectedOps.Domain.Reports.ReportCategory.Financial, "Calculates capital acquisition plus operational spend (fuel, maintenance, incidents) on a per-vehicle and per-kilometer basis.", null, true, true),
                new ConnectedOps.Domain.Reports.ReportDefinition(null, "EXECUTIVE_KPI", "Executive Fleet Performance Summary", ConnectedOps.Domain.Reports.ReportCategory.Executive, "Consolidated C-suite overview of total expenditure, fleet uptime, cost efficiency, safety index, and carbon footprint.", null, true, true),
                new ConnectedOps.Domain.Reports.ReportDefinition(null, "UTILIZATION", "Asset & Fleet Utilization Analytics", ConnectedOps.Domain.Reports.ReportCategory.Operational, "Detailed analysis of operating hours, active vs idle ratios, and day-of-week demand heatmaps.", null, true, true),
                new ConnectedOps.Domain.Reports.ReportDefinition(null, "ESG_CARBON", "ESG Scope 1 Carbon Emissions Report", ConnectedOps.Domain.Reports.ReportCategory.Sustainability, "Greenhouse Gas Protocol (GHG) calculations of direct fuel combustion emissions and EV transition potential.", null, true, true),
                new ConnectedOps.Domain.Reports.ReportDefinition(null, "MAINT_COST", "Preventive vs Corrective Maintenance Analysis", ConnectedOps.Domain.Reports.ReportCategory.Financial, "Breaks down workshop labour, replacement parts, scheduled services, and unplanned breakdown expenses.", null, true, true),
                new ConnectedOps.Domain.Reports.ReportDefinition(null, "DRIVER_RISK", "Driver Safety & Telematics Risk Index", ConnectedOps.Domain.Reports.ReportCategory.Safety, "Evaluates driver safety compliance, speeding incidents, harsh maneuvers, and accident histories.", null, true, true));
        }

        await dbContext.SaveChangesAsync(
            cancellationToken);
    }

    private sealed record PermissionSeed(
        string Key,
        string Name,
        string Module);
}