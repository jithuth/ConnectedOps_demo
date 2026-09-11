using ConnectedOps.Application.Drivers;
using ConnectedOps.Application.Organization;
using ConnectedOps.Domain.Drivers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ConnectedOps.Web.Pages.Drivers;

public sealed class CreateModel : PageModel
{
    private readonly IDriverService _driverService;
    private readonly IBranchService _branchService;
    private readonly IDepartmentService _departmentService;
    private readonly IEmployeeService _employeeService;

    public CreateModel(
        IDriverService driverService,
        IBranchService branchService,
        IDepartmentService departmentService,
        IEmployeeService employeeService)
    {
        _driverService = driverService;
        _branchService = branchService;
        _departmentService = departmentService;
        _employeeService = employeeService;
    }

    public IReadOnlyCollection<BranchListItemDto> Branches { get; private set; } = [];
    public IReadOnlyCollection<DepartmentListItemDto> Departments { get; private set; } = [];
    public IReadOnlyCollection<EmployeeListItemDto> Employees { get; private set; } = [];

    [BindProperty]
    public CreateDriverRequest Input { get; set; } = new(
        string.Empty,
        string.Empty,
        string.Empty,
        string.Empty,
        DriverType.Employee);

    public async Task OnGetAsync()
    {
        await LoadDropdownsAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadDropdownsAsync();
            return Page();
        }

        try
        {
            var result = await _driverService.CreateDriverAsync(Input, HttpContext.RequestAborted);
            TempData["Feedback"] = $"Driver '{result.DriverNumber} - {result.DisplayName}' was registered successfully.";
            return RedirectToPage("/Drivers/Details", new { id = result.Id });
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
            await LoadDropdownsAsync();
            return Page();
        }
    }

    private async Task LoadDropdownsAsync()
    {
        var ct = HttpContext.RequestAborted;
        Branches = await _branchService.GetBranchesAsync(ct);
        Departments = await _departmentService.GetDepartmentsAsync(cancellationToken: ct);
        Employees = await _employeeService.GetEmployeesAsync(cancellationToken: ct);
    }
}
