using ConnectedOps.Application.Organization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ConnectedOps.Web.Pages.Organization.Departments;

public sealed class IndexModel : PageModel
{
    private readonly IDepartmentService _departmentService;
    private readonly IBranchService _branchService;

    public IndexModel(
        IDepartmentService departmentService,
        IBranchService branchService)
    {
        _departmentService = departmentService;
        _branchService = branchService;
    }

    public IReadOnlyCollection<DepartmentListItemDto> Departments { get; private set; } = [];
    public IReadOnlyCollection<BranchListItemDto> Branches { get; private set; } = [];

    [BindProperty(SupportsGet = true)]
    public Guid? BranchId { get; set; }

    [BindProperty]
    public CreateDepartmentRequest CreateInput { get; set; } = null!;

    [BindProperty]
    public UpdateDepartmentRequest EditInput { get; set; } = null!;

    public async Task OnGetAsync()
    {
        Branches = await _branchService.GetBranchesAsync(HttpContext.RequestAborted);
        Departments = await _departmentService.GetDepartmentsAsync(BranchId, HttpContext.RequestAborted);
    }

    public async Task<IActionResult> OnPostCreateAsync()
    {
        try
        {
            var dept = await _departmentService.CreateDepartmentAsync(CreateInput, HttpContext.RequestAborted);
            TempData["Feedback"] = $"Department '{dept.Name}' ({dept.Code}) was created successfully.";
            return RedirectToPage("/Organization/Departments/Index", new { BranchId });
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToPage("/Organization/Departments/Index", new { BranchId });
        }
    }

    public async Task<IActionResult> OnPostEditAsync(Guid id)
    {
        try
        {
            var dept = await _departmentService.UpdateDepartmentAsync(id, EditInput, HttpContext.RequestAborted);
            TempData["Feedback"] = $"Department '{dept.Name}' ({dept.Code}) was updated successfully.";
            return RedirectToPage("/Organization/Departments/Index", new { BranchId });
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToPage("/Organization/Departments/Index", new { BranchId });
        }
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        try
        {
            await _departmentService.DeleteDepartmentAsync(id, HttpContext.RequestAborted);
            TempData["Feedback"] = "Department was deleted successfully.";
            return RedirectToPage("/Organization/Departments/Index", new { BranchId });
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToPage("/Organization/Departments/Index", new { BranchId });
        }
    }
}
