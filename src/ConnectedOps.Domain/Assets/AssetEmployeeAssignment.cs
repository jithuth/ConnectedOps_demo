using ConnectedOps.Domain.Common;
using ConnectedOps.Domain.Organization;

namespace ConnectedOps.Domain.Assets;

public sealed class AssetEmployeeAssignment : BaseEntity
{
    private AssetEmployeeAssignment()
    {
    }

    public AssetEmployeeAssignment(
        Guid tenantId,
        Guid assetId,
        Guid employeeId,
        DateTime assignedFromUtc,
        DateTime? assignedToUtc = null,
        AssetAssignmentType assignmentType = AssetAssignmentType.Permanent,
        AssetCondition? conditionAtAssignment = null,
        string? notes = null,
        bool isActive = true,
        Guid? assignedByUserId = null)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        if (assetId == Guid.Empty)
            throw new ArgumentException("AssetId is required.", nameof(assetId));
        if (employeeId == Guid.Empty)
            throw new ArgumentException("EmployeeId is required.", nameof(employeeId));

        TenantId = tenantId;
        AssetId = assetId;
        EmployeeId = employeeId;
        AssignedFromUtc = assignedFromUtc;
        AssignedToUtc = assignedToUtc;
        AssignmentType = assignmentType;
        ConditionAtAssignment = conditionAtAssignment;
        Notes = notes?.Trim();
        IsActive = isActive;
        AssignedByUserId = assignedByUserId;
        CreatedBy = assignedByUserId;
    }

    public Guid TenantId { get; private set; }
    public Guid AssetId { get; private set; }
    public Asset Asset { get; private set; } = null!;
    public Guid EmployeeId { get; private set; }
    public Employee Employee { get; private set; } = null!;
    public DateTime AssignedFromUtc { get; private set; }
    public DateTime? AssignedToUtc { get; private set; }
    public AssetAssignmentType AssignmentType { get; private set; }
    public bool IsActive { get; private set; }
    public Guid? AssignedByUserId { get; private set; }
    public Guid? ReturnedByUserId { get; private set; }
    public AssetCondition? ConditionAtAssignment { get; private set; }
    public AssetCondition? ConditionAtReturn { get; private set; }
    public string? Notes { get; private set; }

    public void EndAssignment(
        DateTime assignedToUtc,
        AssetCondition? conditionAtReturn = null,
        string? returnNotes = null,
        Guid? returnedByUserId = null)
    {
        AssignedToUtc = assignedToUtc;
        ConditionAtReturn = conditionAtReturn;
        if (!string.IsNullOrWhiteSpace(returnNotes))
        {
            Notes = string.IsNullOrWhiteSpace(Notes) ? returnNotes.Trim() : $"{Notes} | Return notes: {returnNotes.Trim()}";
        }
        IsActive = false;
        ReturnedByUserId = returnedByUserId;
        MarkUpdated(returnedByUserId);
    }
}
