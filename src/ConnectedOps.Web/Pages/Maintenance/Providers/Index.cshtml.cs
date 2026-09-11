using ConnectedOps.Application.Maintenance;
using ConnectedOps.Application.Organization;
using ConnectedOps.Domain.Maintenance;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ConnectedOps.Web.Pages.Maintenance.Providers;

[Authorize]
public class IndexModel : PageModel
{
    private readonly IMaintenanceProviderService _providerService;
    private readonly IBranchService _branchService;

    public IndexModel(
        IMaintenanceProviderService providerService,
        IBranchService branchService)
    {
        _providerService = providerService;
        _branchService = branchService;
    }

    public IReadOnlyCollection<MaintenanceProviderDto> Providers { get; private set; } = [];
    public IReadOnlyCollection<BranchListItemDto> Branches { get; private set; } = [];

    [TempData]
    public string? SuccessMessage { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public async Task OnGetAsync()
    {
        Providers = await _providerService.GetAllAsync(cancellationToken: HttpContext.RequestAborted);
        Branches = await _branchService.GetBranchesAsync(HttpContext.RequestAborted);
    }

    public async Task<IActionResult> OnPostCreateAsync(
        string code,
        string name,
        MaintenanceProviderType providerType,
        string? contactPerson,
        string? phone,
        string? email,
        string? address,
        Guid? branchId,
        string? notes,
        bool isActive)
    {
        try
        {
            var req = new CreateMaintenanceProviderRequest(code, name, providerType, contactPerson, phone, email, address, branchId, notes, isActive);
            await _providerService.CreateAsync(req, HttpContext.RequestAborted);
            SuccessMessage = $"Provider '{name}' created successfully.";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostUpdateAsync(
        Guid id,
        string code,
        string name,
        MaintenanceProviderType providerType,
        string? contactPerson,
        string? phone,
        string? email,
        string? address,
        Guid? branchId,
        string? notes,
        bool isActive)
    {
        try
        {
            var req = new UpdateMaintenanceProviderRequest(code, name, providerType, contactPerson, phone, email, address, branchId, notes, isActive);
            await _providerService.UpdateAsync(id, req, HttpContext.RequestAborted);
            SuccessMessage = $"Provider '{name}' updated successfully.";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        try
        {
            await _providerService.DeleteAsync(id, HttpContext.RequestAborted);
            SuccessMessage = "Provider deleted successfully.";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }

        return RedirectToPage();
    }
}
