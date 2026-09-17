using ConnectedOps.Domain.Common;
using ConnectedOps.Domain.Drivers;
using ConnectedOps.Domain.Vehicles;

namespace ConnectedOps.Domain.Dispatch;

public sealed class DispatchJob : BaseEntity
{
    private DispatchJob()
    {
    }

    public DispatchJob(
        Guid tenantId,
        string jobNumber,
        string title,
        DispatchJobType jobType,
        DispatchJobPriority priority,
        string customerName,
        string customerPhone,
        string address,
        double latitude,
        double longitude,
        DateTime? timeWindowStartUtc = null,
        DateTime? timeWindowEndUtc = null,
        int serviceDurationMinutes = 15,
        decimal? weightKg = null,
        decimal? volumeM3 = null,
        int packageCount = 1,
        string? specialInstructions = null,
        string? customerEmail = null,
        Guid? createdByUserId = null)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        if (string.IsNullOrWhiteSpace(jobNumber))
            throw new ArgumentException("JobNumber is required.", nameof(jobNumber));
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title is required.", nameof(title));
        if (string.IsNullOrWhiteSpace(customerName))
            throw new ArgumentException("CustomerName is required.", nameof(customerName));
        if (string.IsNullOrWhiteSpace(address))
            throw new ArgumentException("Address is required.", nameof(address));

        TenantId = tenantId;
        JobNumber = jobNumber.Trim().ToUpperInvariant();
        Title = title.Trim();
        JobType = jobType;
        Priority = priority;
        Status = DispatchJobStatus.Unassigned;
        CustomerName = customerName.Trim();
        CustomerPhone = customerPhone?.Trim() ?? string.Empty;
        CustomerEmail = customerEmail?.Trim().ToLowerInvariant();
        Address = address.Trim();
        Latitude = latitude;
        Longitude = longitude;
        TimeWindowStartUtc = timeWindowStartUtc;
        TimeWindowEndUtc = timeWindowEndUtc;
        ServiceDurationMinutes = serviceDurationMinutes > 0 ? serviceDurationMinutes : 15;
        WeightKg = weightKg;
        VolumeM3 = volumeM3;
        PackageCount = packageCount > 0 ? packageCount : 1;
        SpecialInstructions = specialInstructions?.Trim();
        CreatedBy = createdByUserId;
    }

    public Guid TenantId { get; private set; }
    public string JobNumber { get; private set; } = string.Empty;
    public string Title { get; private set; } = string.Empty;
    public DispatchJobType JobType { get; private set; }
    public DispatchJobPriority Priority { get; private set; }
    public DispatchJobStatus Status { get; private set; }

    public string CustomerName { get; private set; } = string.Empty;
    public string CustomerPhone { get; private set; } = string.Empty;
    public string? CustomerEmail { get; private set; }
    public string Address { get; private set; } = string.Empty;
    public double Latitude { get; private set; }
    public double Longitude { get; private set; }

    public DateTime? TimeWindowStartUtc { get; private set; }
    public DateTime? TimeWindowEndUtc { get; private set; }
    public int ServiceDurationMinutes { get; private set; }

    public decimal? WeightKg { get; private set; }
    public decimal? VolumeM3 { get; private set; }
    public int PackageCount { get; private set; }
    public string? SpecialInstructions { get; private set; }

    public Guid? AssignedRouteId { get; private set; }
    public DispatchRoute? AssignedRoute { get; private set; }

    public Guid? AssignedVehicleId { get; private set; }
    public Vehicle? AssignedVehicle { get; private set; }

    public Guid? AssignedDriverId { get; private set; }
    public Driver? AssignedDriver { get; private set; }

    public DateTime? CompletedAtUtc { get; private set; }
    public DateTime? CancelledAtUtc { get; private set; }
    public string? FailureReason { get; private set; }

    public void AssignToRoute(Guid routeId, Guid? vehicleId, Guid? driverId, Guid? updatedBy = null)
    {
        AssignedRouteId = routeId;
        AssignedVehicleId = vehicleId;
        AssignedDriverId = driverId;
        Status = DispatchJobStatus.Scheduled;
        MarkUpdated(updatedBy);
    }

    public void Unassign(Guid? updatedBy = null)
    {
        AssignedRouteId = null;
        AssignedVehicleId = null;
        AssignedDriverId = null;
        Status = DispatchJobStatus.Unassigned;
        MarkUpdated(updatedBy);
    }

    public void SetStatus(DispatchJobStatus status, Guid? updatedBy = null)
    {
        Status = status;
        MarkUpdated(updatedBy);
    }

    public void MarkCompleted(DateTime? completedAtUtc = null, Guid? updatedBy = null)
    {
        Status = DispatchJobStatus.Completed;
        CompletedAtUtc = completedAtUtc ?? DateTime.UtcNow;
        FailureReason = null;
        MarkUpdated(updatedBy);
    }

    public void MarkFailed(string reason, Guid? updatedBy = null)
    {
        Status = DispatchJobStatus.Failed;
        FailureReason = reason?.Trim() ?? "Delivery failed";
        MarkUpdated(updatedBy);
    }

    public void Cancel(string? reason = null, Guid? updatedBy = null)
    {
        Status = DispatchJobStatus.Cancelled;
        CancelledAtUtc = DateTime.UtcNow;
        FailureReason = reason?.Trim();
        MarkUpdated(updatedBy);
    }

    public void Update(
        string title,
        DispatchJobType jobType,
        DispatchJobPriority priority,
        string customerName,
        string customerPhone,
        string? customerEmail,
        string address,
        double latitude,
        double longitude,
        DateTime? timeWindowStartUtc,
        DateTime? timeWindowEndUtc,
        int serviceDurationMinutes,
        decimal? weightKg,
        decimal? volumeM3,
        int packageCount,
        string? specialInstructions,
        Guid? updatedBy = null)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title is required.", nameof(title));
        if (string.IsNullOrWhiteSpace(customerName))
            throw new ArgumentException("CustomerName is required.", nameof(customerName));
        if (string.IsNullOrWhiteSpace(address))
            throw new ArgumentException("Address is required.", nameof(address));

        Title = title.Trim();
        JobType = jobType;
        Priority = priority;
        CustomerName = customerName.Trim();
        CustomerPhone = customerPhone?.Trim() ?? string.Empty;
        CustomerEmail = customerEmail?.Trim().ToLowerInvariant();
        Address = address.Trim();
        Latitude = latitude;
        Longitude = longitude;
        TimeWindowStartUtc = timeWindowStartUtc;
        TimeWindowEndUtc = timeWindowEndUtc;
        ServiceDurationMinutes = serviceDurationMinutes > 0 ? serviceDurationMinutes : 15;
        WeightKg = weightKg;
        VolumeM3 = volumeM3;
        PackageCount = packageCount > 0 ? packageCount : 1;
        SpecialInstructions = specialInstructions?.Trim();
        MarkUpdated(updatedBy);
    }
}
