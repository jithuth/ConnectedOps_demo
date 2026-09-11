using ConnectedOps.Domain.Billing;

namespace ConnectedOps.Application.Billing;

public sealed record SubscriptionPlanDto(
    Guid Id,
    string Name,
    string Code,
    decimal Price,
    string Currency,
    BillingInterval BillingInterval,
    string? Description,
    int MaxVehicles,
    int MaxAssets,
    int MaxUsers,
    int MaxStorageGb,
    bool HasAdvancedAnalytics,
    bool HasApiAccess,
    bool HasCustomBranding,
    bool HasAuditExport,
    bool IsActive,
    bool IsPublic,
    int SortOrder,
    DateTime CreatedAtUtc);

public sealed record CreateSubscriptionPlanRequest
{
    public string Name { get; init; } = string.Empty;
    public string Code { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public string Currency { get; init; } = "USD";
    public BillingInterval BillingInterval { get; init; } = BillingInterval.Monthly;
    public string? Description { get; init; }
    public int MaxVehicles { get; init; } = 50;
    public int MaxAssets { get; init; } = 100;
    public int MaxUsers { get; init; } = 10;
    public int MaxStorageGb { get; init; } = 20;
    public bool HasAdvancedAnalytics { get; init; }
    public bool HasApiAccess { get; init; }
    public bool HasCustomBranding { get; init; }
    public bool HasAuditExport { get; init; }
    public bool IsPublic { get; init; } = true;
    public int SortOrder { get; init; }
}

public sealed record UpdateSubscriptionPlanRequest
{
    public string Name { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public string Currency { get; init; } = "USD";
    public BillingInterval BillingInterval { get; init; } = BillingInterval.Monthly;
    public string? Description { get; init; }
    public int MaxVehicles { get; init; } = 50;
    public int MaxAssets { get; init; } = 100;
    public int MaxUsers { get; init; } = 10;
    public int MaxStorageGb { get; init; } = 20;
    public bool HasAdvancedAnalytics { get; init; }
    public bool HasApiAccess { get; init; }
    public bool HasCustomBranding { get; init; }
    public bool HasAuditExport { get; init; }
    public bool IsPublic { get; init; } = true;
    public int SortOrder { get; init; }
}
