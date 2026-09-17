using ConnectedOps.Application.Ev;
using ConnectedOps.Domain.Ev;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ConnectedOps.Web.Pages.Ev;

[Authorize]
public class DashboardModel : PageModel
{
    private readonly IEvService _evService;

    public DashboardModel(IEvService evService)
    {
        _evService = evService;
    }

    public EvFleetDashboardDto Dashboard { get; private set; } = null!;

    [TempData]
    public string? SuccessMessage { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Dashboard = await _evService.GetDashboardAsync(cancellationToken);
    }

    public async Task<IActionResult> OnPostCreateStationAsync(
        string code,
        string name,
        ChargingStationType stationType,
        ChargingConnectorType connectorType,
        decimal maxPowerKw,
        int totalPlugs,
        string? address,
        decimal offPeakRatePerKwh,
        decimal peakRatePerKwh,
        CancellationToken cancellationToken)
    {
        try
        {
            var req = new CreateChargingStationRequest(
                Code: code,
                Name: name,
                StationType: stationType,
                ConnectorType: connectorType,
                MaxPowerKw: maxPowerKw,
                TotalPlugs: totalPlugs,
                Address: address,
                OffPeakRatePerKwh: offPeakRatePerKwh,
                PeakRatePerKwh: peakRatePerKwh);

            await _evService.CreateStationAsync(req, cancellationToken);
            SuccessMessage = $"Charging station '{name}' registered successfully.";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostStartSessionAsync(
        Guid vehicleId,
        Guid chargingStationId,
        decimal startSocPercent,
        bool isScheduled,
        DateTime? scheduledStartUtc,
        CancellationToken cancellationToken)
    {
        try
        {
            var req = new StartChargingSessionRequest(
                VehicleId: vehicleId,
                ChargingStationId: chargingStationId,
                StartSocPercent: startSocPercent,
                IsScheduled: isScheduled,
                ScheduledStartUtc: scheduledStartUtc);

            await _evService.StartSessionAsync(req, cancellationToken);
            SuccessMessage = "Charging session initiated successfully.";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostCompleteSessionAsync(
        Guid sessionId,
        decimal endSocPercent,
        decimal energyDeliveredKwh,
        decimal totalCost,
        CancellationToken cancellationToken)
    {
        try
        {
            var req = new CompleteChargingSessionRequest(
                SessionId: sessionId,
                EndSocPercent: endSocPercent,
                EnergyDeliveredKwh: energyDeliveredKwh,
                TotalCost: totalCost);

            await _evService.CompleteSessionAsync(req, cancellationToken);
            SuccessMessage = $"Charging session completed. Delivered {energyDeliveredKwh} kWh.";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostConfigureSmartChargingAsync(
        Guid vehicleId,
        int targetSocLimitPercent,
        bool isOffPeakOnlyCharging,
        CancellationToken cancellationToken)
    {
        try
        {
            var req = new ConfigureSmartChargingRequest(
                VehicleId: vehicleId,
                TargetSocLimitPercent: targetSocLimitPercent,
                IsOffPeakOnlyCharging: isOffPeakOnlyCharging);

            await _evService.ConfigureSmartChargingAsync(req, cancellationToken);
            SuccessMessage = "Smart charging schedule and SoC threshold updated.";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }

        return RedirectToPage();
    }
}
