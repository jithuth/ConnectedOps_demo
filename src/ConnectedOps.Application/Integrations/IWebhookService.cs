using ConnectedOps.Application.Vehicles;
using ConnectedOps.Domain.Integrations;

namespace ConnectedOps.Application.Integrations;

public interface IWebhookService
{
    Task<IReadOnlyCollection<WebhookSubscriptionDto>> GetSubscriptionsAsync(CancellationToken cancellationToken = default);

    Task<WebhookSubscriptionDto> CreateSubscriptionAsync(CreateWebhookRequest request, CancellationToken cancellationToken = default);

    Task<WebhookSubscriptionDto> UpdateSubscriptionAsync(Guid id, UpdateWebhookRequest request, CancellationToken cancellationToken = default);

    Task<bool> ToggleSubscriptionAsync(Guid id, bool isEnabled, CancellationToken cancellationToken = default);

    Task<bool> DeleteSubscriptionAsync(Guid id, CancellationToken cancellationToken = default);

    Task<WebhookDeliveryAttemptDto> TriggerTestPingAsync(Guid id, CancellationToken cancellationToken = default);

    Task<PagedResult<WebhookDeliveryAttemptDto>> GetDeliveryAttemptsPagedAsync(Guid? subscriptionId = null, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);

    Task DispatchEventAsync(WebhookEventType eventType, object payload, CancellationToken cancellationToken = default);
}
