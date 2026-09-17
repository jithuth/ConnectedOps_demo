using ConnectedOps.Application.Vehicles;

namespace ConnectedOps.Application.Alerts;

public interface IAlertService
{
    Task<AlertDashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default);

    Task<PagedResult<AlertDto>> GetAlertsPagedAsync(
        AlertFilterRequest request,
        CancellationToken cancellationToken = default);

    Task<AlertDto?> GetAlertByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<AlertDto> AcknowledgeAlertAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<AlertDto> ResolveAlertAsync(
        Guid id,
        string? notes = null,
        CancellationToken cancellationToken = default);

    Task<AlertDto> DismissAlertAsync(
        Guid id,
        string? reason = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<AlertRuleDto>> GetRulesAsync(
        CancellationToken cancellationToken = default);

    Task<AlertRuleDto> CreateRuleAsync(
        CreateAlertRuleRequest request,
        CancellationToken cancellationToken = default);

    Task<AlertRuleDto> UpdateRuleAsync(
        Guid id,
        UpdateAlertRuleRequest request,
        CancellationToken cancellationToken = default);

    Task<NotificationMessageDto> SendNotificationAsync(
        SendNotificationRequest request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<NotificationMessageDto>> GetRecentNotificationsAsync(
        int count = 20,
        CancellationToken cancellationToken = default);
}
