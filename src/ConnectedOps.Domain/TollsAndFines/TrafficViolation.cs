using ConnectedOps.Domain.Common;
using ConnectedOps.Domain.Drivers;
using ConnectedOps.Domain.Vehicles;

namespace ConnectedOps.Domain.TollsAndFines;

public sealed class TrafficViolation : BaseEntity
{
    private TrafficViolation()
    {
    }

    public TrafficViolation(
        Guid tenantId,
        string ticketNumber,
        string authorityName,
        string violationCode,
        string description,
        decimal fineAmount,
        int blackPoints,
        DateTime violationTimeUtc,
        Guid vehicleId,
        string? location = null,
        Guid? matchedDriverId = null,
        Guid? matchedUsageSessionId = null,
        Guid? createdByUserId = null)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        if (string.IsNullOrWhiteSpace(ticketNumber))
            throw new ArgumentException("TicketNumber is required.", nameof(ticketNumber));
        if (string.IsNullOrWhiteSpace(authorityName))
            throw new ArgumentException("AuthorityName is required.", nameof(authorityName));
        if (vehicleId == Guid.Empty)
            throw new ArgumentException("VehicleId is required.", nameof(vehicleId));

        TenantId = tenantId;
        TicketNumber = ticketNumber.Trim().ToUpperInvariant();
        AuthorityName = authorityName.Trim();
        ViolationCode = violationCode?.Trim() ?? string.Empty;
        Description = description?.Trim() ?? string.Empty;
        FineAmount = fineAmount >= 0 ? fineAmount : 0m;
        BlackPoints = Math.Max(0, blackPoints);
        ViolationTimeUtc = violationTimeUtc;
        VehicleId = vehicleId;
        Location = location?.Trim();
        MatchedDriverId = matchedDriverId;
        MatchedUsageSessionId = matchedUsageSessionId;
        LiabilityStatus = matchedDriverId.HasValue ? ViolationLiabilityStatus.AssignedToDriver : ViolationLiabilityStatus.PendingReview;
        CreatedBy = createdByUserId;
    }

    public Guid TenantId { get; private set; }
    public string TicketNumber { get; private set; } = string.Empty;
    public string AuthorityName { get; private set; } = string.Empty;
    public string ViolationCode { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public decimal FineAmount { get; private set; }
    public int BlackPoints { get; private set; }
    public DateTime ViolationTimeUtc { get; private set; }
    public string? Location { get; private set; }

    public Guid VehicleId { get; private set; }
    public Vehicle Vehicle { get; set; } = null!;

    public Guid? MatchedDriverId { get; private set; }
    public Driver? MatchedDriver { get; set; }

    public Guid? MatchedUsageSessionId { get; private set; }
    public ViolationLiabilityStatus LiabilityStatus { get; private set; }

    public string? DisputeReason { get; private set; }
    public string? ResolutionNotes { get; private set; }
    public DateTime? SettledAtUtc { get; private set; }

    public void AssignDriver(Guid driverId, Guid? sessionId = null, Guid? updatedBy = null)
    {
        MatchedDriverId = driverId;
        MatchedUsageSessionId = sessionId;
        LiabilityStatus = ViolationLiabilityStatus.AssignedToDriver;
        MarkUpdated(updatedBy);
    }

    public void Settle(ViolationLiabilityStatus status, string? notes = null, Guid? updatedBy = null)
    {
        LiabilityStatus = status;
        ResolutionNotes = notes?.Trim();
        SettledAtUtc = DateTime.UtcNow;
        MarkUpdated(updatedBy);
    }

    public void Dispute(string reason, Guid? updatedBy = null)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Dispute reason is required.", nameof(reason));

        DisputeReason = reason.Trim();
        LiabilityStatus = ViolationLiabilityStatus.Disputed;
        MarkUpdated(updatedBy);
    }
}
