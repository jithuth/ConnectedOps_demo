using ConnectedOps.Domain.Common;

namespace ConnectedOps.Domain.Reports;

public enum ReportCategory
{
    Executive = 1,
    Financial = 2,
    Operational = 3,
    Sustainability = 4,
    Safety = 5,
    Custom = 6
}

public sealed class ReportDefinition : BaseEntity
{
    private ReportDefinition()
    {
    }

    public ReportDefinition(
        Guid? tenantId,
        string code,
        string name,
        ReportCategory category,
        string description,
        string? parametersSchema = null,
        bool isSystem = true,
        bool isActive = true)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Code is required.", nameof(code));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required.", nameof(name));

        TenantId = tenantId;
        Code = code.Trim().ToUpperInvariant();
        Name = name.Trim();
        Category = category;
        Description = description?.Trim() ?? string.Empty;
        ParametersSchema = parametersSchema;
        IsSystem = isSystem;
        IsActive = isActive;
    }

    public Guid? TenantId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public ReportCategory Category { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public string? ParametersSchema { get; private set; }
    public bool IsSystem { get; private set; }
    public bool IsActive { get; private set; }

    public void Update(
        string name,
        ReportCategory category,
        string description,
        string? parametersSchema,
        bool isActive,
        Guid? updatedBy = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required.", nameof(name));

        Name = name.Trim();
        Category = category;
        Description = description?.Trim() ?? string.Empty;
        ParametersSchema = parametersSchema;
        IsActive = isActive;
        MarkUpdated(updatedBy);
    }
}
