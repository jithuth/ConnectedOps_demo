using ConnectedOps.Application.Organization;
using ConnectedOps.Domain.Organization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ConnectedOps.Web.Pages.Organization.Employees;

public sealed class IndexModel : PageModel
{
    private readonly IEmployeeService _employeeService;
    private readonly IBranchService _branchService;
    private readonly IDepartmentService _departmentService;
    private readonly ITeamService _teamService;
    private readonly ITenantUserAdministrationService _userService;

    public IndexModel(
        IEmployeeService employeeService,
        IBranchService branchService,
        IDepartmentService departmentService,
        ITeamService teamService,
        ITenantUserAdministrationService userService)
    {
        _employeeService = employeeService;
        _branchService = branchService;
        _departmentService = departmentService;
        _teamService = teamService;
        _userService = userService;
    }

    public IReadOnlyCollection<EmployeeListItemDto> Employees { get; private set; } = [];
    public IReadOnlyCollection<BranchListItemDto> Branches { get; private set; } = [];
    public IReadOnlyCollection<DepartmentListItemDto> Departments { get; private set; } = [];
    public IReadOnlyCollection<TeamListItemDto> Teams { get; private set; } = [];
    public IReadOnlyCollection<TenantUserAdminDto> TenantUsers { get; private set; } = [];

    [BindProperty(SupportsGet = true)]
    public Guid? BranchId { get; set; }

    [BindProperty(SupportsGet = true)]
    public Guid? DepartmentId { get; set; }

    [BindProperty(SupportsGet = true)]
    public Guid? TeamId { get; set; }

    [BindProperty]
    public CreateEmployeeRequest CreateInput { get; set; } = null!;

    [BindProperty]
    public UpdateEmployeeRequest EditInput { get; set; } = null!;

    [BindProperty]
    public Guid SelectedUserId { get; set; }

    public async Task OnGetAsync()
    {
        Branches = await _branchService.GetBranchesAsync(HttpContext.RequestAborted);
        Departments = await _departmentService.GetDepartmentsAsync(null, HttpContext.RequestAborted);
        Teams = await _teamService.GetTeamsAsync(null, HttpContext.RequestAborted);
        TenantUsers = await _userService.GetUsersAsync(HttpContext.RequestAborted);
        Employees = await _employeeService.GetEmployeesAsync(BranchId, DepartmentId, TeamId, HttpContext.RequestAborted);
    }

    public async Task<IActionResult> OnPostCreateAsync()
    {
        try
        {
            var emp = await _employeeService.CreateEmployeeAsync(CreateInput, HttpContext.RequestAborted);
            TempData["Feedback"] = $"Employee '{emp.FullName}' ({emp.EmployeeNumber}) was enrolled successfully.";
            return RedirectToPage("/Organization/Employees/Index", new { BranchId, DepartmentId, TeamId });
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToPage("/Organization/Employees/Index", new { BranchId, DepartmentId, TeamId });
        }
    }

    public async Task<IActionResult> OnPostEditAsync(Guid id)
    {
        try
        {
            var emp = await _employeeService.UpdateEmployeeAsync(id, EditInput, HttpContext.RequestAborted);
            TempData["Feedback"] = $"Employee '{emp.FullName}' was updated successfully.";
            return RedirectToPage("/Organization/Employees/Index", new { BranchId, DepartmentId, TeamId });
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToPage("/Organization/Employees/Index", new { BranchId, DepartmentId, TeamId });
        }
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        try
        {
            await _employeeService.DeleteEmployeeAsync(id, HttpContext.RequestAborted);
            TempData["Feedback"] = "Employee was removed successfully.";
            return RedirectToPage("/Organization/Employees/Index", new { BranchId, DepartmentId, TeamId });
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToPage("/Organization/Employees/Index", new { BranchId, DepartmentId, TeamId });
        }
    }

    public async Task<IActionResult> OnPostLinkUserAsync(Guid id)
    {
        try
        {
            await _employeeService.LinkUserAsync(id, SelectedUserId, HttpContext.RequestAborted);
            TempData["Feedback"] = "System user was linked to employee successfully.";
            return RedirectToPage("/Organization/Employees/Index", new { BranchId, DepartmentId, TeamId });
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToPage("/Organization/Employees/Index", new { BranchId, DepartmentId, TeamId });
        }
    }

    public async Task<IActionResult> OnPostUnlinkUserAsync(Guid id)
    {
        try
        {
            await _employeeService.UnlinkUserAsync(id, HttpContext.RequestAborted);
            TempData["Feedback"] = "User link was removed from employee.";
            return RedirectToPage("/Organization/Employees/Index", new { BranchId, DepartmentId, TeamId });
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToPage("/Organization/Employees/Index", new { BranchId, DepartmentId, TeamId });
        }
    }
}
