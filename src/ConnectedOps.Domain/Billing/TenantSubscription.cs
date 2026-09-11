using ConnectedOps.Domain.Common;

namespace ConnectedOps.Domain.Billing;

public sealed class TenantSubscription : BaseEntity
{
    private TenantSubscription()
    {
    }

    public TenantSubscription(
        Guid tenantId,
        Guid planId,
        DateTime currentPeriodStartUtc,
        DateTime currentPeriodEndUtc,
        SubscriptionStatus status = SubscriptionStatus.Active,
        bool autoRenew = true,
        DateTime? trialEndUtc = null)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        if (planId == Guid.Empty)
            throw new ArgumentException("PlanId is required.", nameof(planId));

        TenantId = tenantId;
        PlanId = planId;
        CurrentPeriodStartUtc = currentPeriodStartUtc;
        CurrentPeriodEndUtc = currentPeriodEndUtc;
        Status = status;
        AutoRenew = autoRenew;
        TrialEndUtc = trialEndUtc;
    }

    public Guid TenantId { get; private set; }
    public Guid PlanId { get; private set; }
    public SubscriptionPlan Plan { get; private set; } = null!;

    public SubscriptionStatus Status { get; private set; }
    public DateTime CurrentPeriodStartUtc { get; private set; }
    public DateTime CurrentPeriodEndUtc { get; private set; }
    public DateTime? TrialEndUtc { get; private set; }
    public bool AutoRenew { get; private set; }
    public DateTime? CancelledAtUtc { get; private set; }
    public string? CancellationReason { get; private set; }

    public int? CustomMaxVehicles { get; private set; }
    public int? CustomMaxAssets { get; private set; }
    public int? CustomMaxUsers { get; private set; }

    public bool IsInTrial =>
        Status == SubscriptionStatus.Trialing &&
        TrialEndUtc.HasValue &&
        TrialEndUtc.Value > DateTime.UtcNow;

    public bool IsActiveOrInTrial =>
        Status == SubscriptionStatus.Active ||
        IsInTrial;

    public void ChangePlan(Guid newPlanId, DateTime newPeriodEndUtc)
    {
        if (newPlanId == Guid.Empty)
            throw new ArgumentException("PlanId is required.", nameof(newPlanId));

        PlanId = newPlanId;
        CurrentPeriodStartUtc = DateTime.UtcNow;
        CurrentPeriodEndUtc = newPeriodEndUtc;
        Status = SubscriptionStatus.Active;
        MarkUpdated();
    }

    public void Renew(DateTime nextPeriodEndUtc)
    {
        CurrentPeriodStartUtc = CurrentPeriodEndUtc;
        CurrentPeriodEndUtc = nextPeriodEndUtc;
        Status = SubscriptionStatus.Active;
        MarkUpdated();
    }

    public void Cancel(string? reason = null)
    {
        Status = SubscriptionStatus.Cancelled;
        CancelledAtUtc = DateTime.UtcNow;
        CancellationReason = reason?.Trim();
        AutoRenew = false;
        MarkUpdated();
    }

    public void MarkPastDue()
    {
        Status = SubscriptionStatus.PastDue;
        MarkUpdated();
    }

    public void MarkExpired()
    {
        Status = SubscriptionStatus.Expired;
        AutoRenew = false;
        MarkUpdated();
    }

    public void Reactivate()
    {
        Status = SubscriptionStatus.Active;
        CancelledAtUtc = null;
        CancellationReason = null;
        AutoRenew = true;
        MarkUpdated();
    }

    public void SetCustomQuotas(int? maxVehicles, int? maxAssets, int? maxUsers)
    {
        CustomMaxVehicles = maxVehicles > 0 ? maxVehicles : null;
        CustomMaxAssets = maxAssets > 0 ? maxAssets : null;
        CustomMaxUsers = maxUsers > 0 ? maxUsers : null;
        MarkUpdated();
    }

    public void SetAutoRenew(bool autoRenew)
    {
        AutoRenew = autoRenew;
        MarkUpdated();
    }
}
