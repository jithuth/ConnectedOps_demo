using ConnectedOps.Application.Telematics;
using ConnectedOps.Application.Geofences;
using ConnectedOps.Application.Vehicles;
using ConnectedOps.Domain.Telematics;
using ConnectedOps.Domain.Vehicles;
using ConnectedOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ConnectedOps.Infrastructure.Telematics;

public sealed class TelemetryIngestionService : ITelemetryIngestionService
{
    private readonly ConnectedOpsDbContext _dbContext;
    private readonly ITelemetryValidationService _validationService;
    private readonly ITelemetryDeduplicationService _deduplicationService;
    private readonly IVehicleOdometerService _odometerService;
    private readonly IGeofenceEvaluationService? _geofenceEvaluationService;
    private readonly ILogger<TelemetryIngestionService> _logger;

    public TelemetryIngestionService(
        ConnectedOpsDbContext dbContext,
        ITelemetryValidationService validationService,
        ITelemetryDeduplicationService deduplicationService,
        IVehicleOdometerService odometerService,
        ILogger<TelemetryIngestionService> logger,
        IGeofenceEvaluationService? geofenceEvaluationService = null)
    {
        _dbContext = dbContext;
        _validationService = validationService;
        _deduplicationService = deduplicationService;
        _odometerService = odometerService;
        _logger = logger;
        _geofenceEvaluationService = geofenceEvaluationService;
    }

    public async Task<TelemetryIngestionResult> IngestAsync(
        NormalizedTelemetryMessage message,
        CancellationToken cancellationToken = default)
    {
        // 1. Identify device by DeviceIdentifier or IMEI
        var device = await _dbContext.TrackingDevices
            .Include(x => x.Provider)
            .FirstOrDefaultAsync(x =>
                (x.DeviceIdentifier == message.DeviceIdentifier || (x.IMEI != null && x.IMEI == message.DeviceIdentifier)) &&
                !x.IsDeleted,
                cancellationToken);

        if (device == null)
        {
            _logger.LogWarning(
                "Ingestion rejected: Device '{DeviceIdentifier}' from provider '{Provider}' is not registered.",
                message.DeviceIdentifier,
                message.Provider);

            var failure = new TelemetryIngestionFailure(
                message.DeviceIdentifier,
                message.Provider,
                "Device not registered in platform.",
                tenantId: null);

            _dbContext.TelemetryIngestionFailures.Add(failure);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return new TelemetryIngestionResult(
                Success: false,
                TelemetryRecordId: null,
                DeviceIdentifier: message.DeviceIdentifier,
                DeviceId: null,
                VehicleId: null,
                IsQuarantined: true,
                ErrorMessage: "Device is not registered in ConnectedOps.");
        }

        // 2. Resolve Tenant strictly from registered device
        var tenantId = device.TenantId;

        // 3. Retrieve tenant telematics settings
        var settings = await _dbContext.TelematicsSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId, cancellationToken);

        int maxFutureMinutes = settings?.MaxAcceptedFutureMinutes ?? 15;
        int maxPastDays = settings?.MaxAcceptedPastDays ?? 7;
        decimal odometerThresholdKm = settings?.OdometerUpdateThresholdKm ?? 1.0m;

        // 4. Validate telemetry payload
        var validation = _validationService.Validate(message, maxFutureMinutes, maxPastDays);
        if (!validation.IsValid)
        {
            _logger.LogWarning(
                "Ingestion quarantined: Payload from device '{DeviceIdentifier}' failed validation: {Reason}",
                device.DeviceIdentifier,
                validation.Reason);

            var failure = new TelemetryIngestionFailure(
                device.DeviceIdentifier,
                message.Provider,
                validation.Reason ?? "Validation failed.",
                tenantId: tenantId);

            _dbContext.TelemetryIngestionFailures.Add(failure);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return new TelemetryIngestionResult(
                Success: false,
                TelemetryRecordId: null,
                DeviceIdentifier: device.DeviceIdentifier,
                DeviceId: device.Id,
                VehicleId: null,
                IsQuarantined: true,
                ErrorMessage: validation.Reason);
        }

        // 5. Check deduplication
        var isDuplicate = await _deduplicationService.IsDuplicateAsync(
            tenantId,
            device.Id,
            message,
            cancellationToken);

        if (isDuplicate)
        {
            _logger.LogDebug(
                "Ingestion ignored duplicate packet for device '{DeviceIdentifier}' at {RecordedAtUtc}",
                device.DeviceIdentifier,
                message.RecordedAtUtc);

            return new TelemetryIngestionResult(
                Success: true,
                TelemetryRecordId: null,
                DeviceIdentifier: device.DeviceIdentifier,
                DeviceId: device.Id,
                VehicleId: null,
                IsDuplicate: true,
                RecordedAtUtc: message.RecordedAtUtc);
        }

        // 6. Resolve active vehicle assignment
        var activeAssignment = await _dbContext.TrackingDeviceVehicleAssignments
            .Include(a => a.Vehicle)
            .FirstOrDefaultAsync(a => a.TrackingDeviceId == device.Id && a.IsActive && a.TenantId == tenantId, cancellationToken);

        var vehicleId = activeAssignment?.VehicleId;
        var vehicle = activeAssignment?.Vehicle;

        // 7. Create immutable telemetry record
        var telemetryRecord = new TelemetryRecord(
            tenantId: tenantId,
            trackingDeviceId: device.Id,
            recordedAtUtc: message.RecordedAtUtc,
            receivedAtUtc: message.ReceivedAtUtc,
            sourceProvider: message.Provider,
            vehicleId: vehicleId,
            latitude: message.Latitude,
            longitude: message.Longitude,
            altitudeMeters: message.AltitudeMeters,
            speedKph: message.SpeedKph,
            headingDegrees: message.HeadingDegrees,
            ignitionOn: message.IgnitionOn,
            odometerKm: message.OdometerKm,
            engineHours: message.EngineHours,
            fuelLevelPercent: message.FuelLevelPercent,
            fuelVolumeLiters: message.FuelVolumeLiters,
            batteryVoltage: message.BatteryVoltage,
            externalPowerVoltage: message.ExternalPowerVoltage,
            gsmSignal: message.GsmSignal,
            gpsSatellites: message.GpsSatellites,
            temperatureCelsius: message.TemperatureCelsius,
            digitalInput1: message.DigitalInput1,
            digitalInput2: message.DigitalInput2,
            rawEventCode: message.RawEventCode,
            eventType: message.EventType,
            providerMessageId: message.ProviderMessageId);

        _dbContext.TelemetryRecords.Add(telemetryRecord);

        // 8. Update device telemetry state
        int? batteryPct = null;
        if (message.AdditionalIoElements != null && message.AdditionalIoElements.TryGetValue(113, out var bpVal))
        {
            batteryPct = Convert.ToInt32(bpVal);
        }

        device.UpdateTelemetryState(
            message.Latitude,
            message.Longitude,
            message.SpeedKph,
            message.HeadingDegrees,
            message.IgnitionOn,
            batteryPct,
            message.ExternalPowerVoltage,
            message.BatteryVoltage,
            message.GsmSignal,
            message.RecordedAtUtc,
            message.ReceivedAtUtc);

        // 9. Update current vehicle position projection if vehicle assigned and coordinates present
        if (vehicle != null && message.Latitude.HasValue && message.Longitude.HasValue)
        {
            var currentState = await _dbContext.VehicleTelemetryStates
                .FirstOrDefaultAsync(x => x.VehicleId == vehicle.Id, cancellationToken);

            if (currentState == null)
            {
                currentState = new VehicleTelemetryState(
                    vehicleId: vehicle.Id,
                    tenantId: tenantId,
                    trackingDeviceId: device.Id,
                    recordedAtUtc: message.RecordedAtUtc,
                    receivedAtUtc: message.ReceivedAtUtc,
                    latitude: message.Latitude.Value,
                    longitude: message.Longitude.Value,
                    altitudeMeters: message.AltitudeMeters,
                    speedKph: message.SpeedKph,
                    headingDegrees: message.HeadingDegrees,
                    ignitionOn: message.IgnitionOn,
                    odometerKm: message.OdometerKm,
                    engineHours: message.EngineHours,
                    fuelLevelPercent: message.FuelLevelPercent,
                    batteryVoltage: message.BatteryVoltage,
                    externalPowerVoltage: message.ExternalPowerVoltage,
                    signalStrength: message.GsmSignal);

                _dbContext.VehicleTelemetryStates.Add(currentState);
            }
            else
            {
                // Enforce newer-timestamp-only projection rule
                currentState.UpdateIfNewer(
                    trackingDeviceId: device.Id,
                    recordedAtUtc: message.RecordedAtUtc,
                    receivedAtUtc: message.ReceivedAtUtc,
                    latitude: message.Latitude.Value,
                    longitude: message.Longitude.Value,
                    altitudeMeters: message.AltitudeMeters,
                    speedKph: message.SpeedKph,
                    headingDegrees: message.HeadingDegrees,
                    ignitionOn: message.IgnitionOn,
                    odometerKm: message.OdometerKm,
                    engineHours: message.EngineHours,
                    fuelLevelPercent: message.FuelLevelPercent,
                    batteryVoltage: message.BatteryVoltage,
                    externalPowerVoltage: message.ExternalPowerVoltage,
                    signalStrength: message.GsmSignal);
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        // 10. Phase 7 Geofencing Integration: evaluate vehicle position against active geofences
        if (_geofenceEvaluationService != null && vehicle != null && message.Latitude.HasValue && message.Longitude.HasValue)
        {
            try
            {
                await _geofenceEvaluationService.EvaluatePositionAsync(
                    tenantId,
                    vehicle.Id,
                    message.Latitude.Value,
                    message.Longitude.Value,
                    message.RecordedAtUtc,
                    message.ReceivedAtUtc,
                    device.Id,
                    telemetryRecord.Id,
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Geofence evaluation failed for vehicle '{VehicleId}'", vehicle.Id);
            }
        }

        // 11. Phase 3 Odometer Integration: conditional update if threshold met
        if (vehicle != null && message.OdometerKm.HasValue && message.OdometerKm.Value > vehicle.CurrentOdometer)
        {
            var diff = message.OdometerKm.Value - vehicle.CurrentOdometer;
            if (diff >= odometerThresholdKm)
            {
                try
                {
                    await _odometerService.RecordOdometerAsync(
                        vehicle.Id,
                        new RecordVehicleOdometerRequest(
                            Reading: message.OdometerKm.Value,
                            Unit: OdometerUnit.Kilometers,
                            ReadingDateUtc: message.RecordedAtUtc,
                            Source: OdometerSource.Telematics,
                            Notes: $"Automated telematics telemetry update from device '{device.DeviceIdentifier}'"),
                        cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Failed to record telematics odometer update for vehicle '{VehicleNumber}'",
                        vehicle.VehicleNumber);
                }
            }
        }

        return new TelemetryIngestionResult(
            Success: true,
            TelemetryRecordId: telemetryRecord.Id,
            DeviceIdentifier: device.DeviceIdentifier,
            DeviceId: device.Id,
            VehicleId: vehicleId,
            IsDuplicate: false,
            IsQuarantined: false,
            RecordedAtUtc: message.RecordedAtUtc);
    }

    public async Task<IReadOnlyList<TelemetryIngestionResult>> IngestBatchAsync(
        IReadOnlyList<NormalizedTelemetryMessage> messages,
        CancellationToken cancellationToken = default)
    {
        var results = new List<TelemetryIngestionResult>(messages.Count);
        foreach (var message in messages)
        {
            var result = await IngestAsync(message, cancellationToken);
            results.Add(result);
        }
        return results;
    }
}
