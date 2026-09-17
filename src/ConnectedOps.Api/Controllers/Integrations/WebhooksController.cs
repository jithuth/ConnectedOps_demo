using ConnectedOps.Application.Integrations;
using ConnectedOps.Domain.Authorization;
using ConnectedOps.Infrastructure.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConnectedOps.Api.Controllers.Integrations;

[ApiController]
[Route("api/integrations/webhooks")]
[Authorize]
public sealed class WebhooksController : ControllerBase
{
    private readonly IWebhookService _webhookService;

    public WebhooksController(IWebhookService webhookService)
    {
        _webhookService = webhookService;
    }

    [HttpGet]
    [RequirePermission(PermissionKeys.Integrations.View)]
    public async Task<IActionResult> GetSubscriptions(CancellationToken cancellationToken)
    {
        var result = await _webhookService.GetSubscriptionsAsync(cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [RequirePermission(PermissionKeys.Integrations.ManageWebhooks)]
    public async Task<IActionResult> CreateSubscription([FromBody] CreateWebhookRequest request, CancellationToken cancellationToken)
    {
        var result = await _webhookService.CreateSubscriptionAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpPut("{id:guid}")]
    [RequirePermission(PermissionKeys.Integrations.ManageWebhooks)]
    public async Task<IActionResult> UpdateSubscription(Guid id, [FromBody] UpdateWebhookRequest request, CancellationToken cancellationToken)
    {
        var result = await _webhookService.UpdateSubscriptionAsync(id, request, cancellationToken);
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    [RequirePermission(PermissionKeys.Integrations.ManageWebhooks)]
    public async Task<IActionResult> DeleteSubscription(Guid id, CancellationToken cancellationToken)
    {
        var result = await _webhookService.DeleteSubscriptionAsync(id, cancellationToken);
        if (!result) return NotFound();
        return NoContent();
    }

    [HttpPost("{id:guid}/toggle")]
    [RequirePermission(PermissionKeys.Integrations.ManageWebhooks)]
    public async Task<IActionResult> ToggleSubscription(Guid id, [FromQuery] bool isEnabled, CancellationToken cancellationToken)
    {
        var result = await _webhookService.ToggleSubscriptionAsync(id, isEnabled, cancellationToken);
        return Ok(new { isEnabled = result });
    }

    [HttpPost("{id:guid}/ping")]
    [RequirePermission(PermissionKeys.Integrations.ManageWebhooks)]
    public async Task<IActionResult> PingSubscription(Guid id, CancellationToken cancellationToken)
    {
        var result = await _webhookService.TriggerTestPingAsync(id, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}/attempts")]
    [RequirePermission(PermissionKeys.Integrations.View)]
    public async Task<IActionResult> GetDeliveryAttempts(Guid id, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var result = await _webhookService.GetDeliveryAttemptsPagedAsync(id, page, pageSize, cancellationToken);
        return Ok(result);
    }
}
