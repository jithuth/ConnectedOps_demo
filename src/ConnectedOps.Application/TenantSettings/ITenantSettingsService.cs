namespace ConnectedOps.Application.TenantSettings;

public interface ITenantSettingsService
{
    Task<TenantSettingsResult> GetAsync(
        CancellationToken cancellationToken = default);

    Task<TenantSettingsResult> UpdateGeneralAsync(
        UpdateTenantGeneralSettingsRequest request,
        CancellationToken cancellationToken = default);

    Task<TenantSettingsResult> UpdateNotificationsAsync(
        UpdateTenantNotificationSettingsRequest request,
        CancellationToken cancellationToken = default);
}