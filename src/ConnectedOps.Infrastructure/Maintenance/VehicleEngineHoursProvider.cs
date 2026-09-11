using ConnectedOps.Application.Maintenance;
using ConnectedOps.Domain.Maintenance;
using ConnectedOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ConnectedOps.Infrastructure.Maintenance;

public sealed class VehicleEngineHoursProvider : IVehicleEngineHoursProvider
{
    private readonly ConnectedOpsDbContext _dbContext;

    public VehicleEngineHoursProvider(ConnectedOpsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<decimal?> GetCurrentEngineHoursAsync(
        Guid vehicleId,
        CancellationToken cancellationToken = default)
    {
        // 1. Telemetry engine hours
        var telemetryState = await _dbContext.VehicleTelemetryStates
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.VehicleId == vehicleId, cancellationToken);

        if (telemetryState?.EngineHours is not null && telemetryState.EngineHours > 0)
        {
            return telemetryState.EngineHours.Value;
        }

        // 2. Latest completed maintenance record
        var latestRecord = await _dbContext.VehicleMaintenanceRecords
            .AsNoTracking()
            .Where(x => x.VehicleId == vehicleId && x.Status == MaintenanceRecordStatus.Completed && x.EngineHours != null)
            .OrderByDescending(x => x.CompletedDateTimeUtc ?? x.ServiceDateUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (latestRecord?.EngineHours is not null)
        {
            return latestRecord.EngineHours.Value;
        }

        // 3. Plan assignment baseline
        var assignment = await _dbContext.VehicleMaintenancePlanAssignments
            .AsNoTracking()
            .Where(x => x.VehicleId == vehicleId && x.IsActive && x.BaselineEngineHours != null)
            .OrderByDescending(x => x.EffectiveFromUtc)
            .FirstOrDefaultAsync(cancellationToken);

        return assignment?.BaselineEngineHours;
    }
}
