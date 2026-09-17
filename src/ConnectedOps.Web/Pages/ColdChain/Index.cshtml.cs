using ConnectedOps.Application.ColdChain;
using ConnectedOps.Application.Vehicles;
using ConnectedOps.Domain.ColdChain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ConnectedOps.Web.Pages.ColdChain;

[Authorize]
public class IndexModel : PageModel
{
    private readonly IColdChainService _coldChainService;
    private readonly IVehicleService _vehicleService;

    public IndexModel(
        IColdChainService coldChainService,
        IVehicleService vehicleService)
    {
        _coldChainService = coldChainService;
        _vehicleService = vehicleService;
    }

    public ColdChainDashboardDto Dashboard { get; private set; } = null!;
    public PagedResult<CargoSensorDeviceDto> Sensors { get; private set; } = null!;
    public PagedResult<ColdChainExcursionDto> Excursions { get; private set; } = null!;
    public IReadOnlyCollection<VehicleListItemDto> Vehicles { get; private set; } = [];

    [BindProperty(SupportsGet = true)]
    public ExcursionStatus? ExcursionStatusFilter { get; set; }

    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    [TempData]
    public string? SuccessMessage { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Dashboard = await _coldChainService.GetDashboardAsync(cancellationToken);

        Sensors = await _coldChainService.GetSensorsPagedAsync(
            new SensorFilterRequest(PageNumber: PageNumber, PageSize: 20),
            cancellationToken);

        Excursions = await _coldChainService.GetExcursionsPagedAsync(
            new ExcursionFilterRequest(Status: ExcursionStatusFilter, PageNumber: PageNumber, PageSize: 20),
            cancellationToken);

        var vehiclesPaged = await _vehicleService.GetVehiclesPagedAsync(
            new VehicleQueryParameters { PageNumber = 1, PageSize = 100 },
            cancellationToken);
        Vehicles = vehiclesPaged.Items;
    }

    public async Task<IActionResult> OnPostRegisterSensorAsync(
        string sensorTagNumber,
        string compartmentName,
        Guid vehicleId,
        double minTargetTemperatureCelsius,
        double maxTargetTemperatureCelsius,
        int batteryLevelPercent,
        string? macAddress,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _coldChainService.RegisterSensorAsync(
                new RegisterCargoSensorRequest(sensorTagNumber, compartmentName, vehicleId, minTargetTemperatureCelsius, maxTargetTemperatureCelsius, batteryLevelPercent, macAddress),
                cancellationToken);

            SuccessMessage = $"Sensor {result.SensorTagNumber} registered to {result.VehiclePlateNumber} ({result.CompartmentName}).";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostRecordTelemetryAsync(
        Guid cargoSensorDeviceId,
        DateTime recordedAtUtc,
        double temperatureCelsius,
        double? humidityPercent,
        bool doorOpen,
        ReeferMode reeferMode,
        double? setpointTemperatureCelsius,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _coldChainService.RecordTelemetryAsync(
                new RecordCargoTelemetryRequest(cargoSensorDeviceId, recordedAtUtc, temperatureCelsius, humidityPercent, doorOpen, reeferMode, setpointTemperatureCelsius),
                cancellationToken);

            SuccessMessage = $"Telemetry reading recorded: {result.TemperatureCelsius:F1}°C ({result.ReeferModeName}).";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostResolveExcursionAsync(
        Guid excursionId,
        string actionTaken,
        CancellationToken cancellationToken)
    {
        try
        {
            await _coldChainService.ResolveExcursionAsync(
                excursionId,
                new ResolveExcursionRequest(actionTaken),
                cancellationToken);

            SuccessMessage = "Temperature excursion incident resolved and signed off.";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }

        return RedirectToPage();
    }
}
