using ConnectedOps.Application.Common.Interfaces;
using ConnectedOps.Application.Telematics;
using ConnectedOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ConnectedOps.Infrastructure.Telematics;

public sealed class TelemetryHistoryService : ITelemetryHistoryService
{
    private const int MaxQueryDays = 31;
    private const int DefaultHours = 24;

    private readonly ConnectedOpsDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;

    public TelemetryHistoryService(
        ConnectedOpsDbContext dbContext,
        ICurrentUserContext currentUserContext)
    {
        _dbContext = dbContext;
        _currentUserContext = currentUserContext;
    }

    private Guid RequireTenantId()
    {
        return _currentUserContext.TenantId ??
               throw new InvalidOperationException("Active tenant context is required for telemetry history.");
    }

    public async Task<TelemetryHistoryPagedResult> GetVehicleTelemetryHistoryAsync(
        Guid vehicleId,
        TelemetryHistoryQueryParameters parameters,
        CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();
        var (fromUtc, toUtc) = NormalizeWindow(parameters.FromUtc, parameters.ToUtc);

        var query = _dbContext.TelemetryRecords
            .AsNoTracking()
            .Include(x => x.TrackingDevice)
            .Include(x => x.Vehicle)
            .Where(x => x.TenantId == tenantId &&
                        x.VehicleId == vehicleId &&
                        x.RecordedAtUtc >= fromUtc &&
                        x.RecordedAtUtc <= toUtc);

        if (parameters.EventType.HasValue)
        {
            query = query.Where(x => x.EventType == parameters.EventType.Value);
        }

        int total = await query.CountAsync(cancellationToken);

        int pageSize = Math.Clamp(parameters.PageSize, 1, 500);
        int page = Math.Max(1, parameters.Page);

        var items = await query
            .OrderByDescending(x => x.RecordedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new TelemetryRecordDto(
                x.Id,
                x.TenantId,
                x.TrackingDeviceId,
                x.TrackingDevice.DeviceIdentifier,
                x.VehicleId,
                x.Vehicle != null ? (x.Vehicle.DisplayName ?? x.Vehicle.VehicleNumber) : null,
                x.RecordedAtUtc,
                x.ReceivedAtUtc,
                x.Latitude,
                x.Longitude,
                x.AltitudeMeters,
                x.SpeedKph,
                x.HeadingDegrees,
                x.IgnitionOn,
                x.OdometerKm,
                x.EngineHours,
                x.FuelLevelPercent,
                x.FuelVolumeLiters,
                x.BatteryVoltage,
                x.ExternalPowerVoltage,
                x.GsmSignal,
                x.GpsSatellites,
                x.TemperatureCelsius,
                x.DigitalInput1,
                x.DigitalInput2,
                x.RawEventCode,
                x.EventType,
                x.SourceProvider,
                x.ProviderMessageId,
                x.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        return new TelemetryHistoryPagedResult(items, total, page, pageSize);
    }

    public async Task<TelemetryHistoryPagedResult> GetDeviceTelemetryHistoryAsync(
        Guid deviceId,
        TelemetryHistoryQueryParameters parameters,
        CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();
        var (fromUtc, toUtc) = NormalizeWindow(parameters.FromUtc, parameters.ToUtc);

        var query = _dbContext.TelemetryRecords
            .AsNoTracking()
            .Include(x => x.TrackingDevice)
            .Include(x => x.Vehicle)
            .Where(x => x.TenantId == tenantId &&
                        x.TrackingDeviceId == deviceId &&
                        x.RecordedAtUtc >= fromUtc &&
                        x.RecordedAtUtc <= toUtc);

        if (parameters.EventType.HasValue)
        {
            query = query.Where(x => x.EventType == parameters.EventType.Value);
        }

        int total = await query.CountAsync(cancellationToken);

        int pageSize = Math.Clamp(parameters.PageSize, 1, 500);
        int page = Math.Max(1, parameters.Page);

        var items = await query
            .OrderByDescending(x => x.RecordedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new TelemetryRecordDto(
                x.Id,
                x.TenantId,
                x.TrackingDeviceId,
                x.TrackingDevice.DeviceIdentifier,
                x.VehicleId,
                x.Vehicle != null ? (x.Vehicle.DisplayName ?? x.Vehicle.VehicleNumber) : null,
                x.RecordedAtUtc,
                x.ReceivedAtUtc,
                x.Latitude,
                x.Longitude,
                x.AltitudeMeters,
                x.SpeedKph,
                x.HeadingDegrees,
                x.IgnitionOn,
                x.OdometerKm,
                x.EngineHours,
                x.FuelLevelPercent,
                x.FuelVolumeLiters,
                x.BatteryVoltage,
                x.ExternalPowerVoltage,
                x.GsmSignal,
                x.GpsSatellites,
                x.TemperatureCelsius,
                x.DigitalInput1,
                x.DigitalInput2,
                x.RawEventCode,
                x.EventType,
                x.SourceProvider,
                x.ProviderMessageId,
                x.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        return new TelemetryHistoryPagedResult(items, total, page, pageSize);
    }

    private static (DateTime FromUtc, DateTime ToUtc) NormalizeWindow(DateTime? fromUtc, DateTime? toUtc)
    {
        var end = toUtc ?? DateTime.UtcNow;
        var start = fromUtc ?? end.AddHours(-DefaultHours);

        if (start > end)
        {
            (start, end) = (end, start);
        }

        var maxWindow = TimeSpan.FromDays(MaxQueryDays);
        if (end - start > maxWindow)
        {
            start = end - maxWindow;
        }

        return (start, end);
    }
}
