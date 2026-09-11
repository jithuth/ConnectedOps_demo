using ConnectedOps.Application.Organization;
using ConnectedOps.Application.TenantRoles;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ConnectedOps.Web.Pages.Organization.Users;

public sealed class IndexModel : PageModel
{
    private readonly ITenantUserAdministrationService _userAdminService;
    private readonly ITenantRoleManagementService _roleService;
    private readonly IBranchService _branchService;
    private readonly IDepartmentService _deptService;
    private readonly ITeamService _teamService;

    public IndexModel(
        ITenantUserAdministrationService userAdminService,
        ITenantRoleManagementService roleService,
        IBranchService branchService,
        IDepartmentService deptService,
        ITeamService teamService)
    {
        _userAdminService = userAdminService;
        _roleService = roleService;
        _branchService = branchService;
        _deptService = deptService;
        _teamService = teamService;
    }

    public IReadOnlyCollection<TenantUserAdminDto> Users { get; private set; } = [];
    public IReadOnlyCollection<TenantRoleListItem> Roles { get; private set; } = [];
    public IReadOnlyCollection<BranchListItemDto> Branches { get; private set; } = [];
    public IReadOnlyCollection<DepartmentListItemDto> Departments { get; private set; } = [];
    public IReadOnlyCollection<TeamListItemDto> Teams { get; private set; } = [];

    [BindProperty]
    public CreateTenantUserAdminRequest CreateInput { get; set; } = null!;

    [BindProperty]
    public List<Guid> SelectedRoleIds { get; set; } = [];

    public async Task OnGetAsync()
    {
        Users = await _userAdminService.GetUsersAsync(HttpContext.RequestAborted);
        Roles = await _roleService.GetRolesAsync(HttpContext.RequestAborted);
        Branches = await _branchService.GetBranchesAsync(HttpContext.RequestAborted);
        Departments = await _deptService.GetDepartmentsAsync(null, HttpContext.RequestAborted);
        Teams = await _teamService.GetTeamsAsync(null, HttpContext.RequestAborted);
    }

    public async Task<IActionResult> OnPostCreateAsync()
    {
        try
        {
            var req = new CreateTenantUserAdminRequest(
                CreateInput.Email,
                CreateInput.FirstName,
                CreateInput.LastName,
                CreateInput.PhoneNumber,
                CreateInput.Password,
                SelectedRoleIds,
                CreateInput.CreateLinkedEmployee,
                CreateInput.BranchId,
                CreateInput.DepartmentId,
                CreateInput.TeamId,
                CreateInput.JobTitle);

            var created = await _userAdminService.CreateUserAsync(req, HttpContext.RequestAborted);
            TempData["Feedback"] = $"Tenant user '{created.Email}' created successfully.";
            return RedirectToPage("/Organization/Users/Index");
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToPage("/Organization/Users/Index");
        }
    }

    public async Task<IActionResult> OnPostActivateAsync(Guid id)
    {
        try
        {
            await _userAdminService.ActivateUserAsync(id, HttpContext.RequestAborted);
            TempData["Feedback"] = "User activated successfully.";
            return RedirectToPage("/Organization/Users/Index");
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToPage("/Organization/Users/Index");
        }
    }

    public async Task<IActionResult> OnPostDeactivateAsync(Guid id)
    {
        try
        {
            await _userAdminService.DeactivateUserAsync(id, HttpContext.RequestAborted);
            TempData["Feedback"] = "User deactivated successfully.";
            return RedirectToPage("/Organization/Users/Index");
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToPage("/Organization/Users/Index");
        }
    }

    public async Task<IActionResult> OnPostRemoveAsync(Guid id)
    {
        try
        {
            await _userAdminService.RemoveUserAsync(id, HttpContext.RequestAborted);
            TempData["Feedback"] = "User removed from organization successfully.";
            return RedirectToPage("/Organization/Users/Index");
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToPage("/Organization/Users/Index");
        }
    }
}
