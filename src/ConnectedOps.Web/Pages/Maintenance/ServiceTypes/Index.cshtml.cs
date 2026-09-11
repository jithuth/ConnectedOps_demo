using ConnectedOps.Application.Maintenance;
using ConnectedOps.Domain.Maintenance;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ConnectedOps.Web.Pages.Maintenance.ServiceTypes;

[Authorize]
public class IndexModel : PageModel
{
    private readonly IMaintenanceServiceTypeService _serviceTypeService;

    public IndexModel(IMaintenanceServiceTypeService serviceTypeService)
    {
        _serviceTypeService = serviceTypeService;
    }

    public IReadOnlyCollection<MaintenanceServiceTypeDto> ServiceTypes { get; private set; } = [];

    [TempData]
    public string? SuccessMessage { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public async Task OnGetAsync()
    {
        ServiceTypes = await _serviceTypeService.GetAllAsync(cancellationToken: HttpContext.RequestAborted);
    }

    public async Task<IActionResult> OnPostCreateAsync(
        string code,
        string name,
        MaintenanceServiceCategory category,
        string? description,
        decimal? defaultDurationHours,
        bool isActive)
    {
        try
        {
            var req = new CreateMaintenanceServiceTypeRequest(code, name, category, description, defaultDurationHours, isActive);
            await _serviceTypeService.CreateAsync(req, HttpContext.RequestAborted);
            SuccessMessage = $"Service type '{name}' created successfully.";
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
        MaintenanceServiceCategory category,
        string? description,
        decimal? defaultDurationHours,
        bool isActive)
    {
        try
        {
            var req = new UpdateMaintenanceServiceTypeRequest(code, name, category, description, defaultDurationHours, isActive);
            await _serviceTypeService.UpdateAsync(id, req, HttpContext.RequestAborted);
            SuccessMessage = $"Service type '{name}' updated successfully.";
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
            await _serviceTypeService.DeleteAsync(id, HttpContext.RequestAborted);
            SuccessMessage = "Service type deleted successfully.";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }

        return RedirectToPage();
    }
}
