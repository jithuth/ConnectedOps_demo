using ConnectedOps.Application.Platform;
using ConnectedOps.Domain.Platform;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ConnectedOps.Web.Pages.Platform.Users;

public sealed class IndexModel : PageModel
{
    private readonly IPlatformUserService _userService;

    public IndexModel(IPlatformUserService userService)
    {
        _userService = userService;
    }

    public PlatformUserPage UsersPage { get; private set; } = null!;

    [BindProperty(SupportsGet = true)]
    public string? Search { get; set; }

    [BindProperty(SupportsGet = true)]
    public PlatformRole? Role { get; set; }

    [BindProperty(SupportsGet = true)]
    public bool? IsActive { get; set; }

    [BindProperty(SupportsGet = true)]
    public int PageIndex { get; set; } = 1;

    public async Task OnGetAsync()
    {
        var query = new PlatformUserQuery(
            Search: Search,
            Role: Role,
            IsActive: IsActive,
            Page: PageIndex,
            PageSize: 10);

        UsersPage = await _userService.GetUsersAsync(query, HttpContext.RequestAborted);
    }

    public async Task<IActionResult> OnPostCreateAsync(CreatePlatformUserRequest request)
    {
        try
        {
            var result = await _userService.CreateUserAsync(request, HttpContext.RequestAborted);
            TempData["Feedback"] = $"Platform user '{result.User.Email}' created successfully. Initial temporary password: {result.GeneratedPassword}";
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Failed to create platform user: {ex.Message}";
        }

        return RedirectToPage(new { Search, Role, IsActive, PageIndex });
    }

    public async Task<IActionResult> OnPostUpdateRoleAsync(Guid userId, PlatformRole role)
    {
        try
        {
            await _userService.UpdateUserRoleAsync(userId, role, HttpContext.RequestAborted);
            TempData["Feedback"] = "User platform role updated successfully.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Failed to update user role: {ex.Message}";
        }

        return RedirectToPage(new { Search, Role, IsActive, PageIndex });
    }

    public async Task<IActionResult> OnPostActivateAsync(Guid userId)
    {
        try
        {
            await _userService.ActivateUserAsync(userId, HttpContext.RequestAborted);
            TempData["Feedback"] = "User activated successfully.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Failed to activate user: {ex.Message}";
        }

        return RedirectToPage(new { Search, Role, IsActive, PageIndex });
    }

    public async Task<IActionResult> OnPostDeactivateAsync(Guid userId)
    {
        try
        {
            await _userService.DeactivateUserAsync(userId, HttpContext.RequestAborted);
            TempData["Feedback"] = "User deactivated successfully.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Failed to deactivate user: {ex.Message}";
        }

        return RedirectToPage(new { Search, Role, IsActive, PageIndex });
    }
}
