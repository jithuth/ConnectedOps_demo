namespace ConnectedOps.Application.Billing;

public interface ISubscriptionPlanService
{
    Task<IReadOnlyCollection<SubscriptionPlanDto>> GetPublicPlansAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<SubscriptionPlanDto>> GetAllPlansAsync(CancellationToken cancellationToken = default);
    Task<SubscriptionPlanDto?> GetPlanByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<SubscriptionPlanDto> CreatePlanAsync(CreateSubscriptionPlanRequest request, CancellationToken cancellationToken = default);
    Task<SubscriptionPlanDto> UpdatePlanAsync(Guid id, UpdateSubscriptionPlanRequest request, CancellationToken cancellationToken = default);
    Task ActivatePlanAsync(Guid id, CancellationToken cancellationToken = default);
    Task DeactivatePlanAsync(Guid id, CancellationToken cancellationToken = default);
}
