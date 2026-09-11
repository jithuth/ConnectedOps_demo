using ConnectedOps.Application.Organization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ConnectedOps.Web.Pages.Organization.Teams;

public sealed class IndexModel : PageModel
{
    private readonly ITeamService _teamService;
    private readonly IDepartmentService _departmentService;
    private readonly IEmployeeService _employeeService;

    public IndexModel(
        ITeamService teamService,
        IDepartmentService departmentService,
        IEmployeeService employeeService)
    {
        _teamService = teamService;
        _departmentService = departmentService;
        _employeeService = employeeService;
    }

    public IReadOnlyCollection<TeamListItemDto> Teams { get; private set; } = [];
    public IReadOnlyCollection<DepartmentListItemDto> Departments { get; private set; } = [];
    public IReadOnlyCollection<EmployeeListItemDto> Employees { get; private set; } = [];

    [BindProperty(SupportsGet = true)]
    public Guid? DepartmentId { get; set; }

    [BindProperty]
    public CreateTeamRequest CreateInput { get; set; } = null!;

    [BindProperty]
    public UpdateTeamRequest EditInput { get; set; } = null!;

    public async Task OnGetAsync()
    {
        Departments = await _departmentService.GetDepartmentsAsync(null, HttpContext.RequestAborted);
        Employees = await _employeeService.GetEmployeesAsync(null, null, null, HttpContext.RequestAborted);
        Teams = await _teamService.GetTeamsAsync(DepartmentId, HttpContext.RequestAborted);
    }

    public async Task<IActionResult> OnPostCreateAsync()
    {
        try
        {
            var team = await _teamService.CreateTeamAsync(CreateInput, HttpContext.RequestAborted);
            TempData["Feedback"] = $"Team '{team.Name}' ({team.Code}) was created successfully.";
            return RedirectToPage("/Organization/Teams/Index", new { DepartmentId });
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToPage("/Organization/Teams/Index", new { DepartmentId });
        }
    }

    public async Task<IActionResult> OnPostEditAsync(Guid id)
    {
        try
        {
            var team = await _teamService.UpdateTeamAsync(id, EditInput, HttpContext.RequestAborted);
            TempData["Feedback"] = $"Team '{team.Name}' ({team.Code}) was updated successfully.";
            return RedirectToPage("/Organization/Teams/Index", new { DepartmentId });
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToPage("/Organization/Teams/Index", new { DepartmentId });
        }
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        try
        {
            await _teamService.DeleteTeamAsync(id, HttpContext.RequestAborted);
            TempData["Feedback"] = "Team was deleted successfully.";
            return RedirectToPage("/Organization/Teams/Index", new { DepartmentId });
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToPage("/Organization/Teams/Index", new { DepartmentId });
        }
    }
}
