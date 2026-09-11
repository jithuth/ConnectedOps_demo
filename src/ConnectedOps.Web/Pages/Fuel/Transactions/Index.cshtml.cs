using ConnectedOps.Application.Fuel;
using ConnectedOps.Application.Vehicles;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ConnectedOps.Web.Pages.Fuel.Transactions;

[Authorize]
public class IndexModel : PageModel
{
    private readonly IFuelTransactionService _transactionService;
    private readonly IFuelStationService _stationService;
    private readonly IFuelCardService _cardService;
    private readonly IVehicleService _vehicleService;

    public IndexModel(
        IFuelTransactionService transactionService,
        IFuelStationService stationService,
        IFuelCardService cardService,
        IVehicleService vehicleService)
    {
        _transactionService = transactionService;
        _stationService = stationService;
        _cardService = cardService;
        _vehicleService = vehicleService;
    }

    [BindProperty(SupportsGet = true)]
    public FuelTransactionQueryParameters Query { get; set; } = new();

    public PagedResult<FuelTransactionListItemDto> Result { get; private set; } = null!;
    public IReadOnlyCollection<FuelStationDto> Stations { get; private set; } = [];
    public IReadOnlyCollection<FuelCardDto> Cards { get; private set; } = [];

    public async Task OnGetAsync()
    {
        Result = await _transactionService.GetTransactionsPagedAsync(Query);
        Stations = await _stationService.GetAllAsync(activeOnly: true);
        Cards = await _cardService.GetAllAsync(activeOnly: true);
    }
}
