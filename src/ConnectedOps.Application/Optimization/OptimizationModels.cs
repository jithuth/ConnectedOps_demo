using ConnectedOps.Domain.Optimization;

namespace ConnectedOps.Application.Optimization;

public sealed record OptimizedStopDto(
    Guid Id,
    int SequenceOrder,
    OptimizedStopType StopType,
    string LocationName,
    string Address,
    double Latitude,
    double Longitude,
    DateTime PlannedArrivalUtc,
    DateTime PlannedDepartureUtc,
    string? CustomerContact,
    decimal DemandWeightKg);

public sealed record OptimizedRoutePlanDto(
    Guid Id,
    Guid OptimizationRunId,
    Guid VehicleId,
    string VehicleName,
    Guid? DriverId,
    string? DriverName,
    string RouteName,
    decimal DistanceKm,
    int DurationMinutes,
    decimal PayloadWeightKg,
    Guid? DispatchedJobId,
    IReadOnlyList<OptimizedStopDto> Stops);

public sealed record RouteOptimizationRunDto(
    Guid Id,
    Guid TenantId,
    string RunNumber,
    OptimizationObjective Objective,
    OptimizationRunStatus Status,
    int TotalStopsInput,
    int VehiclesAvailable,
    int VehiclesAllocated,
    decimal TotalDistanceKm,
    int TotalDurationMinutes,
    decimal TotalPayloadWeightKg,
    decimal EfficiencyScorePercent,
    DateTime CreatedAtUtc,
    DateTime? SolvedAtUtc,
    DateTime? DispatchedAtUtc,
    string? SummaryJson,
    IReadOnlyList<OptimizedRoutePlanDto> RoutePlans);

public sealed record StopInputRequest(
    string LocationName,
    string Address,
    double Latitude,
    double Longitude,
    decimal DemandWeightKg = 0m,
    string? CustomerContact = null,
    DateTime? EarliestTimeUtc = null,
    DateTime? LatestTimeUtc = null);

public sealed record CreateOptimizationRunRequest(
    OptimizationObjective Objective,
    List<StopInputRequest> Stops,
    List<Guid>? AllowedVehicleIds = null,
    string DepotAddress = "Main Distribution Hub",
    double DepotLatitude = 37.7749,
    double DepotLongitude = -122.4194);

public sealed record DispatchOptimizationRunRequest(
    Guid RunId);

public sealed record OptimizationFilterRequest(
    int Page = 1,
    int PageSize = 20,
    OptimizationRunStatus? Status = null);
