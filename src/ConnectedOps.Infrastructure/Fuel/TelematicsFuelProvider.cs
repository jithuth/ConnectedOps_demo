using ConnectedOps.Application.Common.Interfaces;
using ConnectedOps.Application.Fuel;
using ConnectedOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ConnectedOps.Infrastructure.Fuel;

public sealed class TelematicsFuelProvider : ITelematicsFuelProvider
{
    private readonly ConnectedOpsDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;

    public TelematicsFuelProvider(
        ConnectedOpsDbContext dbContext,
        ICurrentUserContext currentUserContext)
    {
        _dbContext = dbContext;
        _currentUserContext = currentUserContext;
    }

    public async Task<decimal?> GetCurrentFuelLevelPercentAsync(
        Guid vehicleId,
        CancellationToken cancellationToken = default)
    {
        var tenantId = _currentUserContext.TenantId;

        var state = await _dbContext.VehicleTelemetryStates
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.VehicleId == vehicleId && (tenantId == null || x.TenantId == tenantId), cancellationToken);

        return state?.FuelLevelPercent;
    }

    public async Task<decimal?> GetCurrentOdometerKmAsync(
        Guid vehicleId,
        CancellationToken cancellationToken = default)
    {
        var tenantId = _currentUserContext.TenantId;

        var state = await _dbContext.VehicleTelemetryStates
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.VehicleId == vehicleId && (tenantId == null || x.TenantId == tenantId), cancellationToken);

        return state?.OdometerKm;
    }

    public async Task<decimal?> GetCurrentEngineHoursAsync(
        Guid vehicleId,
        CancellationToken cancellationToken = default)
    {
        var tenantId = _currentUserContext.TenantId;

        var state = await _dbContext.VehicleTelemetryStates
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.VehicleId == vehicleId && (tenantId == null || x.TenantId == tenantId), cancellationToken);

        return state?.EngineHours;
    }
}
