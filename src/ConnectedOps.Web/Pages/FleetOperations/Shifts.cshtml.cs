using ConnectedOps.Application.FleetOperations;
using ConnectedOps.Application.Organization;
using ConnectedOps.Domain.FleetOperations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ConnectedOps.Web.Pages.FleetOperations;

[Authorize]
public sealed class ShiftsModel : PageModel
{
    private readonly IFleetShiftService _shiftService;
    private readonly IBranchService _branchService;

    public ShiftsModel(
        IFleetShiftService shiftService,
        IBranchService branchService)
    {
        _shiftService = shiftService;
        _branchService = branchService;
    }

    public IReadOnlyCollection<FleetShiftDto> Shifts { get; private set; } = [];
    public IReadOnlyCollection<BranchListItemDto> Branches { get; private set; } = [];

    [BindProperty]
    public CreateFleetShiftRequest CreateInput { get; set; } = new(
        string.Empty,
        string.Empty,
        new TimeOnly(8, 0),
        new TimeOnly(17, 0),
        DayOfWeekFlags.Weekdays,
        null,
        null);

    [TempData]
    public string? StatusMessage { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public async Task OnGetAsync()
    {
        await LoadDataAsync();
    }

    public async Task<IActionResult> OnPostCreateShiftAsync()
    {
        try
        {
            var shift = await _shiftService.CreateShiftAsync(CreateInput, HttpContext.RequestAborted);
            StatusMessage = $"Shift '{shift.Name}' ({shift.Code}) created successfully.";
            return RedirectToPage();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            await LoadDataAsync();
            return Page();
        }
    }

    public async Task<IActionResult> OnPostActivateShiftAsync(Guid id)
    {
        try
        {
            await _shiftService.ActivateShiftAsync(id, HttpContext.RequestAborted);
            StatusMessage = "Shift activated.";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeactivateShiftAsync(Guid id)
    {
        try
        {
            await _shiftService.DeactivateShiftAsync(id, HttpContext.RequestAborted);
            StatusMessage = "Shift deactivated.";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteShiftAsync(Guid id)
    {
        try
        {
            await _shiftService.DeleteShiftAsync(id, HttpContext.RequestAborted);
            StatusMessage = "Shift deleted.";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }

        return RedirectToPage();
    }

    private async Task LoadDataAsync()
    {
        var ct = HttpContext.RequestAborted;
        Shifts = await _shiftService.GetShiftsAsync(null, null, ct);
        Branches = await _branchService.GetBranchesAsync(ct);
    }
}
