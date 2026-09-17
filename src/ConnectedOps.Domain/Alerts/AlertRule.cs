using ConnectedOps.Domain.Common;

namespace ConnectedOps.Domain.Alerts;

public sealed class AlertRule : BaseEntity
{
    private readonly List<AlertRuleCondition> _conditions = [];

    private AlertRule()
    {
    }

    public AlertRule(
        Guid tenantId,
        string code,
        string name,
        AlertSourceType sourceType,
        AlertSeverity severity,
        int cooldownMinutes = 15,
        string? description = null,
        bool isEnabled = true,
        Guid? createdByUserId = null)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Code is required.", nameof(code));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required.", nameof(name));

        TenantId = tenantId;
        Code = code.Trim().ToUpperInvariant();
        Name = name.Trim();
        SourceType = sourceType;
        Severity = severity;
        CooldownMinutes = Math.Max(1, cooldownMinutes);
        Description = description?.Trim();
        IsEnabled = isEnabled;
        CreatedBy = createdByUserId;
    }

    public Guid TenantId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public AlertSourceType SourceType { get; private set; }
    public AlertSeverity Severity { get; private set; }
    public int CooldownMinutes { get; private set; }
    public bool IsEnabled { get; private set; }

    public IReadOnlyCollection<AlertRuleCondition> Conditions => _conditions.AsReadOnly();

    public void AddCondition(AlertRuleCondition condition)
    {
        ArgumentNullException.ThrowIfNull(condition);
        _conditions.Add(condition);
    }

    public void SetEnabled(bool enabled, Guid? updatedBy = null)
    {
        IsEnabled = enabled;
        MarkUpdated(updatedBy);
    }

    public void Update(
        string name,
        AlertSeverity severity,
        int cooldownMinutes,
        string? description = null,
        Guid? updatedBy = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required.", nameof(name));

        Name = name.Trim();
        Severity = severity;
        CooldownMinutes = Math.Max(1, cooldownMinutes);
        Description = description?.Trim();
        MarkUpdated(updatedBy);
    }
}
