using ConnectedOps.Application.Common.Exceptions;
using ConnectedOps.Application.Drivers;
using ConnectedOps.Application.Fuel;
using ConnectedOps.Application.Vehicles;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ConnectedOps.Web.Pages.Fuel.Transactions;

[Authorize]
public class CreateModel : PageModel
{
    private readonly IFuelTransactionService _transactionService;
    private readonly IVehicleService _vehicleService;
    private readonly IDriverService _driverService;
    private readonly IFuelStationService _stationService;
    private readonly IFuelCardService _cardService;

    public CreateModel(
        IFuelTransactionService transactionService,
        IVehicleService vehicleService,
        IDriverService driverService,
        IFuelStationService stationService,
        IFuelCardService cardService)
    {
        _transactionService = transactionService;
        _vehicleService = vehicleService;
        _driverService = driverService;
        _stationService = stationService;
        _cardService = cardService;
    }

    [BindProperty]
    public CreateFuelTransactionRequest Input { get; set; } = new(
        Guid.Empty,
        DateTime.UtcNow,
        0,
        0);

    public IReadOnlyCollection<VehicleListItemDto> Vehicles { get; private set; } = [];
    public IReadOnlyCollection<DriverListItemDto> Drivers { get; private set; } = [];
    public IReadOnlyCollection<FuelStationDto> Stations { get; private set; } = [];
    public IReadOnlyCollection<FuelCardDto> Cards { get; private set; } = [];

    [TempData]
    public string? ErrorMessage { get; set; }

    public async Task OnGetAsync([FromQuery] Guid? vehicleId = null)
    {
        await LoadDropdownsAsync();

        if (vehicleId.HasValue)
        {
            Input = Input with { VehicleId = vehicleId.Value };
        }
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (Input.VehicleId == Guid.Empty)
        {
            ModelState.AddModelError("Input.VehicleId", "Vehicle is required.");
        }
        if (Input.Quantity <= 0)
        {
            ModelState.AddModelError("Input.Quantity", "Quantity must be greater than 0.");
        }
        if (Input.UnitPrice < 0)
        {
            ModelState.AddModelError("Input.UnitPrice", "Unit price cannot be negative.");
        }

        if (!ModelState.IsValid)
        {
            await LoadDropdownsAsync();
            return Page();
        }

        try
        {
            var result = await _transactionService.CreateAsync(Input);
            return RedirectToPage("/Fuel/Transactions/Details", new { id = result.Id });
        }
        catch (Exception ex) when (ex is ValidationException or ConflictException or InvalidOperationException)
        {
            ErrorMessage = ex.Message;
            ModelState.AddModelError(string.Empty, ex.Message);
            await LoadDropdownsAsync();
            return Page();
        }
    }

    private async Task LoadDropdownsAsync()
    {
        var vResult = await _vehicleService.GetVehiclesPagedAsync(new VehicleQueryParameters { PageSize = 200 });
        Vehicles = vResult.Items;

        var dResult = await _driverService.GetDriversPagedAsync(new DriverQueryParameters { PageSize = 200 });
        Drivers = dResult.Items;

        Stations = await _stationService.GetAllAsync(activeOnly: true);
        Cards = await _cardService.GetAllAsync(activeOnly: true);
    }
}
