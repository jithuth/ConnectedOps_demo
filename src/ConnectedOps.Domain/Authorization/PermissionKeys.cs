namespace ConnectedOps.Domain.Authorization;

public static class PermissionKeys
{
    public static class Tenants
    {
        public const string View = "Tenants.View";
        public const string Manage = "Tenants.Manage";
    }

    public static class AuditLogs
    {
        public const string View =
            "AuditLogs.View";
    }

    public static class SecurityLogs
    {
        public const string View =
            "SecurityLogs.View";
    }

    public static class Users
    {
        public const string View = "Users.View";
        public const string Create = "Users.Create";
        public const string Edit = "Users.Edit";
        public const string Delete = "Users.Delete";
        public const string ManageRoles = "Users.ManageRoles";
    }

    public static class Organization
    {
        public const string View = "Organization.View";
        public const string Manage = "Organization.Manage";
    }

    public static class Branches
    {
        public const string View = "Branches.View";
        public const string Manage = "Branches.Manage";
    }

    public static class Locations
    {
        public const string View = "Locations.View";
        public const string Manage = "Locations.Manage";
    }

    public static class Departments
    {
        public const string View = "Departments.View";
        public const string Manage = "Departments.Manage";
    }

    public static class Teams
    {
        public const string View = "Teams.View";
        public const string Manage = "Teams.Manage";
    }

    public static class Employees
    {
        public const string View = "Employees.View";
        public const string Create = "Employees.Create";
        public const string Edit = "Employees.Edit";
        public const string Delete = "Employees.Delete";
    }

    public static class OrganizationDashboard
    {
        public const string View = "OrganizationDashboard.View";
    }

    public static class Vehicles
    {
        public const string View = "Vehicles.View";
        public const string Create = "Vehicles.Create";
        public const string Edit = "Vehicles.Edit";
        public const string Delete = "Vehicles.Delete";
        public const string AssignDriver = "Vehicles.AssignDriver";
        public const string ManageStatus = "Vehicles.ManageStatus";
        public const string ManageOdometer = "Vehicles.ManageOdometer";
        public const string ManageDocuments = "Vehicles.ManageDocuments";
        public const string ManageNotes = "Vehicles.ManageNotes";
    }

    public static class VehicleCategories
    {
        public const string View = "VehicleCategories.View";
        public const string Manage = "VehicleCategories.Manage";
    }

    public static class VehicleMakes
    {
        public const string View = "VehicleMakes.View";
        public const string Manage = "VehicleMakes.Manage";
    }

    public static class VehicleModels
    {
        public const string View = "VehicleModels.View";
        public const string Manage = "VehicleModels.Manage";
    }

    public static class VehicleDashboard
    {
        public const string View = "VehicleDashboard.View";
    }

    public static class Drivers
    {
        public const string View = "Drivers.View";
        public const string Create = "Drivers.Create";
        public const string Edit = "Drivers.Edit";
        public const string Delete = "Drivers.Delete";
        public const string ChangeStatus = "Drivers.ChangeStatus";
        public const string ManageOrganization = "Drivers.ManageOrganization";
        public const string LinkEmployee = "Drivers.LinkEmployee";
        public const string AssignVehicle = "Drivers.AssignVehicle";
    }

    public static class DriverLicenses
    {
        public const string View = "DriverLicenses.View";
        public const string Manage = "DriverLicenses.Manage";
    }

    public static class DriverCertifications
    {
        public const string View = "DriverCertifications.View";
        public const string Manage = "DriverCertifications.Manage";
    }

    public static class DriverDocuments
    {
        public const string View = "DriverDocuments.View";
        public const string Manage = "DriverDocuments.Manage";
    }

    public static class DriverAssignments
    {
        public const string View = "DriverAssignments.View";
        public const string Create = "DriverAssignments.Create";
        public const string End = "DriverAssignments.End";
    }

    public static class DriverDashboard
    {
        public const string View = "DriverDashboard.View";
    }

    public static class FleetOperations
    {
        public const string View = "FleetOperations.View";
        public const string ViewBoard = "FleetOperations.ViewBoard";
        public const string ViewDashboard = "FleetOperations.ViewDashboard";
        public const string ManageOperations = "FleetOperations.ManageOperations";
        public const string CheckOut = "FleetOperations.CheckOut";
        public const string Checkout = "FleetOperations.CheckOut";
        public const string CheckIn = "FleetOperations.CheckIn";
        public const string Handover = "FleetOperations.Handover";
        public const string ViewSessions = "FleetOperations.ViewSessions";
        public const string ManageShifts = "FleetOperations.ManageShifts";
        public const string AssignShifts = "FleetOperations.AssignShifts";
        public const string ViewAvailability = "FleetOperations.ViewAvailability";
        public const string ViewExceptions = "FleetOperations.ViewExceptions";
        public const string ManageExceptions = "FleetOperations.ManageExceptions";
        public const string ResolveExceptions = "FleetOperations.ResolveExceptions";
        public const string ViewTimeline = "FleetOperations.ViewTimeline";
    }


    public static class Reports
    {
        public const string View = "Reports.View";
        public const string ViewDashboard = "Reports.ViewDashboard";
        public const string ViewTco = "Reports.ViewTco";
        public const string ViewUtilization = "Reports.ViewUtilization";
        public const string ViewEsg = "Reports.ViewEsg";
        public const string Export = "Reports.Export";
    }

    public static class Dispatch
    {
        public const string View = "Dispatch.View";
        public const string ViewDashboard = "Dispatch.ViewDashboard";
        public const string ViewJobs = "Dispatch.ViewJobs";
        public const string CreateJob = "Dispatch.CreateJob";
        public const string EditJob = "Dispatch.EditJob";
        public const string CancelJob = "Dispatch.CancelJob";
        public const string ViewRoutes = "Dispatch.ViewRoutes";
        public const string CreateRoute = "Dispatch.CreateRoute";
        public const string EditRoute = "Dispatch.EditRoute";
        public const string DispatchRoute = "Dispatch.DispatchRoute";
        public const string CompletePod = "Dispatch.CompletePod";
    }

    public static class Subscriptions
    {
        public const string View = "Subscriptions.View";
        public const string Manage = "Subscriptions.Manage";
    }

    public static class Invoices
    {
        public const string View = "Invoices.View";
        public const string Create = "Invoices.Create";
        public const string Manage = "Invoices.Manage";
    }

    public static class Payments
    {
        public const string View = "Payments.View";
        public const string Create = "Payments.Create";
        public const string Manage = "Payments.Manage";
    }

    public static class Accounting
    {
        public const string View = "Accounting.View";
        public const string Manage = "Accounting.Manage";
    }

    public static class TrackingDevices
    {
        public const string View = "TrackingDevices.View";
        public const string Create = "TrackingDevices.Create";
        public const string Edit = "TrackingDevices.Edit";
        public const string Delete = "TrackingDevices.Delete";
        public const string Provision = "TrackingDevices.Provision";
        public const string AssignVehicle = "TrackingDevices.AssignVehicle";
    }

    public static class Telematics
    {
        public const string View = "Telematics.View";
        public const string ViewLive = "Telematics.ViewLive";
        public const string ViewHistory = "Telematics.ViewHistory";
        public const string ViewDeviceHealth = "Telematics.ViewDeviceHealth";
        public const string ViewDashboard = "Telematics.ViewDashboard";
        public const string SendCommands = "Telematics.SendCommands";
    }

    public static class Maps
    {
        public const string View = "Maps.View";
        public const string ViewLive = "Maps.ViewLive";
        public const string ViewHistory = "Maps.ViewHistory";
    }

    public static class Geofences
    {
        public const string View = "Geofences.View";
        public const string Create = "Geofences.Create";
        public const string Edit = "Geofences.Edit";
        public const string Delete = "Geofences.Delete";
        public const string ViewEvents = "Geofences.ViewEvents";
    }

    public static class DemoFleet
    {
        public const string View = "DemoFleet.View";
        public const string Manage = "DemoFleet.Manage";
    }

    public static class Maintenance
    {
        public const string View = "Maintenance.View";
    }

    public static class MaintenancePlans
    {
        public const string View = "MaintenancePlans.View";
        public const string Create = "MaintenancePlans.Create";
        public const string Edit = "MaintenancePlans.Edit";
        public const string Delete = "MaintenancePlans.Delete";
    }

    public static class MaintenanceRecords
    {
        public const string View = "MaintenanceRecords.View";
        public const string Create = "MaintenanceRecords.Create";
        public const string Edit = "MaintenanceRecords.Edit";
        public const string Complete = "MaintenanceRecords.Complete";
        public const string Cancel = "MaintenanceRecords.Cancel";
    }

    public static class MaintenanceTasks
    {
        public const string Manage = "MaintenanceTasks.Manage";
    }

    public static class MaintenanceParts
    {
        public const string Manage = "MaintenanceParts.Manage";
    }

    public static class MaintenanceLabour
    {
        public const string Manage = "MaintenanceLabour.Manage";
    }

    public static class MaintenanceExpenses
    {
        public const string Manage = "MaintenanceExpenses.Manage";
    }

    public static class MaintenanceDocuments
    {
        public const string View = "MaintenanceDocuments.View";
        public const string Manage = "MaintenanceDocuments.Manage";
    }

    public static class MaintenanceDue
    {
        public const string View = "MaintenanceDue.View";
    }

    public static class MaintenanceDashboard
    {
        public const string View = "MaintenanceDashboard.View";
    }

    public static class MaintenanceProviders
    {
        public const string View = "MaintenanceProviders.View";
        public const string Manage = "MaintenanceProviders.Manage";
    }

    public static class MaintenanceServiceTypes
    {
        public const string View = "MaintenanceServiceTypes.View";
        public const string Manage = "MaintenanceServiceTypes.Manage";
    }

    public static class Fuel
    {
        public const string View = "Fuel.View";
    }

    public static class FuelTransactions
    {
        public const string View = "FuelTransactions.View";
        public const string Create = "FuelTransactions.Create";
        public const string Edit = "FuelTransactions.Edit";
        public const string Cancel = "FuelTransactions.Cancel";
        public const string Import = "FuelTransactions.Import";
    }

    public static class FuelStations
    {
        public const string View = "FuelStations.View";
        public const string Manage = "FuelStations.Manage";
    }

    public static class FuelCards
    {
        public const string View = "FuelCards.View";
        public const string Manage = "FuelCards.Manage";
    }

    public static class FuelImports
    {
        public const string View = "FuelImports.View";
        public const string Manage = "FuelImports.Manage";
    }

    public static class FuelAnalytics
    {
        public const string View = "FuelAnalytics.View";
    }

    public static class FuelAnomalies
    {
        public const string View = "FuelAnomalies.View";
        public const string Resolve = "FuelAnomalies.Resolve";
    }

    public static class FuelDashboard
    {
        public const string View = "FuelDashboard.View";
    }

    public static class FuelDocuments
    {
        public const string View = "FuelDocuments.View";
        public const string Manage = "FuelDocuments.Manage";
    }

    public static class Assets
    {
        public const string View = "Assets.View";
        public const string Create = "Assets.Create";
        public const string Edit = "Assets.Edit";
        public const string Delete = "Assets.Delete";
        public const string ChangeStatus = "Assets.ChangeStatus";
        public const string ManageLocation = "Assets.ManageLocation";
    }

    public static class AssetCategories
    {
        public const string View = "AssetCategories.View";
        public const string Manage = "AssetCategories.Manage";
    }

    public static class AssetTypes
    {
        public const string View = "AssetTypes.View";
        public const string Manage = "AssetTypes.Manage";
    }

    public static class AssetCustody
    {
        public const string View = "AssetCustody.View";
        public const string Manage = "AssetCustody.Manage";
        public const string CheckOut = "AssetCustody.CheckOut";
        public const string CheckIn = "AssetCustody.CheckIn";
    }

    public static class AssetTransfers
    {
        public const string View = "AssetTransfers.View";
        public const string Create = "AssetTransfers.Create";
        public const string Complete = "AssetTransfers.Complete";
        public const string Cancel = "AssetTransfers.Cancel";
    }

    public static class AssetInspections
    {
        public const string View = "AssetInspections.View";
        public const string Manage = "AssetInspections.Manage";
    }

    public static class AssetConditions
    {
        public const string View = "AssetConditions.View";
        public const string Manage = "AssetConditions.Manage";
    }

    public static class AssetDocuments
    {
        public const string View = "AssetDocuments.View";
        public const string Manage = "AssetDocuments.Manage";
    }

    public static class AssetIdentifiers
    {
        public const string View = "AssetIdentifiers.View";
        public const string Manage = "AssetIdentifiers.Manage";
    }

    public static class AssetDashboard
    {
        public const string View = "AssetDashboard.View";
    }

    public static class Compliance
    {
        public const string View = "Compliance.View";
    }

    public static class ComplianceRequirements
    {
        public const string View = "ComplianceRequirements.View";
        public const string Manage = "ComplianceRequirements.Manage";
    }

    public static class ComplianceRecords
    {
        public const string View = "ComplianceRecords.View";
        public const string Create = "ComplianceRecords.Create";
        public const string Edit = "ComplianceRecords.Edit";
        public const string Verify = "ComplianceRecords.Verify";
        public const string Delete = "ComplianceRecords.Delete";
    }

    public static class ComplianceExceptions
    {
        public const string View = "ComplianceExceptions.View";
        public const string Manage = "ComplianceExceptions.Manage";
    }

    public static class ComplianceDashboard
    {
        public const string View = "ComplianceDashboard.View";
    }

    public static class Safety
    {
        public const string View = "Safety.View";
    }

    public static class SafetyIncidents
    {
        public const string View = "SafetyIncidents.View";
        public const string Create = "SafetyIncidents.Create";
        public const string Edit = "SafetyIncidents.Edit";
        public const string Close = "SafetyIncidents.Close";
        public const string Cancel = "SafetyIncidents.Cancel";
    }

    public static class SafetyInvestigations
    {
        public const string View = "SafetyInvestigations.View";
        public const string Manage = "SafetyInvestigations.Manage";
    }

    public static class SafetyViolations
    {
        public const string View = "SafetyViolations.View";
        public const string Create = "SafetyViolations.Create";
        public const string Edit = "SafetyViolations.Edit";
        public const string Resolve = "SafetyViolations.Resolve";
    }

    public static class CorrectiveActions
    {
        public const string View = "CorrectiveActions.View";
        public const string Manage = "CorrectiveActions.Manage";
        public const string Verify = "CorrectiveActions.Verify";
    }

    public static class SafetyDashboard
    {
        public const string View = "SafetyDashboard.View";
    }

    public static class Alerts
    {
        public const string View = "Alerts.View";
        public const string Acknowledge = "Alerts.Acknowledge";
        public const string Assign = "Alerts.Assign";
        public const string Resolve = "Alerts.Resolve";
        public const string Dismiss = "Alerts.Dismiss";
    }

    public static class AlertRules
    {
        public const string View = "AlertRules.View";
        public const string Create = "AlertRules.Create";
        public const string Edit = "AlertRules.Edit";
        public const string EnableDisable = "AlertRules.EnableDisable";
    }

    public static class AlertSuppressions
    {
        public const string View = "AlertSuppressions.View";
        public const string Manage = "AlertSuppressions.Manage";
    }

    public static class AlertEscalations
    {
        public const string View = "AlertEscalations.View";
        public const string Manage = "AlertEscalations.Manage";
    }

    public static class AlertDashboard
    {
        public const string View = "AlertDashboard.View";
    }

    public static class Notifications
    {
        public const string ViewOwn = "Notifications.ViewOwn";
        public const string ViewTenantHistory = "Notifications.ViewTenantHistory";
        public const string ManageTemplates = "Notifications.ManageTemplates";
        public const string ManageProviders = "Notifications.ManageProviders";
        public const string ViewDashboard = "Notifications.ViewDashboard";
    }

    public static class NotificationPreferences
    {
        public const string ManageOwn = "NotificationPreferences.ManageOwn";
    }

    public static class Dvir
    {
        public const string View = "Dvir.View";
        public const string Create = "Dvir.Create";
        public const string Edit = "Dvir.Edit";
        public const string SignOff = "Dvir.SignOff";
        public const string ManageTemplates = "Dvir.ManageTemplates";
    }

    public static class TollsAndFines
    {
        public const string View = "TollsAndFines.View";
        public const string Create = "TollsAndFines.Create";
        public const string Edit = "TollsAndFines.Edit";
        public const string AssignDriver = "TollsAndFines.AssignDriver";
        public const string ResolveDispute = "TollsAndFines.ResolveDispute";
        public const string Export = "TollsAndFines.Export";
    }

    public static class ColdChain
    {
        public const string View = "ColdChain.View";
        public const string ManageSensors = "ColdChain.ManageSensors";
        public const string ViewExcursions = "ColdChain.ViewExcursions";
        public const string ConfigureThresholds = "ColdChain.ConfigureThresholds";
    }
}