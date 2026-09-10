using ConnectedOps.Domain.Tenancy;

namespace ConnectedOps.Application.TenantSettings;

public sealed record TenantSettingsResult(
    Guid TenantId,
    string TimeZone,
    string CurrencyCode,
    string DistanceUnit,
    string FuelUnit,
    string DateFormat,
    string TimeFormat,
    WeekStartDay WeekStartsOn,
    string LanguageCode,
    bool EnableNotifications,
    bool EnableEmailNotifications,
    bool EnableSmsNotifications,
    bool EnablePushNotifications);