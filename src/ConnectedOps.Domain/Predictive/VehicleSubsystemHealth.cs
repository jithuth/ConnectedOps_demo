using ConnectedOps.Domain.Common;
using ConnectedOps.Domain.Vehicles;

namespace ConnectedOps.Domain.Predictive;

public sealed class VehicleSubsystemHealth : BaseEntity
{
    private VehicleSubsystemHealth()
    {
    }

    public VehicleSubsystemHealth(
        Guid tenantId,
        Guid vehicleId,
        SubsystemCategory subsystem,
        decimal healthScore,
        HealthTrend trend = HealthTrend.Stable,
        int anomalyCount = 0,
        int? estimatedRulDays = null,
        string? diagnosticsNotes = null)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        if (vehicleId == Guid.Empty)
            throw new ArgumentException("VehicleId is required.", nameof(vehicleId));

        TenantId = tenantId;
        VehicleId = vehicleId;
        Subsystem = subsystem;
        HealthScore = Math.Clamp(healthScore, 0m, 100m);
        Trend = trend;
        AnomalyCount = Math.Max(0, anomalyCount);
        EstimatedRulDays = estimatedRulDays;
        DiagnosticsNotes = diagnosticsNotes?.Trim();
        LastEvaluatedAtUtc = DateTime.UtcNow;
    }

    public Guid TenantId { get; private set; }
    public Guid VehicleId { get; private set; }
    public Vehicle Vehicle { get; set; } = null!;

    public SubsystemCategory Subsystem { get; private set; }
    public decimal HealthScore { get; private set; }
    public HealthTrend Trend { get; private set; }
    public int AnomalyCount { get; private set; }
    public int? EstimatedRulDays { get; private set; }
    public string? DiagnosticsNotes { get; private set; }
    public DateTime LastEvaluatedAtUtc { get; private set; }

    public void UpdateHealth(
        decimal healthScore,
        HealthTrend trend,
        int anomalyCount,
        int? estimatedRulDays = null,
        string? diagnosticsNotes = null)
    {
        HealthScore = Math.Clamp(healthScore, 0m, 100m);
        Trend = trend;
        AnomalyCount = Math.Max(0, anomalyCount);
        EstimatedRulDays = estimatedRulDays;
        DiagnosticsNotes = diagnosticsNotes?.Trim();
        LastEvaluatedAtUtc = DateTime.UtcNow;
        MarkUpdated();
    }
}
