using ConnectedOps.Domain.Assets;
using ConnectedOps.Domain.Common;
using ConnectedOps.Domain.Drivers;
using ConnectedOps.Domain.Vehicles;

namespace ConnectedOps.Domain.Compliance;

public sealed class ComplianceRecord : BaseEntity
{
    private readonly List<ComplianceDocument> _documents = [];

    private ComplianceRecord()
    {
    }

    public ComplianceRecord(
        Guid tenantId,
        Guid complianceRequirementId,
        ComplianceSubjectType subjectType,
        Guid? vehicleId = null,
        Guid? driverId = null,
        Guid? assetId = null,
        string? referenceNumber = null,
        DateTime? issueDateUtc = null,
        DateTime? effectiveFromUtc = null,
        DateTime? expiryDateUtc = null,
        ComplianceStatus status = ComplianceStatus.Valid,
        DateTime? verifiedAtUtc = null,
        Guid? verifiedByUserId = null,
        string? notes = null,
        Guid? createdByUserId = null)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        if (complianceRequirementId == Guid.Empty)
            throw new ArgumentException("ComplianceRequirementId is required.", nameof(complianceRequirementId));

        ValidateSubject(subjectType, vehicleId, driverId, assetId);

        if (issueDateUtc.HasValue && expiryDateUtc.HasValue && expiryDateUtc < issueDateUtc)
            throw new ArgumentException("Expiry date cannot be earlier than issue date.");

        TenantId = tenantId;
        ComplianceRequirementId = complianceRequirementId;
        SubjectType = subjectType;
        VehicleId = vehicleId;
        DriverId = driverId;
        AssetId = assetId;
        ReferenceNumber = referenceNumber?.Trim();
        IssueDateUtc = issueDateUtc;
        EffectiveFromUtc = effectiveFromUtc ?? issueDateUtc;
        ExpiryDateUtc = expiryDateUtc;
        Status = status;
        VerifiedAtUtc = verifiedAtUtc;
        VerifiedByUserId = verifiedByUserId;
        Notes = notes?.Trim();
        CreatedBy = createdByUserId;
    }

    public Guid TenantId { get; private set; }
    public Guid ComplianceRequirementId { get; private set; }
    public ComplianceRequirement ComplianceRequirement { get; private set; } = null!;

    public ComplianceSubjectType SubjectType { get; private set; }
    public Guid? VehicleId { get; private set; }
    public Vehicle? Vehicle { get; private set; }

    public Guid? DriverId { get; private set; }
    public Driver? Driver { get; private set; }

    public Guid? AssetId { get; private set; }
    public Asset? Asset { get; private set; }

    public string? ReferenceNumber { get; private set; }
    public DateTime? IssueDateUtc { get; private set; }
    public DateTime? EffectiveFromUtc { get; private set; }
    public DateTime? ExpiryDateUtc { get; private set; }

    public ComplianceStatus Status { get; private set; }
    public DateTime? VerifiedAtUtc { get; private set; }
    public Guid? VerifiedByUserId { get; private set; }
    public string? Notes { get; private set; }

    public IReadOnlyCollection<ComplianceDocument> Documents => _documents;

    public void Update(
        string? referenceNumber,
        DateTime? issueDateUtc,
        DateTime? effectiveFromUtc,
        DateTime? expiryDateUtc,
        ComplianceStatus status,
        string? notes,
        Guid? updatedByUserId = null)
    {
        if (issueDateUtc.HasValue && expiryDateUtc.HasValue && expiryDateUtc < issueDateUtc)
            throw new ArgumentException("Expiry date cannot be earlier than issue date.");

        ReferenceNumber = referenceNumber?.Trim();
        IssueDateUtc = issueDateUtc;
        EffectiveFromUtc = effectiveFromUtc ?? issueDateUtc;
        ExpiryDateUtc = expiryDateUtc;
        Status = status;
        Notes = notes?.Trim();
        UpdatedBy = updatedByUserId;
        MarkUpdated();
    }

    public void Verify(Guid verifiedByUserId, DateTime? verifiedAtUtc = null)
    {
        if (verifiedByUserId == Guid.Empty)
            throw new ArgumentException("VerifiedByUserId is required.", nameof(verifiedByUserId));

        VerifiedByUserId = verifiedByUserId;
        VerifiedAtUtc = verifiedAtUtc ?? DateTime.UtcNow;
        if (Status == ComplianceStatus.PendingVerification)
        {
            Status = ComplianceStatus.Valid;
        }
        MarkUpdated();
    }

    public void SetStatus(ComplianceStatus status)
    {
        Status = status;
        MarkUpdated();
    }

    public void AddDocument(ComplianceDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        if (document.TenantId != TenantId)
            throw new InvalidOperationException("Tenant mismatch between record and document.");
        _documents.Add(document);
    }

    private static void ValidateSubject(
        ComplianceSubjectType subjectType,
        Guid? vehicleId,
        Guid? driverId,
        Guid? assetId)
    {
        int subjectCount = (vehicleId.HasValue ? 1 : 0) + (driverId.HasValue ? 1 : 0) + (assetId.HasValue ? 1 : 0);
        if (subjectCount != 1)
            throw new ArgumentException("Exactly one of VehicleId, DriverId, or AssetId must be populated.");

        switch (subjectType)
        {
            case ComplianceSubjectType.Vehicle when !vehicleId.HasValue:
                throw new ArgumentException("VehicleId is required when SubjectType is Vehicle.");
            case ComplianceSubjectType.Driver when !driverId.HasValue:
                throw new ArgumentException("DriverId is required when SubjectType is Driver.");
            case ComplianceSubjectType.Asset when !assetId.HasValue:
                throw new ArgumentException("AssetId is required when SubjectType is Asset.");
        }
    }
}
