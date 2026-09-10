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

    public static class Vehicles
    {
        public const string View = "Vehicles.View";
        public const string Create = "Vehicles.Create";
        public const string Edit = "Vehicles.Edit";
        public const string Delete = "Vehicles.Delete";
        public const string AssignDriver = "Vehicles.AssignDriver";
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
}