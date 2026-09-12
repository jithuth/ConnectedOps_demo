using ConnectedOps.Domain.Assets;
using ConnectedOps.Domain.Common;
using ConnectedOps.Domain.Drivers;
using ConnectedOps.Domain.Vehicles;

namespace ConnectedOps.Domain.Compliance;

public sealed class ComplianceException : BaseEntity
{
    private ComplianceException()
    {
    }

    public ComplianceException(
        Guid tenantId,
        Guid complianceRequirementId,
        ComplianceSubjectType subjectType,
        string reason,
        Guid approvedByUserId,
        DateTime effectiveFromUtc,
        DateTime effectiveToUtc,
        Guid? vehicleId = null,
        Guid? driverId = null,
        Guid? assetId = null,
        ComplianceExceptionStatus status = ComplianceExceptionStatus.Approved,
        Guid? createdByUserId = null)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        if (complianceRequirementId == Guid.Empty)
            throw new ArgumentException("ComplianceRequirementId is required.", nameof(complianceRequirementId));
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Exemption reason is required.", nameof(reason));
        if (approvedByUserId == Guid.Empty)
            throw new ArgumentException("ApprovedByUserId is required.", nameof(approvedByUserId));
        if (effectiveToUtc < effectiveFromUtc)
            throw new ArgumentException("Effective to date cannot be earlier than effective from date.");

        ValidateSubject(subjectType, vehicleId, driverId, assetId);

        TenantId = tenantId;
        ComplianceRequirementId = complianceRequirementId;
        SubjectType = subjectType;
        Reason = reason.Trim();
        ApprovedByUserId = approvedByUserId;
        EffectiveFromUtc = effectiveFromUtc;
        EffectiveToUtc = effectiveToUtc;
        VehicleId = vehicleId;
        DriverId = driverId;
        AssetId = assetId;
        Status = status;
        CreatedBy = createdByUserId ?? approvedByUserId;
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

    public string Reason { get; private set; } = string.Empty;
    public Guid ApprovedByUserId { get; private set; }
    public DateTime EffectiveFromUtc { get; private set; }
    public DateTime EffectiveToUtc { get; private set; }
    public ComplianceExceptionStatus Status { get; private set; }

    public bool IsCurrentlyActive(DateTime? nowUtc = null)
    {
        var now = nowUtc ?? DateTime.UtcNow;
        return Status == ComplianceExceptionStatus.Approved &&
               EffectiveFromUtc <= now &&
               now <= EffectiveToUtc;
    }

    public void Approve(Guid approvedByUserId)
    {
        ApprovedByUserId = approvedByUserId;
        Status = ComplianceExceptionStatus.Approved;
        MarkUpdated();
    }

    public void Reject(Guid rejectedByUserId)
    {
        ApprovedByUserId = rejectedByUserId;
        Status = ComplianceExceptionStatus.Rejected;
        MarkUpdated();
    }

    public void Cancel(Guid cancelledByUserId)
    {
        Status = ComplianceExceptionStatus.Cancelled;
        UpdatedBy = cancelledByUserId;
        MarkUpdated();
    }

    public void CheckExpiration(DateTime? nowUtc = null)
    {
        var now = nowUtc ?? DateTime.UtcNow;
        if (Status == ComplianceExceptionStatus.Approved && EffectiveToUtc < now)
        {
            Status = ComplianceExceptionStatus.Expired;
            MarkUpdated();
        }
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
