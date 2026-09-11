using ConnectedOps.Application.Organization;
using ConnectedOps.Application.Vehicles;
using ConnectedOps.Domain.Vehicles;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ConnectedOps.Web.Pages.Vehicles;

public sealed class CreateModel : PageModel
{
    private readonly IVehicleService _vehicleService;
    private readonly IVehicleCategoryService _categoryService;
    private readonly IVehicleMakeModelService _makeModelService;
    private readonly IBranchService _branchService;
    private readonly ILocationService _locationService;

    public CreateModel(
        IVehicleService vehicleService,
        IVehicleCategoryService categoryService,
        IVehicleMakeModelService makeModelService,
        IBranchService branchService,
        ILocationService locationService)
    {
        _vehicleService = vehicleService;
        _categoryService = categoryService;
        _makeModelService = makeModelService;
        _branchService = branchService;
        _locationService = locationService;
    }

    public IReadOnlyCollection<VehicleCategoryDto> Categories { get; private set; } = [];
    public IReadOnlyCollection<VehicleMakeDto> Makes { get; private set; } = [];
    public IReadOnlyCollection<VehicleModelDto> AllModels { get; private set; } = [];
    public IReadOnlyCollection<BranchListItemDto> Branches { get; private set; } = [];
    public IReadOnlyCollection<LocationListItemDto> Locations { get; private set; } = [];

    [BindProperty]
    public CreateVehicleRequest Input { get; set; } = new(
        string.Empty,
        Guid.Empty,
        Guid.Empty,
        Guid.Empty,
        null, null, null, null, null, null,
        DateTime.UtcNow.Year,
        DateTime.UtcNow.Year,
        FuelType.Diesel,
        TransmissionType.Automatic,
        OwnershipType.CompanyOwned,
        null, null,
        0m,
        OdometerUnit.Kilometers,
        VehicleStatus.Active,
        null, null, null, null,
        DateOnly.FromDateTime(DateTime.UtcNow),
        null, "USD",
        DateOnly.FromDateTime(DateTime.UtcNow),
        null, null, null, null, null, null);

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
            var result = await _vehicleService.CreateVehicleAsync(Input, HttpContext.RequestAborted);
            TempData["Feedback"] = $"Vehicle '{result.VehicleNumber}' was registered successfully.";
            return RedirectToPage("/Vehicles/Details", new { id = result.Id });
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
        Categories = await _categoryService.GetCategoriesAsync(false, ct);
        Makes = await _makeModelService.GetMakesAsync(false, ct);
        AllModels = await _makeModelService.GetAllModelsAsync(false, ct);
        Branches = await _branchService.GetBranchesAsync(ct);
        Locations = await _locationService.GetLocationsAsync(null, ct);
    }
}
