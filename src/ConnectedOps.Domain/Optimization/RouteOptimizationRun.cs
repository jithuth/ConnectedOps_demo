using ConnectedOps.Domain.Common;

namespace ConnectedOps.Domain.Optimization;

public sealed class RouteOptimizationRun : BaseEntity
{
    private readonly List<OptimizedRoutePlan> _routePlans = [];

    private RouteOptimizationRun()
    {
    }

    public RouteOptimizationRun(
        Guid tenantId,
        string runNumber,
        OptimizationObjective objective,
        int totalStopsInput,
        int vehiclesAvailable)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        if (string.IsNullOrWhiteSpace(runNumber))
            throw new ArgumentException("RunNumber is required.", nameof(runNumber));

        TenantId = tenantId;
        RunNumber = runNumber.Trim().ToUpperInvariant();
        Objective = objective;
        TotalStopsInput = totalStopsInput;
        VehiclesAvailable = vehiclesAvailable;
        Status = OptimizationRunStatus.Pending;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public Guid TenantId { get; private set; }
    public string RunNumber { get; private set; } = string.Empty;
    public OptimizationObjective Objective { get; private set; }
    public OptimizationRunStatus Status { get; private set; }
    public int TotalStopsInput { get; private set; }
    public int VehiclesAvailable { get; private set; }
    public int VehiclesAllocated { get; private set; }
    public decimal TotalDistanceKm { get; private set; }
    public int TotalDurationMinutes { get; private set; }
    public decimal TotalPayloadWeightKg { get; private set; }
    public decimal EfficiencyScorePercent { get; private set; }
    public DateTime? SolvedAtUtc { get; private set; }
    public DateTime? DispatchedAtUtc { get; private set; }
    public string? SummaryJson { get; private set; }

    public IReadOnlyCollection<OptimizedRoutePlan> RoutePlans => _routePlans.AsReadOnly();

    public void CompleteOptimization(
        int vehiclesAllocated,
        decimal totalDistanceKm,
        int totalDurationMinutes,
        decimal totalPayloadWeightKg,
        decimal efficiencyScorePercent,
        string? summaryJson = null)
    {
        VehiclesAllocated = vehiclesAllocated;
        TotalDistanceKm = Math.Round(totalDistanceKm, 2);
        TotalDurationMinutes = totalDurationMinutes;
        TotalPayloadWeightKg = Math.Round(totalPayloadWeightKg, 2);
        EfficiencyScorePercent = Math.Clamp(efficiencyScorePercent, 0m, 100m);
        SummaryJson = summaryJson;
        Status = OptimizationRunStatus.Completed;
        SolvedAtUtc = DateTime.UtcNow;
        MarkUpdated();
    }

    public void MarkDispatched()
    {
        Status = OptimizationRunStatus.Dispatched;
        DispatchedAtUtc = DateTime.UtcNow;
        MarkUpdated();
    }

    public void AddPlan(OptimizedRoutePlan plan)
    {
        _routePlans.Add(plan);
    }
}
