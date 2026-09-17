using ConnectedOps.Domain.Predictive;

namespace ConnectedOps.Application.Predictive;

public sealed record SubsystemHealthScoreDto(
    SubsystemCategory Subsystem,
    string SubsystemName,
    decimal HealthScore,
    HealthTrend Trend,
    string TrendName,
    int AnomalyCount,
    int? EstimatedRulDays,
    string? DiagnosticsNotes,
    DateTime LastEvaluatedAtUtc);

public sealed record VehicleDigitalTwinHealthDto(
    Guid VehicleId,
    string VehicleNumber,
    string? DisplayName,
    string? RegistrationNumber,
    decimal OverallHealthScore,
    PredictiveRiskLevel OverallRisk,
    string OverallRiskName,
    int ActiveAlertsCount,
    List<SubsystemHealthScoreDto> Subsystems,
    DateTime LastEvaluatedAtUtc);

public sealed record PredictiveAlertDto(
    Guid Id,
    Guid VehicleId,
    string VehicleNumber,
    string? VehicleDisplayName,
    SubsystemCategory Subsystem,
    string SubsystemName,
    PredictiveRiskLevel RiskLevel,
    string RiskLevelName,
    decimal FailureProbability,
    int EstimatedRulDays,
    string ComponentTitle,
    string SymptomDescription,
    string RecommendedAction,
    decimal EstimatedRepairCost,
    string Currency,
    PredictiveRecommendationStatus Status,
    string StatusName,
    DateTime DetectedAtUtc,
    Guid? PromotedMaintenanceRecordId,
    string? DismissReason);

public sealed record PredictiveFleetDashboardDto(
    int TotalMonitoredVehicles,
    decimal AverageFleetHealthScore,
    int CriticalVehiclesCount,
    int WatchVehiclesCount,
    int ActivePredictiveAlertsCount,
    decimal PotentialSavingsEstimated,
    List<PredictiveAlertDto> UrgentAlerts,
    List<VehicleDigitalTwinHealthDto> TopAtRiskVehicles);

public sealed record TriggerDiagnosticScanRequest(
    Guid? VehicleId = null);

public sealed record PromoteToWorkOrderRequest(
    string? Notes = null,
    DateTime? ScheduledDateUtc = null,
    decimal? EstimatedCost = null);
