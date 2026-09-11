using ConnectedOps.Domain.Billing;

namespace ConnectedOps.Application.Billing;

public sealed record TenantSubscriptionDto(
    Guid Id,
    Guid TenantId,
    Guid PlanId,
    string PlanName,
    string PlanCode,
    decimal PlanPrice,
    string Currency,
    BillingInterval BillingInterval,
    SubscriptionStatus Status,
    DateTime CurrentPeriodStartUtc,
    DateTime CurrentPeriodEndUtc,
    DateTime? TrialEndUtc,
    bool AutoRenew,
    bool IsInTrial,
    DateTime? CancelledAtUtc,
    string? CancellationReason,
    int EffectiveMaxVehicles,
    int EffectiveMaxAssets,
    int EffectiveMaxUsers,
    int CurrentVehicleCount,
    int CurrentAssetCount,
    int CurrentUserCount,
    int? CustomMaxVehicles,
    int? CustomMaxAssets,
    int? CustomMaxUsers);

public sealed record ChangeSubscriptionPlanRequest
{
    public Guid NewPlanId { get; init; }
}

public sealed record CancelSubscriptionRequest
{
    public string? Reason { get; init; }
}

public sealed record SetCustomQuotasRequest
{
    public int? CustomMaxVehicles { get; init; }
    public int? CustomMaxAssets { get; init; }
    public int? CustomMaxUsers { get; init; }
}
