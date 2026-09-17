namespace ConnectedOps.Domain.Predictive;

public enum SubsystemCategory
{
    PowertrainEngine = 1,
    ElectricalBattery = 2,
    BrakingChassis = 3,
    TransmissionDrivetrain = 4,
    TiresSuspension = 5
}

public enum PredictiveRiskLevel
{
    LowRisk = 1,
    ModerateWatch = 2,
    ElevatedWarning = 3,
    CriticalFailureImminent = 4
}

public enum HealthTrend
{
    Improving = 1,
    Stable = 2,
    Degrading = 3
}

public enum PredictiveRecommendationStatus
{
    Active = 1,
    Acknowledged = 2,
    WorkOrderCreated = 3,
    Dismissed = 4
}
