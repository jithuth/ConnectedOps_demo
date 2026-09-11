using ConnectedOps.Application.Organization;
using ConnectedOps.Application.Vehicles;
using ConnectedOps.Domain.Vehicles;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ConnectedOps.Web.Pages.Vehicles;

public sealed class EditModel : PageModel
{
    private readonly IVehicleService _vehicleService;
    private readonly IVehicleCategoryService _categoryService;
    private readonly IVehicleMakeModelService _makeModelService;
    private readonly IBranchService _branchService;
    private readonly ILocationService _locationService;

    public EditModel(
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

    public Guid VehicleId { get; private set; }
    public VehicleDetailDto Vehicle { get; private set; } = null!;
    public IReadOnlyCollection<VehicleCategoryDto> Categories { get; private set; } = [];
    public IReadOnlyCollection<VehicleMakeDto> Makes { get; private set; } = [];
    public IReadOnlyCollection<VehicleModelDto> AllModels { get; private set; } = [];
    public IReadOnlyCollection<BranchListItemDto> Branches { get; private set; } = [];
    public IReadOnlyCollection<LocationListItemDto> Locations { get; private set; } = [];

    [BindProperty]
    public UpdateVehicleRequest Input { get; set; } = null!;

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        VehicleId = id;
        try
        {
            var ct = HttpContext.RequestAborted;
            Vehicle = await _vehicleService.GetVehicleByIdAsync(id, ct);
            await LoadDropdownsAsync();

            Input = new UpdateVehicleRequest(
                Vehicle.VehicleNumber,
                Vehicle.VehicleCategoryId,
                Vehicle.VehicleMakeId,
                Vehicle.VehicleModelId,
                Vehicle.DisplayName,
                Vehicle.InternalCode,
                Vehicle.RegistrationNumber,
                Vehicle.VIN,
                Vehicle.ChassisNumber,
                Vehicle.EngineNumber,
                Vehicle.ModelYear,
                Vehicle.ManufactureYear,
                Vehicle.FuelType,
                Vehicle.TransmissionType,
                Vehicle.OwnershipType,
                Vehicle.BranchId,
                Vehicle.LocationId,
                Vehicle.Color,
                Vehicle.NumberOfSeats,
                Vehicle.GrossVehicleWeight,
                Vehicle.PayloadCapacity,
                Vehicle.PurchaseDate,
                Vehicle.PurchasePrice,
                Vehicle.CurrencyCode,
                Vehicle.OwnerName,
                Vehicle.LeaseCompany,
                Vehicle.LeaseStartDate,
                Vehicle.LeaseEndDate,
                Vehicle.MonthlyLeaseCost,
                Vehicle.Notes);

            return Page();
        }
        catch (KeyNotFoundException)
        {
            TempData["Error"] = $"Vehicle '{id}' was not found.";
            return RedirectToPage("/Vehicles/Index");
        }
    }

    public async Task<IActionResult> OnPostAsync(Guid id)
    {
        VehicleId = id;
        if (!ModelState.IsValid)
        {
            await LoadDropdownsAsync();
            return Page();
        }

        try
        {
            await _vehicleService.UpdateVehicleAsync(id, Input, HttpContext.RequestAborted);
            TempData["Feedback"] = "Vehicle profile updated successfully.";
            return RedirectToPage("/Vehicles/Details", new { id });
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
            await LoadDropdownsAsync();
            Vehicle = await _vehicleService.GetVehicleByIdAsync(id, HttpContext.RequestAborted);
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
