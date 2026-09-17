using ConnectedOps.Domain.Common;
using ConnectedOps.Domain.Drivers;
using ConnectedOps.Domain.Vehicles;

namespace ConnectedOps.Domain.Optimization;

public sealed class OptimizedRoutePlan : BaseEntity
{
    private readonly List<OptimizedStopSequence> _stops = [];

    private OptimizedRoutePlan()
    {
    }

    public OptimizedRoutePlan(
        Guid tenantId,
        Guid optimizationRunId,
        Guid vehicleId,
        Guid? driverId,
        string routeName,
        decimal distanceKm,
        int durationMinutes,
        decimal payloadWeightKg)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        if (optimizationRunId == Guid.Empty)
            throw new ArgumentException("OptimizationRunId is required.", nameof(optimizationRunId));
        if (vehicleId == Guid.Empty)
            throw new ArgumentException("VehicleId is required.", nameof(vehicleId));

        TenantId = tenantId;
        OptimizationRunId = optimizationRunId;
        VehicleId = vehicleId;
        DriverId = driverId;
        RouteName = string.IsNullOrWhiteSpace(routeName) ? "Optimized Route" : routeName.Trim();
        DistanceKm = Math.Round(distanceKm, 2);
        DurationMinutes = durationMinutes;
        PayloadWeightKg = Math.Round(payloadWeightKg, 2);
    }

    public Guid TenantId { get; private set; }
    public Guid OptimizationRunId { get; private set; }
    public RouteOptimizationRun OptimizationRun { get; set; } = null!;

    public Guid VehicleId { get; private set; }
    public Vehicle Vehicle { get; set; } = null!;

    public Guid? DriverId { get; private set; }
    public Driver? Driver { get; set; }

    public string RouteName { get; private set; } = string.Empty;
    public decimal DistanceKm { get; private set; }
    public int DurationMinutes { get; private set; }
    public decimal PayloadWeightKg { get; private set; }
    public Guid? DispatchedJobId { get; private set; }

    public IReadOnlyCollection<OptimizedStopSequence> Stops => _stops.AsReadOnly();

    public void AddStop(OptimizedStopSequence stop)
    {
        _stops.Add(stop);
    }

    public void MarkDispatched(Guid jobId)
    {
        DispatchedJobId = jobId;
        MarkUpdated();
    }
}
