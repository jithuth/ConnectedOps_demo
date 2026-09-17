using ConnectedOps.Application.Vehicles;
using ConnectedOps.Domain.Predictive;

namespace ConnectedOps.Application.Predictive;

public interface IPredictiveMaintenanceService
{
    Task<PredictiveFleetDashboardDto> GetFleetDashboardAsync(CancellationToken cancellationToken = default);

    Task<VehicleDigitalTwinHealthDto?> GetVehicleDigitalTwinAsync(Guid vehicleId, CancellationToken cancellationToken = default);

    Task<PagedResult<PredictiveAlertDto>> GetPredictiveAlertsPagedAsync(
        Guid? vehicleId = null,
        PredictiveRiskLevel? riskLevel = null,
        PredictiveRecommendationStatus? status = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);

    Task<int> RunFleetDiagnosticEvaluationAsync(Guid? vehicleId = null, CancellationToken cancellationToken = default);

    Task<Guid> PromoteAlertToWorkOrderAsync(Guid alertId, PromoteToWorkOrderRequest request, CancellationToken cancellationToken = default);

    Task<bool> DismissAlertAsync(Guid alertId, string reason, CancellationToken cancellationToken = default);
}
