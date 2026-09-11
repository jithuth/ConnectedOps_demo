using ConnectedOps.Application.Billing;
using ConnectedOps.Domain.Billing;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ConnectedOps.Web.Pages.Billing.Subscription;

public sealed class IndexModel : PageModel
{
    private readonly ITenantSubscriptionService _subscriptionService;
    private readonly ISubscriptionPlanService _planService;

    public IndexModel(
        ITenantSubscriptionService subscriptionService,
        ISubscriptionPlanService planService)
    {
        _subscriptionService = subscriptionService;
        _planService = planService;
    }

    public TenantSubscriptionDto? CurrentSubscription { get; private set; }
    public IReadOnlyCollection<SubscriptionPlanDto> AvailablePlans { get; private set; } = [];
    public string? ErrorMessage { get; private set; }

    public async Task OnGetAsync()
    {
        try
        {
            CurrentSubscription = await _subscriptionService.GetCurrentSubscriptionAsync(HttpContext.RequestAborted);
            AvailablePlans = await _planService.GetPublicPlansAsync(HttpContext.RequestAborted);
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    public async Task<IActionResult> OnPostChangePlanAsync(Guid newPlanId)
    {
        try
        {
            await _subscriptionService.ChangePlanAsync(
                new ChangeSubscriptionPlanRequest { NewPlanId = newPlanId },
                HttpContext.RequestAborted);

            TempData["SuccessMessage"] = "Subscription plan updated successfully!";
            return RedirectToPage();
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToPage();
        }
    }

    public async Task<IActionResult> OnPostCancelAsync(string? reason)
    {
        try
        {
            await _subscriptionService.CancelSubscriptionAsync(
                new CancelSubscriptionRequest { Reason = reason },
                HttpContext.RequestAborted);

            TempData["SuccessMessage"] = "Subscription has been cancelled.";
            return RedirectToPage();
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToPage();
        }
    }

    public async Task<IActionResult> OnPostReactivateAsync()
    {
        try
        {
            await _subscriptionService.ReactivateSubscriptionAsync(HttpContext.RequestAborted);
            TempData["SuccessMessage"] = "Subscription reactivated successfully!";
            return RedirectToPage();
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToPage();
        }
    }
}
