namespace ConnectedOps.Domain.Integrations;

public enum WebhookEventType
{
    VehicleStatusChanged = 1,
    GeofenceBreached = 2,
    HarshDrivingDetected = 3,
    SafetyIncidentCreated = 4,
    MaintenanceDue = 5,
    FuelAnomalyDetected = 6,
    DispatchJobUpdated = 7,
    DvirDefectReported = 8,
    ColdChainExcursion = 9
}

public enum ApiKeyScope
{
    FleetRead = 1,
    FleetWrite = 2,
    DispatchRead = 3,
    DispatchWrite = 4,
    TelematicsRead = 5,
    ReportsRead = 6,
    WebhooksManage = 7
}

public enum ErpTargetSystem
{
    QuickBooks = 1,
    Xero = 2,
    Sap = 3,
    MicrosoftDynamics = 4,
    GenericCsv = 5
}

public enum ErpBatchType
{
    Expenses = 1,
    MaintenanceCosts = 2,
    FuelSpend = 3,
    GeneralLedgerSummary = 4
}

public enum ErpBatchStatus
{
    Generated = 1,
    Exported = 2,
    Acknowledged = 3,
    Failed = 4
}

public enum FuelClearinghouseProvider
{
    Wex = 1,
    FleetCor = 2,
    ShellFleet = 3,
    AdnocFleet = 4,
    EnocEppco = 5
}
