using ConnectedOps.Application.Common.Exceptions;
using ConnectedOps.Application.Drivers;
using ConnectedOps.Application.Fuel;
using ConnectedOps.Application.Vehicles;
using ConnectedOps.Domain.Fuel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ConnectedOps.Web.Pages.Fuel.Cards;

[Authorize]
public class IndexModel : PageModel
{
    private readonly IFuelCardService _cardService;
    private readonly IVehicleService _vehicleService;
    private readonly IDriverService _driverService;

    public IndexModel(
        IFuelCardService cardService,
        IVehicleService vehicleService,
        IDriverService driverService)
    {
        _cardService = cardService;
        _vehicleService = vehicleService;
        _driverService = driverService;
    }

    [BindProperty(SupportsGet = true)]
    public FuelCardQueryParameters Query { get; set; } = new();

    public PagedResult<FuelCardListItemDto> Result { get; private set; } = null!;
    public IReadOnlyCollection<VehicleListItemDto> Vehicles { get; private set; } = [];
    public IReadOnlyCollection<DriverListItemDto> Drivers { get; private set; } = [];

    [TempData]
    public string? StatusMessage { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public async Task OnGetAsync()
    {
        Result = await _cardService.GetCardsPagedAsync(Query);
        var vResult = await _vehicleService.GetVehiclesPagedAsync(new VehicleQueryParameters { PageSize = 200 });
        Vehicles = vResult.Items;
        var dResult = await _driverService.GetDriversPagedAsync(new DriverQueryParameters { PageSize = 200 });
        Drivers = dResult.Items;
    }

    public async Task<IActionResult> OnPostCreateAsync([FromForm] CreateFuelCardRequest request)
    {
        try
        {
            await _cardService.CreateAsync(request);
            StatusMessage = $"Fuel card '{request.CardReference}' added.";
            return RedirectToPage();
        }
        catch (Exception ex) when (ex is ValidationException or ConflictException)
        {
            ErrorMessage = ex.Message;
            return RedirectToPage();
        }
    }

    public async Task<IActionResult> OnPostSetStatusAsync(Guid id, [FromForm] FuelCardStatus status)
    {
        try
        {
            await _cardService.SetStatusAsync(id, status);
            StatusMessage = $"Fuel card status updated to {status}.";
            return RedirectToPage();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            return RedirectToPage();
        }
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        try
        {
            await _cardService.DeleteAsync(id);
            StatusMessage = "Fuel card deleted.";
            return RedirectToPage();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            return RedirectToPage();
        }
    }
}
