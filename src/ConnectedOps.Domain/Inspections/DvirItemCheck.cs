using ConnectedOps.Domain.Common;

namespace ConnectedOps.Domain.Inspections;

public sealed class DvirItemCheck : BaseEntity
{
    private DvirItemCheck()
    {
    }

    public DvirItemCheck(
        Guid tenantId,
        Guid dvirInspectionId,
        string category,
        string itemName,
        bool isPassed,
        DefectSeverity? severity = null,
        string? defectDescription = null)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        if (dvirInspectionId == Guid.Empty)
            throw new ArgumentException("DvirInspectionId is required.", nameof(dvirInspectionId));
        if (string.IsNullOrWhiteSpace(category))
            throw new ArgumentException("Category is required.", nameof(category));
        if (string.IsNullOrWhiteSpace(itemName))
            throw new ArgumentException("ItemName is required.", nameof(itemName));

        TenantId = tenantId;
        DvirInspectionId = dvirInspectionId;
        Category = category.Trim();
        ItemName = itemName.Trim();
        IsPassed = isPassed;
        Severity = isPassed ? null : (severity ?? DefectSeverity.Minor);
        DefectDescription = isPassed ? null : defectDescription?.Trim();
    }

    public Guid TenantId { get; private set; }
    public Guid DvirInspectionId { get; private set; }
    public DvirInspection DvirInspection { get; set; } = null!;

    public string Category { get; private set; } = string.Empty;
    public string ItemName { get; private set; } = string.Empty;
    public bool IsPassed { get; private set; }
    public DefectSeverity? Severity { get; private set; }
    public string? DefectDescription { get; private set; }
}
