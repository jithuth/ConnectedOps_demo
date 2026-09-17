using ConnectedOps.Domain.Common;
using ConnectedOps.Domain.Vehicles;

namespace ConnectedOps.Domain.Predictive;

public sealed class PredictiveMaintenanceAlert : BaseEntity
{
    private PredictiveMaintenanceAlert()
    {
    }

    public PredictiveMaintenanceAlert(
        Guid tenantId,
        Guid vehicleId,
        SubsystemCategory subsystem,
        PredictiveRiskLevel riskLevel,
        decimal failureProbability,
        int estimatedRulDays,
        string componentTitle,
        string symptomDescription,
        string recommendedAction,
        decimal estimatedRepairCost = 0m,
        string currency = "USD")
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        if (vehicleId == Guid.Empty)
            throw new ArgumentException("VehicleId is required.", nameof(vehicleId));
        if (string.IsNullOrWhiteSpace(componentTitle))
            throw new ArgumentException("ComponentTitle is required.", nameof(componentTitle));
        if (string.IsNullOrWhiteSpace(symptomDescription))
            throw new ArgumentException("SymptomDescription is required.", nameof(symptomDescription));

        TenantId = tenantId;
        VehicleId = vehicleId;
        Subsystem = subsystem;
        RiskLevel = riskLevel;
        FailureProbability = Math.Clamp(failureProbability, 0m, 100m);
        EstimatedRulDays = Math.Max(0, estimatedRulDays);
        ComponentTitle = componentTitle.Trim();
        SymptomDescription = symptomDescription.Trim();
        RecommendedAction = string.IsNullOrWhiteSpace(recommendedAction) ? "Inspect component." : recommendedAction.Trim();
        EstimatedRepairCost = Math.Max(0m, estimatedRepairCost);
        Currency = string.IsNullOrWhiteSpace(currency) ? "USD" : currency.Trim().ToUpperInvariant();
        Status = PredictiveRecommendationStatus.Active;
        DetectedAtUtc = DateTime.UtcNow;
    }

    public Guid TenantId { get; private set; }
    public Guid VehicleId { get; private set; }
    public Vehicle Vehicle { get; set; } = null!;

    public SubsystemCategory Subsystem { get; private set; }
    public PredictiveRiskLevel RiskLevel { get; private set; }
    public decimal FailureProbability { get; private set; }
    public int EstimatedRulDays { get; private set; }
    public string ComponentTitle { get; private set; } = string.Empty;
    public string SymptomDescription { get; private set; } = string.Empty;
    public string RecommendedAction { get; private set; } = string.Empty;
    public decimal EstimatedRepairCost { get; private set; }
    public string Currency { get; private set; } = "USD";
    public PredictiveRecommendationStatus Status { get; private set; }
    public DateTime DetectedAtUtc { get; private set; }
    public Guid? PromotedMaintenanceRecordId { get; private set; }
    public DateTime? WorkOrderCreatedAtUtc { get; private set; }
    public string? DismissReason { get; private set; }
    public DateTime? AcknowledgedAtUtc { get; private set; }
    public Guid? AcknowledgedBy { get; private set; }

    public void Acknowledge(Guid userId)
    {
        Status = PredictiveRecommendationStatus.Acknowledged;
        AcknowledgedAtUtc = DateTime.UtcNow;
        AcknowledgedBy = userId;
        MarkUpdated(userId);
    }

    public void PromoteToWorkOrder(Guid maintenanceRecordId, Guid userId)
    {
        if (maintenanceRecordId == Guid.Empty)
            throw new ArgumentException("MaintenanceRecordId is required.", nameof(maintenanceRecordId));

        Status = PredictiveRecommendationStatus.WorkOrderCreated;
        PromotedMaintenanceRecordId = maintenanceRecordId;
        WorkOrderCreatedAtUtc = DateTime.UtcNow;
        MarkUpdated(userId);
    }

    public void Dismiss(string reason, Guid userId)
    {
        Status = PredictiveRecommendationStatus.Dismissed;
        DismissReason = string.IsNullOrWhiteSpace(reason) ? "Dismissed by fleet manager" : reason.Trim();
        MarkUpdated(userId);
    }
}
