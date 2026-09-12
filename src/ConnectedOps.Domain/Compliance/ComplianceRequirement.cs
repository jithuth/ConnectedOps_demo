using ConnectedOps.Domain.Assets;
using ConnectedOps.Domain.Common;
using ConnectedOps.Domain.Drivers;
using ConnectedOps.Domain.Organization;
using ConnectedOps.Domain.Vehicles;

namespace ConnectedOps.Domain.Compliance;

public sealed class ComplianceRequirement : BaseEntity
{
    private readonly List<ComplianceRequirementRule> _rules = [];
    private readonly List<ComplianceRecord> _records = [];

    private ComplianceRequirement()
    {
    }

    public ComplianceRequirement(
        Guid tenantId,
        string code,
        string name,
        ComplianceSubjectType appliesTo,
        ComplianceRequirementType requirementType,
        ComplianceValidityType validityType = ComplianceValidityType.Recurring,
        string? description = null,
        int? defaultValidityDays = null,
        int? defaultReminderDays = 30,
        bool isMandatory = true,
        bool isActive = true,
        Guid? createdByUserId = null)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Requirement code is required.", nameof(code));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Requirement name is required.", nameof(name));

        TenantId = tenantId;
        Code = code.Trim().ToUpperInvariant();
        Name = name.Trim();
        Description = description?.Trim();
        AppliesTo = appliesTo;
        RequirementType = requirementType;
        ValidityType = validityType;
        DefaultValidityDays = defaultValidityDays;
        DefaultReminderDays = defaultReminderDays ?? 30;
        IsMandatory = isMandatory;
        IsActive = isActive;
        CreatedBy = createdByUserId;
    }

    public Guid TenantId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public ComplianceSubjectType AppliesTo { get; private set; }
    public ComplianceRequirementType RequirementType { get; private set; }
    public ComplianceValidityType ValidityType { get; private set; }
    public int? DefaultValidityDays { get; private set; }
    public int DefaultReminderDays { get; private set; } = 30;
    public bool IsMandatory { get; private set; }
    public bool IsActive { get; private set; }

    public IReadOnlyCollection<ComplianceRequirementRule> Rules => _rules;
    public IReadOnlyCollection<ComplianceRecord> Records => _records;

    public void Update(
        string name,
        string? description,
        ComplianceSubjectType appliesTo,
        ComplianceRequirementType requirementType,
        ComplianceValidityType validityType,
        int? defaultValidityDays,
        int defaultReminderDays,
        bool isMandatory,
        bool isActive,
        Guid? updatedByUserId = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Requirement name is required.", nameof(name));

        Name = name.Trim();
        Description = description?.Trim();
        AppliesTo = appliesTo;
        RequirementType = requirementType;
        ValidityType = validityType;
        DefaultValidityDays = defaultValidityDays;
        DefaultReminderDays = defaultReminderDays;
        IsMandatory = isMandatory;
        IsActive = isActive;
        UpdatedBy = updatedByUserId;
        MarkUpdated();
    }

    public void AddRule(ComplianceRequirementRule rule)
    {
        ArgumentNullException.ThrowIfNull(rule);
        if (rule.TenantId != TenantId)
            throw new InvalidOperationException("Tenant mismatch between requirement and rule.");
        _rules.Add(rule);
    }

    public void RemoveRule(Guid ruleId)
    {
        var rule = _rules.FirstOrDefault(r => r.Id == ruleId);
        if (rule != null)
        {
            _rules.Remove(rule);
        }
    }
}
