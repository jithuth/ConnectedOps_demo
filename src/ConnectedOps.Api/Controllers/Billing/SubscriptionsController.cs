using ConnectedOps.Application.Billing;
using ConnectedOps.Domain.Authorization;
using ConnectedOps.Infrastructure.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConnectedOps.Api.Controllers.Billing;

[ApiController]
[Route("api/billing/subscriptions")]
[Authorize]
public sealed class SubscriptionsController : ControllerBase
{
    private readonly ITenantSubscriptionService _subscriptionService;
    private readonly ISubscriptionPlanService _planService;

    public SubscriptionsController(
        ITenantSubscriptionService subscriptionService,
        ISubscriptionPlanService planService)
    {
        _subscriptionService = subscriptionService;
        _planService = planService;
    }

    [HttpGet("current")]
    [RequirePermission(PermissionKeys.Subscriptions.View)]
    public async Task<ActionResult<TenantSubscriptionDto>> GetCurrent(
        CancellationToken cancellationToken)
    {
        var result = await _subscriptionService.GetCurrentSubscriptionAsync(cancellationToken);
        if (result is null)
            return NotFound();

        return Ok(result);
    }

    [HttpGet("available-plans")]
    [RequirePermission(PermissionKeys.Subscriptions.View)]
    public async Task<ActionResult<IReadOnlyList<SubscriptionPlanDto>>> GetAvailablePlans(
        CancellationToken cancellationToken)
    {
        var result = await _planService.GetPublicPlansAsync(cancellationToken);
        return Ok(result);
    }

    [HttpPost("change-plan")]
    [RequirePermission(PermissionKeys.Subscriptions.Manage)]
    public async Task<ActionResult<TenantSubscriptionDto>> ChangePlan(
        [FromBody] ChangeSubscriptionPlanRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _subscriptionService.ChangePlanAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpPost("cancel")]
    [RequirePermission(PermissionKeys.Subscriptions.Manage)]
    public async Task<ActionResult<TenantSubscriptionDto>> Cancel(
        [FromBody] CancelSubscriptionRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _subscriptionService.CancelSubscriptionAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpPost("reactivate")]
    [RequirePermission(PermissionKeys.Subscriptions.Manage)]
    public async Task<ActionResult<TenantSubscriptionDto>> Reactivate(
        CancellationToken cancellationToken)
    {
        var result = await _subscriptionService.ReactivateSubscriptionAsync(cancellationToken);
        return Ok(result);
    }

    [HttpPatch("quotas")]
    [RequirePermission(PermissionKeys.Subscriptions.Manage)]
    public async Task<ActionResult<TenantSubscriptionDto>> UpdateQuotas(
        [FromBody] SetCustomQuotasRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _subscriptionService.SetCustomQuotasAsync(request, cancellationToken);
        return Ok(result);
    }
}
