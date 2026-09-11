using ConnectedOps.Application.Billing;
using ConnectedOps.Domain.Billing;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ConnectedOps.Web.Pages.Platform.Plans;

public sealed class IndexModel : PageModel
{
    private readonly ISubscriptionPlanService _planService;

    public IndexModel(ISubscriptionPlanService planService)
    {
        _planService = planService;
    }

    public IReadOnlyCollection<SubscriptionPlanDto> Plans { get; private set; } = [];
    public string? ErrorMessage { get; private set; }

    public async Task OnGetAsync()
    {
        try
        {
            Plans = await _planService.GetAllPlansAsync(HttpContext.RequestAborted);
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    public async Task<IActionResult> OnPostCreateAsync(
        string name,
        string code,
        decimal price,
        BillingInterval billingInterval,
        string currency,
        string? description,
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
        try
        {
            var request = new CreateSubscriptionPlanRequest
            {
                Name = name,
                Code = code,
                Price = price,
                BillingInterval = billingInterval,
                Currency = string.IsNullOrWhiteSpace(currency) ? "USD" : currency.Trim().ToUpperInvariant(),
                Description = description,
                MaxVehicles = maxVehicles,
                MaxAssets = maxAssets,
                MaxUsers = maxUsers,
                MaxStorageGb = maxStorageGb,
                HasAdvancedAnalytics = hasAdvancedAnalytics,
                HasApiAccess = hasApiAccess,
                HasCustomBranding = hasCustomBranding,
                HasAuditExport = hasAuditExport,
                IsPublic = isPublic,
                SortOrder = sortOrder
            };

            await _planService.CreatePlanAsync(request, HttpContext.RequestAborted);
            TempData["SuccessMessage"] = $"Subscription plan '{name}' created successfully.";
            return RedirectToPage();
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToPage();
        }
    }

    public async Task<IActionResult> OnPostToggleActiveAsync(Guid planId, bool isActive)
    {
        try
        {
            if (isActive)
            {
                await _planService.DeactivatePlanAsync(planId, HttpContext.RequestAborted);
                TempData["SuccessMessage"] = "Plan deactivated.";
            }
            else
            {
                await _planService.ActivatePlanAsync(planId, HttpContext.RequestAborted);
                TempData["SuccessMessage"] = "Plan activated.";
            }

            return RedirectToPage();
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToPage();
        }
    }
}
