using ConnectedOps.Domain.Common;

namespace ConnectedOps.Domain.Tenancy;

public sealed class TenantSettings : BaseEntity
{
    private TenantSettings()
    {
    }

    public TenantSettings(Guid tenantId)
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException(
                "TenantId is required.",
                nameof(tenantId));
        }

        TenantId = tenantId;

        TimeZone = "UTC";
        CurrencyCode = "USD";
        DistanceUnit = "KM";
        FuelUnit = "LITRE";
        DateFormat = "dd/MM/yyyy";
        TimeFormat = "24H";
        WeekStartsOn = WeekStartDay.Monday;
        LanguageCode = "en";

        EnableNotifications = true;
        EnableEmailNotifications = true;
        EnableSmsNotifications = false;
        EnablePushNotifications = true;
    }

    public Guid TenantId { get; private set; }

    public string TimeZone { get; private set; }
        = "UTC";

    public string CurrencyCode { get; private set; }
        = "USD";

    public string DistanceUnit { get; private set; }
        = "KM";

    public string FuelUnit { get; private set; }
        = "LITRE";

    public string DateFormat { get; private set; }
        = "dd/MM/yyyy";

    public string TimeFormat { get; private set; }
        = "24H";

    public WeekStartDay WeekStartsOn { get; private set; }
        = WeekStartDay.Monday;

    public string LanguageCode { get; private set; }
        = "en";

    public bool EnableNotifications { get; private set; }

    public bool EnableEmailNotifications { get; private set; }

    public bool EnableSmsNotifications { get; private set; }

    public bool EnablePushNotifications { get; private set; }

    public Tenant Tenant { get; private set; }
        = null!;

    public void UpdateGeneralSettings(
        string timeZone,
        string currencyCode,
        string distanceUnit,
        string fuelUnit,
        string dateFormat,
        string timeFormat,
        WeekStartDay weekStartsOn,
        string languageCode)
    {
        SetTimeZone(timeZone);
        SetCurrencyCode(currencyCode);
        SetDistanceUnit(distanceUnit);
        SetFuelUnit(fuelUnit);
        SetDateFormat(dateFormat);
        SetTimeFormat(timeFormat);
        SetLanguageCode(languageCode);

        WeekStartsOn = weekStartsOn;

        MarkUpdated();
    }

    public void UpdateNotificationSettings(
        bool enableNotifications,
        bool enableEmailNotifications,
        bool enableSmsNotifications,
        bool enablePushNotifications)
    {
        EnableNotifications = enableNotifications;

        if (!enableNotifications)
        {
            EnableEmailNotifications = false;
            EnableSmsNotifications = false;
            EnablePushNotifications = false;
        }
        else
        {
            EnableEmailNotifications =
                enableEmailNotifications;

            EnableSmsNotifications =
                enableSmsNotifications;

            EnablePushNotifications =
                enablePushNotifications;
        }

        MarkUpdated();
    }

    private void SetTimeZone(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(
                "Timezone is required.",
                nameof(value));
        }

        var normalized = value.Trim();

        if (normalized.Length > 100)
        {
            throw new ArgumentException(
                "Timezone cannot exceed 100 characters.",
                nameof(value));
        }

        TimeZone = normalized;
    }

    private void SetCurrencyCode(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(
                "Currency code is required.",
                nameof(value));
        }

        var normalized =
            value.Trim().ToUpperInvariant();

        if (normalized.Length != 3)
        {
            throw new ArgumentException(
                "Currency code must contain exactly 3 characters.",
                nameof(value));
        }

        CurrencyCode = normalized;
    }

    private void SetDistanceUnit(string value)
    {
        var normalized =
            value?.Trim().ToUpperInvariant();

        if (normalized is not ("KM" or "MI"))
        {
            throw new ArgumentException(
                "Distance unit must be KM or MI.",
                nameof(value));
        }

        DistanceUnit = normalized;
    }

    private void SetFuelUnit(string value)
    {
        var normalized =
            value?.Trim().ToUpperInvariant();

        if (normalized is not ("LITRE" or "GALLON"))
        {
            throw new ArgumentException(
                "Fuel unit must be LITRE or GALLON.",
                nameof(value));
        }

        FuelUnit = normalized;
    }

    private void SetDateFormat(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(
                "Date format is required.",
                nameof(value));
        }

        var normalized = value.Trim();

        if (normalized.Length > 30)
        {
            throw new ArgumentException(
                "Date format cannot exceed 30 characters.",
                nameof(value));
        }

        DateFormat = normalized;
    }

    private void SetTimeFormat(string value)
    {
        var normalized =
            value?.Trim().ToUpperInvariant();

        if (normalized is not ("12H" or "24H"))
        {
            throw new ArgumentException(
                "Time format must be 12H or 24H.",
                nameof(value));
        }

        TimeFormat = normalized;
    }

    private void SetLanguageCode(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(
                "Language code is required.",
                nameof(value));
        }

        var normalized =
            value.Trim().ToLowerInvariant();

        if (normalized.Length > 10)
        {
            throw new ArgumentException(
                "Language code cannot exceed 10 characters.",
                nameof(value));
        }

        LanguageCode = normalized;
    }
}