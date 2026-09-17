using ConnectedOps.Domain.Dispatch;

namespace ConnectedOps.Application.Dispatch;

public sealed record DispatchJobFilterRequest
{
    public string? SearchTerm { get; init; }
    public DispatchJobStatus? Status { get; init; }
    public DispatchJobType? JobType { get; init; }
    public DispatchJobPriority? Priority { get; init; }
    public Guid? AssignedVehicleId { get; init; }
    public Guid? AssignedDriverId { get; init; }
    public Guid? AssignedRouteId { get; init; }
    public DateTime? FromDateUtc { get; init; }
    public DateTime? ToDateUtc { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 25;
}

public sealed record DispatchRouteFilterRequest
{
    public string? SearchTerm { get; init; }
    public DispatchRouteStatus? Status { get; init; }
    public DateOnly? ScheduledDate { get; init; }
    public Guid? VehicleId { get; init; }
    public Guid? DriverId { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 25;
}

public sealed record CreateDispatchJobRequest
{
    public string Title { get; init; } = string.Empty;
    public DispatchJobType JobType { get; init; } = DispatchJobType.Delivery;
    public DispatchJobPriority Priority { get; init; } = DispatchJobPriority.Standard;
    public string CustomerName { get; init; } = string.Empty;
    public string CustomerPhone { get; init; } = string.Empty;
    public string? CustomerEmail { get; init; }
    public string Address { get; init; } = string.Empty;
    public double Latitude { get; init; }
    public double Longitude { get; init; }
    public DateTime? TimeWindowStartUtc { get; init; }
    public DateTime? TimeWindowEndUtc { get; init; }
    public int ServiceDurationMinutes { get; init; } = 15;
    public decimal? WeightKg { get; init; }
    public decimal? VolumeM3 { get; init; }
    public int PackageCount { get; init; } = 1;
    public string? SpecialInstructions { get; init; }
}

public sealed record UpdateDispatchJobRequest
{
    public string Title { get; init; } = string.Empty;
    public DispatchJobType JobType { get; init; }
    public DispatchJobPriority Priority { get; init; }
    public string CustomerName { get; init; } = string.Empty;
    public string CustomerPhone { get; init; } = string.Empty;
    public string? CustomerEmail { get; init; }
    public string Address { get; init; } = string.Empty;
    public double Latitude { get; init; }
    public double Longitude { get; init; }
    public DateTime? TimeWindowStartUtc { get; init; }
    public DateTime? TimeWindowEndUtc { get; init; }
    public int ServiceDurationMinutes { get; init; }
    public decimal? WeightKg { get; init; }
    public decimal? VolumeM3 { get; init; }
    public int PackageCount { get; init; }
    public string? SpecialInstructions { get; init; }
}

public sealed record CreateDispatchRouteRequest
{
    public string Name { get; init; } = string.Empty;
    public DateOnly ScheduledDate { get; init; } = DateOnly.FromDateTime(DateTime.UtcNow);
    public Guid? VehicleId { get; init; }
    public Guid? DriverId { get; init; }
    public Guid? StartLocationId { get; init; }
    public Guid? EndLocationId { get; init; }
    public string? Notes { get; init; }
    public List<Guid> JobIdsInSequence { get; init; } = [];
}

public sealed record UpdateDispatchRouteRequest
{
    public string Name { get; init; } = string.Empty;
    public DateOnly ScheduledDate { get; init; }
    public Guid? VehicleId { get; init; }
    public Guid? DriverId { get; init; }
    public Guid? StartLocationId { get; init; }
    public Guid? EndLocationId { get; init; }
    public string? Notes { get; init; }
}

public sealed record AddRouteStopRequest
{
    public Guid JobId { get; init; }
    public int SequenceOrder { get; init; }
    public DateTime? PlannedArrivalUtc { get; init; }
    public string? Notes { get; init; }
}

public sealed record UpdateRouteStopStatusRequest
{
    public RouteStopStatus Status { get; init; }
    public string? ReasonOrNotes { get; init; }
}

public sealed record RecordProofOfDeliveryRequest
{
    public PodVerificationType VerificationType { get; init; } = PodVerificationType.Signature;
    public string RecipientName { get; init; } = string.Empty;
    public string? SignatureData { get; init; }
    public string? Notes { get; init; }
    public double? Latitude { get; init; }
    public double? Longitude { get; init; }
}

public sealed record OptimizeRouteStopsRequest
{
    public double? DepotLatitude { get; init; }
    public double? DepotLongitude { get; init; }
}

public sealed record DispatchJobDto
{
    public Guid Id { get; init; }
    public string JobNumber { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public DispatchJobType JobType { get; init; }
    public string JobTypeName => JobType.ToString();
    public DispatchJobPriority Priority { get; init; }
    public string PriorityName => Priority.ToString();
    public DispatchJobStatus Status { get; init; }
    public string StatusName => Status.ToString();

    public string CustomerName { get; init; } = string.Empty;
    public string CustomerPhone { get; init; } = string.Empty;
    public string? CustomerEmail { get; init; }
    public string Address { get; init; } = string.Empty;
    public double Latitude { get; init; }
    public double Longitude { get; init; }

    public DateTime? TimeWindowStartUtc { get; init; }
    public DateTime? TimeWindowEndUtc { get; init; }
    public int ServiceDurationMinutes { get; init; }

    public decimal? WeightKg { get; init; }
    public decimal? VolumeM3 { get; init; }
    public int PackageCount { get; init; }
    public string? SpecialInstructions { get; init; }

    public Guid? AssignedRouteId { get; init; }
    public string? AssignedRouteNumber { get; init; }

    public Guid? AssignedVehicleId { get; init; }
    public string? AssignedVehicleNumber { get; init; }

    public Guid? AssignedDriverId { get; init; }
    public string? AssignedDriverName { get; init; }

    public DateTime? CompletedAtUtc { get; init; }
    public DateTime? CancelledAtUtc { get; init; }
    public string? FailureReason { get; init; }
    public DateTime CreatedAtUtc { get; init; }
}

public sealed record DispatchRouteDto
{
    public Guid Id { get; init; }
    public string RouteNumber { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public DateOnly ScheduledDate { get; init; }
    public DispatchRouteStatus Status { get; init; }
    public string StatusName => Status.ToString();

    public Guid? VehicleId { get; init; }
    public string? VehicleNumber { get; init; }
    public string? VehicleRegistration { get; init; }

    public Guid? DriverId { get; init; }
    public string? DriverName { get; init; }
    public string? DriverPhone { get; init; }

    public Guid? StartLocationId { get; init; }
    public string? StartLocationName { get; init; }

    public Guid? EndLocationId { get; init; }
    public string? EndLocationName { get; init; }

    public decimal EstimatedDistanceKm { get; init; }
    public decimal? ActualDistanceKm { get; init; }
    public int EstimatedDurationMinutes { get; init; }
    public int? ActualDurationMinutes { get; init; }

    public DateTime? StartedAtUtc { get; init; }
    public DateTime? CompletedAtUtc { get; init; }
    public string? Notes { get; init; }
    public DateTime CreatedAtUtc { get; init; }

    public List<DispatchRouteStopDto> Stops { get; init; } = [];
    public int TotalStopsCount => Stops.Count;
    public int CompletedStopsCount => Stops.Count(s => s.Status == RouteStopStatus.Completed);
    public decimal CompletionPercentage => TotalStopsCount > 0 ? Math.Round((decimal)CompletedStopsCount / TotalStopsCount * 100m, 1) : 0m;
}

public sealed record DispatchRouteStopDto
{
    public Guid Id { get; init; }
    public Guid RouteId { get; init; }
    public Guid JobId { get; init; }
    public string JobNumber { get; init; } = string.Empty;
    public string JobTitle { get; init; } = string.Empty;
    public DispatchJobType JobType { get; init; }
    public DispatchJobPriority Priority { get; init; }
    public string CustomerName { get; init; } = string.Empty;
    public string CustomerPhone { get; init; } = string.Empty;
    public string Address { get; init; } = string.Empty;
    public double Latitude { get; init; }
    public double Longitude { get; init; }

    public int SequenceOrder { get; init; }
    public RouteStopStatus Status { get; init; }
    public string StatusName => Status.ToString();

    public DateTime? PlannedArrivalUtc { get; init; }
    public DateTime? ActualArrivalUtc { get; init; }
    public DateTime? ActualDepartureUtc { get; init; }

    public decimal EstimatedDistanceKm { get; init; }
    public int EstimatedMinutes { get; init; }
    public string? Notes { get; init; }
    public bool HasPod { get; init; }
}

public sealed record ProofOfDeliveryDto
{
    public Guid Id { get; init; }
    public Guid JobId { get; init; }
    public string JobNumber { get; init; } = string.Empty;
    public Guid? RouteStopId { get; init; }
    public PodVerificationType VerificationType { get; init; }
    public string VerificationTypeName => VerificationType.ToString();
    public string RecipientName { get; init; } = string.Empty;
    public string? SignatureData { get; init; }
    public string? Notes { get; init; }
    public double? Latitude { get; init; }
    public double? Longitude { get; init; }
    public DateTime CompletedAtUtc { get; init; }
    public Guid? VerifiedByUserId { get; init; }
}

public sealed record DispatchDashboardDto
{
    public int TotalJobsToday { get; init; }
    public int CompletedJobsToday { get; init; }
    public int InProgressJobsToday { get; init; }
    public int PendingJobsToday { get; init; }
    public int FailedJobsToday { get; init; }
    public decimal OnTimeDeliveryRate { get; init; }

    public int TotalRoutesScheduled { get; init; }
    public int ActiveRoutes { get; init; }
    public int CompletedRoutes { get; init; }
    public decimal TotalDistanceKmToday { get; init; }

    public List<DispatchRouteDto> ActiveRoutesList { get; init; } = [];
    public List<DispatchJobDto> RecentCriticalJobs { get; init; } = [];
}
