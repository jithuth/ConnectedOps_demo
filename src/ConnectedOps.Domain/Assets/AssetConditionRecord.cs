using ConnectedOps.Domain.Common;
using ConnectedOps.Domain.Organization;

namespace ConnectedOps.Domain.Assets;

public sealed class AssetConditionRecord : BaseEntity
{
    private AssetConditionRecord()
    {
    }

    public AssetConditionRecord(
        Guid tenantId,
        Guid assetId,
        AssetCondition condition,
        DateTime? recordedAtUtc = null,
        Guid? employeeId = null,
        Guid? usageSessionId = null,
        string? description = null,
        string? photoObjectKey = null,
        Guid? recordedByUserId = null)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        if (assetId == Guid.Empty)
            throw new ArgumentException("AssetId is required.", nameof(assetId));

        TenantId = tenantId;
        AssetId = assetId;
        Condition = condition;
        RecordedAtUtc = recordedAtUtc ?? DateTime.UtcNow;
        EmployeeId = employeeId;
        UsageSessionId = usageSessionId;
        Description = description?.Trim();
        PhotoObjectKey = photoObjectKey?.Trim();
        RecordedByUserId = recordedByUserId;
        CreatedBy = recordedByUserId;
    }

    public Guid TenantId { get; private set; }
    public Guid AssetId { get; private set; }
    public Asset Asset { get; private set; } = null!;
    public AssetCondition Condition { get; private set; }
    public DateTime RecordedAtUtc { get; private set; }
    public Guid? EmployeeId { get; private set; }
    public Employee? Employee { get; private set; }
    public Guid? UsageSessionId { get; private set; }
    public AssetUsageSession? UsageSession { get; private set; }
    public string? Description { get; private set; }
    public string? PhotoObjectKey { get; private set; }
    public Guid? RecordedByUserId { get; private set; }
}
