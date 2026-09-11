using ConnectedOps.Domain.Common;

namespace ConnectedOps.Domain.Billing;

public sealed class SubscriptionPlan : BaseEntity
{
    private SubscriptionPlan()
    {
    }

    public SubscriptionPlan(
        string name,
        string code,
        decimal price,
        BillingInterval billingInterval = BillingInterval.Monthly,
        string currency = "USD",
        string? description = null,
        int maxVehicles = 50,
        int maxAssets = 100,
        int maxUsers = 10,
        int maxStorageGb = 20,
        bool hasAdvancedAnalytics = false,
        bool hasApiAccess = false,
        bool hasCustomBranding = false,
        bool hasAuditExport = false,
        bool isPublic = true,
        int sortOrder = 0)
    {
        SetName(name);
        SetCode(code);
        SetPrice(price, currency);
        BillingInterval = billingInterval;
        Description = description?.Trim();
        MaxVehicles = maxVehicles;
        MaxAssets = maxAssets;
        MaxUsers = maxUsers;
        MaxStorageGb = maxStorageGb;
        HasAdvancedAnalytics = hasAdvancedAnalytics;
        HasApiAccess = hasApiAccess;
        HasCustomBranding = hasCustomBranding;
        HasAuditExport = hasAuditExport;
        IsActive = true;
        IsPublic = isPublic;
        SortOrder = sortOrder;
    }

    public string Name { get; private set; } = string.Empty;
    public string Code { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public decimal Price { get; private set; }
    public string Currency { get; private set; } = "USD";
    public BillingInterval BillingInterval { get; private set; }
    public int MaxVehicles { get; private set; }
    public int MaxAssets { get; private set; }
    public int MaxUsers { get; private set; }
    public int MaxStorageGb { get; private set; }
    public bool HasAdvancedAnalytics { get; private set; }
    public bool HasApiAccess { get; private set; }
    public bool HasCustomBranding { get; private set; }
    public bool HasAuditExport { get; private set; }
    public bool IsActive { get; private set; }
    public bool IsPublic { get; private set; }
    public int SortOrder { get; private set; }

    public void SetName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Plan name is required.", nameof(name));
        if (name.Length > 100)
            throw new ArgumentException("Plan name cannot exceed 100 characters.", nameof(name));

        Name = name.Trim();
        MarkUpdated();
    }

    public void SetCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Plan code is required.", nameof(code));
        if (code.Length > 50)
            throw new ArgumentException("Plan code cannot exceed 50 characters.", nameof(code));

        Code = code.Trim().ToUpperInvariant();
        MarkUpdated();
    }

    public void SetPrice(decimal price, string currency = "USD")
    {
        if (price < 0)
            throw new ArgumentException("Price cannot be negative.", nameof(price));

        Price = price;
        Currency = string.IsNullOrWhiteSpace(currency) ? "USD" : currency.Trim().ToUpperInvariant();
        MarkUpdated();
    }

    public void UpdateDetails(
        string name,
        string? description,
        decimal price,
        BillingInterval billingInterval,
        string currency,
        int maxVehicles,
        int maxAssets,
        int maxUsers,
        int maxStorageGb,
        bool hasAdvancedAnalytics,
        bool hasApiAccess,
        bool hasCustomBranding,
        bool hasAuditExport,
        bool isPublic,
        int sortOrder)
    {
        SetName(name);
        SetPrice(price, currency);
        BillingInterval = billingInterval;
        Description = description?.Trim();
        MaxVehicles = Math.Max(1, maxVehicles);
        MaxAssets = Math.Max(1, maxAssets);
        MaxUsers = Math.Max(1, maxUsers);
        MaxStorageGb = Math.Max(1, maxStorageGb);
        HasAdvancedAnalytics = hasAdvancedAnalytics;
        HasApiAccess = hasApiAccess;
        HasCustomBranding = hasCustomBranding;
        HasAuditExport = hasAuditExport;
        IsPublic = isPublic;
        SortOrder = sortOrder;
        MarkUpdated();
    }

    public void Activate()
    {
        IsActive = true;
        MarkUpdated();
    }

    public void Deactivate()
    {
        IsActive = false;
        MarkUpdated();
    }
}
