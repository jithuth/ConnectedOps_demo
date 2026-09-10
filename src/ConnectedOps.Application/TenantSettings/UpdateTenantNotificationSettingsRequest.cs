namespace ConnectedOps.Application.TenantSettings;

public sealed class UpdateTenantNotificationSettingsRequest
{
    public bool EnableNotifications { get; init; }

    public bool EnableEmailNotifications { get; init; }

    public bool EnableSmsNotifications { get; init; }

    public bool EnablePushNotifications { get; init; }
}