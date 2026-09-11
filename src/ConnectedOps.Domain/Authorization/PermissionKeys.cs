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
        public const string AssignVehicle = "Drivers.AssignVehicle";
    }

    public static class Assets
    {
        public const string View = "Assets.View";
        public const string Create = "Assets.Create";
        public const string Edit = "Assets.Edit";
        public const string Delete = "Assets.Delete";
    }

    public static class Reports
    {
        public const string View = "Reports.View";
        public const string Export = "Reports.Export";
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
}