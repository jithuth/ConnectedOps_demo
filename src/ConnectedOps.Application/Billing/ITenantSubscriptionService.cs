namespace ConnectedOps.Application.Billing;

public interface ITenantSubscriptionService
{
    Task<TenantSubscriptionDto?> GetCurrentSubscriptionAsync(CancellationToken cancellationToken = default);
    Task<TenantSubscriptionDto> ChangePlanAsync(ChangeSubscriptionPlanRequest request, CancellationToken cancellationToken = default);
    Task<TenantSubscriptionDto> CancelSubscriptionAsync(CancelSubscriptionRequest request, CancellationToken cancellationToken = default);
    Task<TenantSubscriptionDto> ReactivateSubscriptionAsync(CancellationToken cancellationToken = default);
    Task<TenantSubscriptionDto> SetCustomQuotasAsync(SetCustomQuotasRequest request, CancellationToken cancellationToken = default);
}
