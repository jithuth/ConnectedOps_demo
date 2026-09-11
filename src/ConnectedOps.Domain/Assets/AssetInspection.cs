using ConnectedOps.Domain.Common;
using ConnectedOps.Domain.Organization;

namespace ConnectedOps.Domain.Assets;

public sealed class AssetInspection : BaseEntity
{
    private readonly List<AssetInspectionItem> _items = [];

    private AssetInspection()
    {
    }

    public AssetInspection(
        Guid tenantId,
        Guid assetId,
        AssetInspectionType inspectionType,
        DateTime inspectionDateUtc,
        Guid? inspectorEmployeeId = null,
        AssetCondition condition = AssetCondition.Good,
        AssetInspectionResult result = AssetInspectionResult.Passed,
        DateTime? nextInspectionDateUtc = null,
        string? notes = null,
        AssetInspectionStatus status = AssetInspectionStatus.Completed,
        DateTime? completedAtUtc = null,
        Guid? createdByUserId = null)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        if (assetId == Guid.Empty)
            throw new ArgumentException("AssetId is required.", nameof(assetId));

        TenantId = tenantId;
        AssetId = assetId;
        InspectionType = inspectionType;
        InspectionDateUtc = inspectionDateUtc;
        InspectorEmployeeId = inspectorEmployeeId;
        Condition = condition;
        Result = result;
        NextInspectionDateUtc = nextInspectionDateUtc;
        Notes = notes?.Trim();
        Status = status;
        CompletedAtUtc = status == AssetInspectionStatus.Completed ? (completedAtUtc ?? DateTime.UtcNow) : null;
        CreatedBy = createdByUserId;
    }

    public Guid TenantId { get; private set; }
    public Guid AssetId { get; private set; }
    public Asset Asset { get; private set; } = null!;
    public AssetInspectionType InspectionType { get; private set; }
    public DateTime InspectionDateUtc { get; private set; }
    public Guid? InspectorEmployeeId { get; private set; }
    public Employee? InspectorEmployee { get; private set; }
    public AssetCondition Condition { get; private set; }
    public AssetInspectionResult Result { get; private set; }
    public DateTime? NextInspectionDateUtc { get; private set; }
    public string? Notes { get; private set; }
    public AssetInspectionStatus Status { get; private set; }
    public DateTime? CompletedAtUtc { get; private set; }
    public Guid? CompletedByUserId { get; private set; }

    public IReadOnlyCollection<AssetInspectionItem> Items => _items.AsReadOnly();

    public void AddItem(AssetInspectionItem item)
    {
        _items.Add(item);
    }

    public void Complete(
        AssetInspectionResult result,
        AssetCondition condition,
        DateTime? nextInspectionDateUtc = null,
        string? notes = null,
        Guid? completedByUserId = null)
    {
        Result = result;
        Condition = condition;
        NextInspectionDateUtc = nextInspectionDateUtc;
        if (!string.IsNullOrWhiteSpace(notes))
        {
            Notes = string.IsNullOrWhiteSpace(Notes) ? notes.Trim() : $"{Notes} | Completion: {notes.Trim()}";
        }
        Status = AssetInspectionStatus.Completed;
        CompletedAtUtc = DateTime.UtcNow;
        CompletedByUserId = completedByUserId;
        MarkUpdated(completedByUserId);
    }

    public void Cancel(string? reason = null, Guid? cancelledByUserId = null)
    {
        Status = AssetInspectionStatus.Cancelled;
        if (!string.IsNullOrWhiteSpace(reason))
        {
            Notes = string.IsNullOrWhiteSpace(Notes) ? reason.Trim() : $"{Notes} | Cancelled: {reason.Trim()}";
        }
        MarkUpdated(cancelledByUserId);
    }
}
