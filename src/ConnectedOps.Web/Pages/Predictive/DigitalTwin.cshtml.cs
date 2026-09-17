using ConnectedOps.Application.Predictive;
using ConnectedOps.Application.Vehicles;
using ConnectedOps.Domain.Predictive;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ConnectedOps.Web.Pages.Predictive;

[Authorize]
public class DigitalTwinModel : PageModel
{
    private readonly IPredictiveMaintenanceService _predictiveService;

    public DigitalTwinModel(IPredictiveMaintenanceService predictiveService)
    {
        _predictiveService = predictiveService;
    }

    public PredictiveFleetDashboardDto Dashboard { get; private set; } = null!;
    public PagedResult<PredictiveAlertDto> Alerts { get; private set; } = null!;
    public VehicleDigitalTwinHealthDto? SelectedTwin { get; private set; }

    [BindProperty(SupportsGet = true)]
    public Guid? VehicleId { get; set; }

    [BindProperty(SupportsGet = true)]
    public PredictiveRiskLevel? RiskLevel { get; set; }

    [BindProperty(SupportsGet = true)]
    public PredictiveRecommendationStatus? Status { get; set; } = PredictiveRecommendationStatus.Active;

    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    [TempData]
    public string? SuccessMessage { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Dashboard = await _predictiveService.GetFleetDashboardAsync(cancellationToken);

        Alerts = await _predictiveService.GetPredictiveAlertsPagedAsync(
            VehicleId,
            RiskLevel,
            Status,
            PageNumber,
            pageSize: 15,
            cancellationToken);

        if (VehicleId.HasValue)
        {
            SelectedTwin = await _predictiveService.GetVehicleDigitalTwinAsync(VehicleId.Value, cancellationToken);
        }
    }

    public async Task<IActionResult> OnPostRunDiagnosticsAsync(Guid? vehicleId, CancellationToken cancellationToken)
    {
        try
        {
            var count = await _predictiveService.RunFleetDiagnosticEvaluationAsync(vehicleId, cancellationToken);
            SuccessMessage = $"AI Telemetry diagnostics complete: Generated / refreshed {count} predictive maintenance alerts.";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }

        return RedirectToPage(new { VehicleId = vehicleId });
    }

    public async Task<IActionResult> OnPostPromoteToWorkOrderAsync(
        Guid alertId,
        string? notes,
        DateTime? scheduledDateUtc,
        CancellationToken cancellationToken)
    {
        try
        {
            var workOrderId = await _predictiveService.PromoteAlertToWorkOrderAsync(
                alertId,
                new PromoteToWorkOrderRequest(notes, scheduledDateUtc),
                cancellationToken);

            SuccessMessage = "Predictive alert successfully promoted to Scheduled Maintenance Work Order.";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDismissAlertAsync(Guid alertId, string reason, CancellationToken cancellationToken)
    {
        try
        {
            await _predictiveService.DismissAlertAsync(alertId, reason, cancellationToken);
            SuccessMessage = "Predictive maintenance alert dismissed.";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }

        return RedirectToPage();
    }
}
