using ConnectedOps.Domain.Alerts;

namespace ConnectedOps.Application.Alerts;

public sealed record AlertFilterRequest(
    AlertStatus? Status = null,
    AlertSeverity? Severity = null,
    AlertSourceType? SourceType = null,
    Guid? VehicleId = null,
    string? SearchTerm = null,
    int PageNumber = 1,
    int PageSize = 20);

public sealed record AlertRuleDto(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    AlertSourceType SourceType,
    string SourceTypeName,
    AlertSeverity Severity,
    string SeverityName,
    int CooldownMinutes,
    bool IsEnabled,
    List<AlertRuleConditionDto> Conditions);

public sealed record AlertRuleConditionDto(
    Guid Id,
    string FieldName,
    ConditionOperator Operator,
    string OperatorName,
    string ThresholdValue,
    int SortOrder);

public sealed record CreateAlertRuleRequest(
    string Code,
    string Name,
    AlertSourceType SourceType,
    AlertSeverity Severity,
    int CooldownMinutes = 15,
    string? Description = null,
    List<CreateAlertRuleConditionRequest>? Conditions = null);

public sealed record CreateAlertRuleConditionRequest(
    string FieldName,
    ConditionOperator Operator,
    string ThresholdValue,
    int SortOrder = 1);

public sealed record UpdateAlertRuleRequest(
    string Name,
    AlertSeverity Severity,
    int CooldownMinutes,
    string? Description = null,
    bool IsEnabled = true);

public sealed record AlertDto(
    Guid Id,
    string Title,
    string Message,
    AlertSeverity Severity,
    string SeverityName,
    AlertStatus Status,
    string StatusName,
    AlertSourceType SourceType,
    string SourceTypeName,
    Guid? VehicleId,
    string? VehiclePlateNumber,
    Guid? DriverId,
    string? DriverName,
    double? Latitude,
    double? Longitude,
    DateTime TriggeredAtUtc,
    DateTime? AcknowledgedAtUtc,
    DateTime? ResolvedAtUtc,
    string? ResolutionNotes);

public sealed record NotificationMessageDto(
    Guid Id,
    NotificationChannelType Channel,
    string ChannelName,
    string Recipient,
    string Title,
    string Body,
    bool IsDelivered,
    string? ExternalMessageId,
    DateTime SentAtUtc);

public sealed record SendNotificationRequest(
    NotificationChannelType Channel,
    string Recipient,
    string Title,
    string Body,
    Guid? AlertId = null);

public sealed record AlertDashboardDto(
    int TotalActiveAlerts,
    int CriticalAlertsCount,
    int WarningAlertsCount,
    int ResolvedTodayCount,
    int NotificationsSentTodayCount,
    List<AlertDto> RecentActiveAlerts,
    List<NotificationMessageDto> RecentDispatches);
