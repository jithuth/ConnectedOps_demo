namespace ConnectedOps.Domain.Maintenance;

public enum MaintenanceServiceCategory
{
    Preventive = 1,
    Corrective = 2,
    Inspection = 3,
    Lubrication = 4,
    Tyre = 5,
    Battery = 6,
    Brake = 7,
    Electrical = 8,
    Engine = 9,
    Transmission = 10,
    AirConditioning = 11,
    Body = 12,
    Safety = 13,
    Other = 99
}

public enum MaintenanceProviderType
{
    InternalWorkshop = 1,
    ExternalWorkshop = 2,
    Dealer = 3,
    MobileService = 4,
    Other = 99
}

public enum MaintenanceScheduleType
{
    Distance = 1,
    EngineHours = 2,
    Calendar = 3,
    DistanceOrCalendar = 4,
    EngineHoursOrCalendar = 5,
    DistanceAndCalendar = 6,
    Custom = 99
}

public enum MaintenanceDueStatus
{
    NotDue = 1,
    Upcoming = 2,
    Due = 3,
    Overdue = 4,
    Completed = 5,
    NotApplicable = 6
}

public enum VehicleMaintenanceType
{
    Scheduled = 1,
    Preventive = 2,
    Corrective = 3,
    Inspection = 4,
    Emergency = 5,
    Other = 99
}

public enum MaintenanceRecordStatus
{
    Draft = 1,
    Scheduled = 2,
    InProgress = 3,
    Completed = 4,
    Cancelled = 5
}

public enum MaintenanceTaskStatus
{
    Pending = 1,
    InProgress = 2,
    Completed = 3,
    Skipped = 4,
    NotApplicable = 5
}

public enum MaintenanceExpenseType
{
    Towing = 1,
    InspectionFee = 2,
    ExternalService = 3,
    Transport = 4,
    Miscellaneous = 99
}

public enum DowntimeType
{
    Maintenance = 1,
    Breakdown = 2,
    Inspection = 3,
    Accident = 4,
    Other = 99
}

public enum MaintenanceDocumentType
{
    Invoice = 1,
    Receipt = 2,
    InspectionReport = 3,
    ServiceReport = 4,
    Photo = 5,
    WarrantyDocument = 6,
    Other = 99
}
