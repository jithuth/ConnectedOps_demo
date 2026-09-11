namespace ConnectedOps.Domain.Fuel;

public enum FuelUnit
{
    Liter = 1,
    GallonUS = 2,
    GallonImperial = 3,
    Kilogram = 4,
    KilowattHour = 5,
    CubicMeter = 6,
    Other = 99
}

public enum FuelStationType
{
    Internal = 1,
    External = 2,
    MobileFuel = 3,
    Supplier = 4,
    Other = 99
}

public enum FuelCardStatus
{
    Active = 1,
    Suspended = 2,
    Expired = 3,
    Cancelled = 4
}

public enum FuelTransactionStatus
{
    Draft = 1,
    Confirmed = 2,
    Completed = 2,
    Cancelled = 3
}

public enum FuelTransactionSource
{
    Manual = 1,
    FuelCard = 2,
    Imported = 3,
    Telematics = 4,
    Api = 5,
    MobileApp = 6,
    Other = 99
}

public enum FuelDocumentType
{
    Receipt = 1,
    Invoice = 2,
    Photo = 3,
    FuelCardStatement = 4,
    Other = 99
}

public enum FuelImportStatus
{
    Pending = 1,
    Processing = 2,
    Completed = 3,
    CompletedWithErrors = 4,
    Failed = 5
}

public enum FuelAnomalyType
{
    ExcessiveQuantity = 1,
    HighUnitPrice = 2,
    LowEfficiency = 3,
    DuplicateFueling = 4,
    FuelWithoutDistance = 5,
    PossibleWrongFuelType = 6,
    TankCapacityExceeded = 7,
    RapidRepeatFueling = 8,
    Other = 99
}

public enum FuelAnomalySeverity
{
    Information = 1,
    Warning = 2,
    Medium = 2,
    High = 3,
    Critical = 4
}

public enum FuelAnomalyStatus
{
    Open = 1,
    Resolved = 2,
    Dismissed = 3
}
