using ConnectedOps.Application.Common.Exceptions;
using ConnectedOps.Application.Drivers;
using ConnectedOps.Application.Fuel;
using ConnectedOps.Application.Vehicles;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ConnectedOps.Web.Pages.Fuel.Transactions;

[Authorize]
public class EditModel : PageModel
{
    private readonly IFuelTransactionService _transactionService;
    private readonly IVehicleService _vehicleService;
    private readonly IDriverService _driverService;
    private readonly IFuelStationService _stationService;
    private readonly IFuelCardService _cardService;

    public EditModel(
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
    public UpdateFuelTransactionRequest Input { get; set; } = null!;

    [BindProperty]
    public Guid Id { get; set; }

    public IReadOnlyCollection<VehicleListItemDto> Vehicles { get; private set; } = [];
    public IReadOnlyCollection<DriverListItemDto> Drivers { get; private set; } = [];
    public IReadOnlyCollection<FuelStationDto> Stations { get; private set; } = [];
    public IReadOnlyCollection<FuelCardDto> Cards { get; private set; } = [];

    [TempData]
    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        Id = id;
        try
        {
            var tx = await _transactionService.GetByIdAsync(id);
            Input = new UpdateFuelTransactionRequest(
                tx.VehicleId,
                tx.TransactionDateUtc,
                tx.Quantity,
                tx.UnitPrice,
                tx.TotalCost,
                tx.FuelType,
                tx.QuantityUnit,
                tx.CurrencyCode,
                tx.IsFullTank,
                tx.IsPartialFill,
                tx.OdometerReading,
                tx.OdometerUnit,
                tx.EngineHours,
                tx.DriverId,
                tx.FuelStationId,
                tx.FuelCardId,
                tx.TransactionReference,
                tx.ReceiptNumber,
                tx.PaymentMethod,
                tx.Latitude,
                tx.Longitude,
                tx.Notes);

            await LoadDropdownsAsync();
            return Page();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
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
            await _transactionService.UpdateAsync(Id, Input);
            return RedirectToPage("/Fuel/Transactions/Details", new { id = Id });
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
