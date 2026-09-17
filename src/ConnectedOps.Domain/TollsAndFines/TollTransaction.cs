using ConnectedOps.Domain.Common;
using ConnectedOps.Domain.Drivers;
using ConnectedOps.Domain.Vehicles;

namespace ConnectedOps.Domain.TollsAndFines;

public sealed class TollTransaction : BaseEntity
{
    private TollTransaction()
    {
    }

    public TollTransaction(
        Guid tenantId,
        TollSystemType tollSystem,
        string tollGateName,
        string tollGateCode,
        decimal amount,
        DateTime transactionTimeUtc,
        Guid vehicleId,
        string? tagNumber = null,
        Guid? matchedDriverId = null,
        Guid? matchedUsageSessionId = null,
        Guid? createdByUserId = null)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        if (string.IsNullOrWhiteSpace(tollGateName))
            throw new ArgumentException("TollGateName is required.", nameof(tollGateName));
        if (vehicleId == Guid.Empty)
            throw new ArgumentException("VehicleId is required.", nameof(vehicleId));

        TenantId = tenantId;
        TollSystem = tollSystem;
        TollGateName = tollGateName.Trim();
        TollGateCode = tollGateCode?.Trim() ?? string.Empty;
        Amount = amount >= 0 ? amount : 0m;
        TransactionTimeUtc = transactionTimeUtc;
        VehicleId = vehicleId;
        TagNumber = tagNumber?.Trim();
        MatchedDriverId = matchedDriverId;
        MatchedUsageSessionId = matchedUsageSessionId;
        CreatedBy = createdByUserId;
    }

    public Guid TenantId { get; private set; }
    public TollSystemType TollSystem { get; private set; }
    public string TollGateName { get; private set; } = string.Empty;
    public string TollGateCode { get; private set; } = string.Empty;
    public decimal Amount { get; private set; }
    public DateTime TransactionTimeUtc { get; private set; }

    public Guid VehicleId { get; private set; }
    public Vehicle Vehicle { get; set; } = null!;

    public string? TagNumber { get; private set; }

    public Guid? MatchedDriverId { get; private set; }
    public Driver? MatchedDriver { get; set; }

    public Guid? MatchedUsageSessionId { get; private set; }

    public void AssignMatchedDriver(Guid driverId, Guid? sessionId = null, Guid? updatedBy = null)
    {
        MatchedDriverId = driverId;
        MatchedUsageSessionId = sessionId;
        MarkUpdated(updatedBy);
    }
}
