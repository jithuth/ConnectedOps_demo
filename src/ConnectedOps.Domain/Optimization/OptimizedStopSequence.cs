using ConnectedOps.Domain.Common;

namespace ConnectedOps.Domain.Optimization;

public sealed class OptimizedStopSequence : BaseEntity
{
    private OptimizedStopSequence()
    {
    }

    public OptimizedStopSequence(
        Guid tenantId,
        Guid optimizedRoutePlanId,
        int sequenceOrder,
        OptimizedStopType stopType,
        string locationName,
        string address,
        double latitude,
        double longitude,
        DateTime plannedArrivalUtc,
        DateTime plannedDepartureUtc,
        string? customerContact = null,
        decimal demandWeightKg = 0m)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        if (optimizedRoutePlanId == Guid.Empty)
            throw new ArgumentException("OptimizedRoutePlanId is required.", nameof(optimizedRoutePlanId));
        if (string.IsNullOrWhiteSpace(locationName))
            throw new ArgumentException("LocationName is required.", nameof(locationName));

        TenantId = tenantId;
        OptimizedRoutePlanId = optimizedRoutePlanId;
        SequenceOrder = sequenceOrder;
        StopType = stopType;
        LocationName = locationName.Trim();
        Address = string.IsNullOrWhiteSpace(address) ? locationName.Trim() : address.Trim();
        Latitude = latitude;
        Longitude = longitude;
        PlannedArrivalUtc = plannedArrivalUtc;
        PlannedDepartureUtc = plannedDepartureUtc;
        CustomerContact = customerContact?.Trim();
        DemandWeightKg = Math.Max(0m, demandWeightKg);
    }

    public Guid TenantId { get; private set; }
    public Guid OptimizedRoutePlanId { get; private set; }
    public OptimizedRoutePlan RoutePlan { get; set; } = null!;

    public int SequenceOrder { get; private set; }
    public OptimizedStopType StopType { get; private set; }
    public string LocationName { get; private set; } = string.Empty;
    public string Address { get; private set; } = string.Empty;
    public double Latitude { get; private set; }
    public double Longitude { get; private set; }
    public DateTime PlannedArrivalUtc { get; private set; }
    public DateTime PlannedDepartureUtc { get; private set; }
    public string? CustomerContact { get; private set; }
    public decimal DemandWeightKg { get; private set; }
}
