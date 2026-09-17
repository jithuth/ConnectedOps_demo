using ConnectedOps.Domain.Common;

namespace ConnectedOps.Domain.Dispatch;

public sealed class ProofOfDelivery : BaseEntity
{
    private ProofOfDelivery()
    {
    }

    public ProofOfDelivery(
        Guid tenantId,
        Guid jobId,
        Guid? routeStopId,
        PodVerificationType verificationType,
        string recipientName,
        string? signatureData = null,
        string? notes = null,
        double? latitude = null,
        double? longitude = null,
        DateTime? completedAtUtc = null,
        Guid? verifiedByUserId = null)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        if (jobId == Guid.Empty)
            throw new ArgumentException("JobId is required.", nameof(jobId));
        if (string.IsNullOrWhiteSpace(recipientName))
            throw new ArgumentException("RecipientName is required.", nameof(recipientName));

        TenantId = tenantId;
        JobId = jobId;
        RouteStopId = routeStopId;
        VerificationType = verificationType;
        RecipientName = recipientName.Trim();
        SignatureData = signatureData;
        Notes = notes?.Trim();
        Latitude = latitude;
        Longitude = longitude;
        CompletedAtUtc = completedAtUtc ?? DateTime.UtcNow;
        VerifiedByUserId = verifiedByUserId;
        CreatedBy = verifiedByUserId;
    }

    public Guid TenantId { get; private set; }
    public Guid JobId { get; private set; }
    public DispatchJob Job { get; private set; } = null!;

    public Guid? RouteStopId { get; private set; }
    public DispatchRouteStop? RouteStop { get; private set; }

    public PodVerificationType VerificationType { get; private set; }
    public string RecipientName { get; private set; } = string.Empty;
    public string? SignatureData { get; private set; }
    public string? Notes { get; private set; }
    public double? Latitude { get; private set; }
    public double? Longitude { get; private set; }
    public DateTime CompletedAtUtc { get; private set; }
    public Guid? VerifiedByUserId { get; private set; }
}
