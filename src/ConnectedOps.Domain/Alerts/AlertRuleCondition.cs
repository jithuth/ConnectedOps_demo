using ConnectedOps.Domain.Common;

namespace ConnectedOps.Domain.Alerts;

public sealed class AlertRuleCondition : BaseEntity
{
    private AlertRuleCondition()
    {
    }

    public AlertRuleCondition(
        Guid tenantId,
        Guid alertRuleId,
        string fieldName,
        ConditionOperator op,
        string thresholdValue,
        int sortOrder = 1)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        if (alertRuleId == Guid.Empty)
            throw new ArgumentException("AlertRuleId is required.", nameof(alertRuleId));
        if (string.IsNullOrWhiteSpace(fieldName))
            throw new ArgumentException("FieldName is required.", nameof(fieldName));
        if (string.IsNullOrWhiteSpace(thresholdValue))
            throw new ArgumentException("ThresholdValue is required.", nameof(thresholdValue));

        TenantId = tenantId;
        AlertRuleId = alertRuleId;
        FieldName = fieldName.Trim();
        Operator = op;
        ThresholdValue = thresholdValue.Trim();
        SortOrder = sortOrder;
    }

    public Guid TenantId { get; private set; }
    public Guid AlertRuleId { get; private set; }
    public AlertRule AlertRule { get; set; } = null!;
    public string FieldName { get; private set; } = string.Empty;
    public ConditionOperator Operator { get; private set; }
    public string ThresholdValue { get; private set; } = string.Empty;
    public int SortOrder { get; private set; }
}
