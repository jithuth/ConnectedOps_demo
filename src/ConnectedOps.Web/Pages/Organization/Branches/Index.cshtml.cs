using ConnectedOps.Application.Organization;
using ConnectedOps.Domain.Organization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ConnectedOps.Web.Pages.Organization.Branches;

public sealed class IndexModel : PageModel
{
    private readonly IBranchService _branchService;

    public IndexModel(IBranchService branchService)
    {
        _branchService = branchService;
    }

    public IReadOnlyCollection<BranchListItemDto> Branches { get; private set; } = [];

    [BindProperty]
    public CreateBranchRequest CreateInput { get; set; } = null!;

    [BindProperty]
    public UpdateBranchRequest EditInput { get; set; } = null!;

    public async Task OnGetAsync()
    {
        Branches = await _branchService.GetBranchesAsync(HttpContext.RequestAborted);
    }

    public async Task<IActionResult> OnPostCreateAsync()
    {
        try
        {
            var branch = await _branchService.CreateBranchAsync(CreateInput, HttpContext.RequestAborted);
            TempData["Feedback"] = $"Branch '{branch.Name}' ({branch.Code}) was created successfully.";
            return RedirectToPage("/Organization/Branches/Index");
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToPage("/Organization/Branches/Index");
        }
    }

    public async Task<IActionResult> OnPostEditAsync(Guid id)
    {
        try
        {
            var branch = await _branchService.UpdateBranchAsync(id, EditInput, HttpContext.RequestAborted);
            TempData["Feedback"] = $"Branch '{branch.Name}' ({branch.Code}) was updated successfully.";
            return RedirectToPage("/Organization/Branches/Index");
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToPage("/Organization/Branches/Index");
        }
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        try
        {
            await _branchService.DeleteBranchAsync(id, HttpContext.RequestAborted);
            TempData["Feedback"] = "Branch was deleted successfully.";
            return RedirectToPage("/Organization/Branches/Index");
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToPage("/Organization/Branches/Index");
        }
    }
}
