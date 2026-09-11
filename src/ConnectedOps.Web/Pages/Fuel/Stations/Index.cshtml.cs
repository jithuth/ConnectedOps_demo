using ConnectedOps.Application.Common.Exceptions;
using ConnectedOps.Application.Fuel;
using ConnectedOps.Application.Organization;
using ConnectedOps.Application.Vehicles;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ConnectedOps.Web.Pages.Fuel.Stations;

[Authorize]
public class IndexModel : PageModel
{
    private readonly IFuelStationService _stationService;
    private readonly IBranchService _branchService;

    public IndexModel(
        IFuelStationService stationService,
        IBranchService branchService)
    {
        _stationService = stationService;
        _branchService = branchService;
    }

    [BindProperty(SupportsGet = true)]
    public FuelStationQueryParameters Query { get; set; } = new();

    public PagedResult<FuelStationListItemDto> Result { get; private set; } = null!;
    public IReadOnlyCollection<BranchListItemDto> Branches { get; private set; } = [];

    [TempData]
    public string? StatusMessage { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public async Task OnGetAsync()
    {
        Result = await _stationService.GetStationsPagedAsync(Query);
        Branches = await _branchService.GetBranchesAsync();
    }

    public async Task<IActionResult> OnPostCreateAsync([FromForm] CreateFuelStationRequest request)
    {
        try
        {
            await _stationService.CreateAsync(request);
            StatusMessage = $"Fuel station '{request.Name}' created successfully.";
            return RedirectToPage();
        }
        catch (Exception ex) when (ex is ValidationException or ConflictException)
        {
            ErrorMessage = ex.Message;
            return RedirectToPage();
        }
    }

    public async Task<IActionResult> OnPostUpdateAsync(Guid id, [FromForm] UpdateFuelStationRequest request)
    {
        try
        {
            await _stationService.UpdateAsync(id, request);
            StatusMessage = $"Fuel station '{request.Name}' updated.";
            return RedirectToPage();
        }
        catch (Exception ex) when (ex is ValidationException or ConflictException)
        {
            ErrorMessage = ex.Message;
            return RedirectToPage();
        }
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        try
        {
            await _stationService.DeleteAsync(id);
            StatusMessage = "Fuel station deleted.";
            return RedirectToPage();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            return RedirectToPage();
        }
    }
}
