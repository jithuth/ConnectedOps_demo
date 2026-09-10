using ConnectedOps.Domain.Tenancy;

namespace ConnectedOps.Application.TenantSettings;

public sealed class UpdateTenantGeneralSettingsRequest
{
    public string TimeZone { get; init; }
        = "UTC";

    public string CurrencyCode { get; init; }
        = "USD";

    public string DistanceUnit { get; init; }
        = "KM";

    public string FuelUnit { get; init; }
        = "LITRE";

    public string DateFormat { get; init; }
        = "dd/MM/yyyy";

    public string TimeFormat { get; init; }
        = "24H";

    public WeekStartDay WeekStartsOn { get; init; }
        = WeekStartDay.Monday;

    public string LanguageCode { get; init; }
        = "en";
}