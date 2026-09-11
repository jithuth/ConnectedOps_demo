using ConnectedOps.Application.Maintenance;
using ConnectedOps.Domain.Maintenance;
using ConnectedOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ConnectedOps.Infrastructure.Maintenance;

public sealed class MaintenanceDueEvaluationService : IMaintenanceDueEvaluationService
{
    private readonly ConnectedOpsDbContext _dbContext;
    private readonly IMaintenanceScheduleService _scheduleService;

    public MaintenanceDueEvaluationService(
        ConnectedOpsDbContext dbContext,
        IMaintenanceScheduleService scheduleService)
    {
        _dbContext = dbContext;
        _scheduleService = scheduleService;
    }

    public async Task EvaluateAndRefreshProjectionsAsync(
        Guid? vehicleId = null,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyCollection<VehicleMaintenanceDueDto> calculatedItems;

        if (vehicleId.HasValue)
        {
            calculatedItems = await _scheduleService.CalculateVehicleMaintenanceAsync(vehicleId.Value, cancellationToken);
        }
        else
        {
            calculatedItems = await _scheduleService.CalculateAllVehiclesMaintenanceAsync(cancellationToken: cancellationToken);
        }

        foreach (var item in calculatedItems)
        {
            var existingProjection = await _dbContext.VehicleMaintenanceDues
                .FirstOrDefaultAsync(x => x.VehicleId == item.VehicleId && x.MaintenancePlanRuleId == item.MaintenancePlanRuleId, cancellationToken);

            if (existingProjection is not null)
            {
                existingProjection.UpdateProjection(
                    null,
                    item.NextDueDateUtc,
                    item.NextDueOdometer,
                    item.NextDueEngineHours,
                    item.DueStatus,
                    item.CalculatedAtUtc);
            }
            else
            {
                var newProjection = new VehicleMaintenanceDue(
                    _dbContext.Vehicles.Where(v => v.Id == item.VehicleId).Select(v => v.TenantId).FirstOrDefault(),
                    item.VehicleId,
                    item.MaintenancePlanRuleId,
                    null,
                    item.NextDueDateUtc,
                    item.NextDueOdometer,
                    item.NextDueEngineHours,
                    item.DueStatus,
                    item.CalculatedAtUtc);

                _dbContext.VehicleMaintenanceDues.Add(newProjection);
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
