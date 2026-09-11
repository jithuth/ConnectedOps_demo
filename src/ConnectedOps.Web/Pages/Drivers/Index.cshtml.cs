using ConnectedOps.Application.Drivers;
using ConnectedOps.Application.Organization;
using ConnectedOps.Application.Vehicles;
using ConnectedOps.Domain.Drivers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ConnectedOps.Web.Pages.Drivers;

public sealed class IndexModel : PageModel
{
    private readonly IDriverService _driverService;
    private readonly IBranchService _branchService;
    private readonly IDepartmentService _departmentService;

    public IndexModel(
        IDriverService driverService,
        IBranchService branchService,
        IDepartmentService departmentService)
    {
        _driverService = driverService;
        _branchService = branchService;
        _departmentService = departmentService;
    }

    public PagedResult<DriverListItemDto> Drivers { get; private set; } = null!;
    public IReadOnlyCollection<BranchListItemDto> Branches { get; private set; } = [];
    public IReadOnlyCollection<DepartmentListItemDto> Departments { get; private set; } = [];

    [BindProperty(SupportsGet = true)]
    public DriverQueryParameters Query { get; set; } = new();

    public async Task OnGetAsync()
    {
        var ct = HttpContext.RequestAborted;
        Drivers = await _driverService.GetDriversPagedAsync(Query, ct);
        Branches = await _branchService.GetBranchesAsync(ct);
        Departments = await _departmentService.GetDepartmentsAsync(cancellationToken: ct);
    }

    public async Task<IActionResult> OnPostChangeStatusAsync(Guid id, DriverStatus newStatus, string? notes)
    {
        try
        {
            await _driverService.ChangeStatusAsync(id, new ChangeDriverStatusRequest(newStatus, notes), HttpContext.RequestAborted);
            TempData["Feedback"] = "Driver status updated successfully.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToPage(new
        {
            Query.SearchTerm,
            Query.BranchId,
            Query.DepartmentId,
            Query.Status,
            Query.DriverType,
            Query.PageNumber,
            Query.PageSize
        });
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        try
        {
            await _driverService.DeleteDriverAsync(id, HttpContext.RequestAborted);
            TempData["Feedback"] = "Driver record was successfully deleted.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToPage(new
        {
            Query.SearchTerm,
            Query.BranchId,
            Query.DepartmentId,
            Query.Status,
            Query.DriverType,
            Query.PageNumber,
            Query.PageSize
        });
    }
}
