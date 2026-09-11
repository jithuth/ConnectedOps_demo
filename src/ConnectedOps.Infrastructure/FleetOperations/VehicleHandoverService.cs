using ConnectedOps.Application.Auditing;
using ConnectedOps.Application.Common.Interfaces;
using ConnectedOps.Application.Drivers;
using ConnectedOps.Application.FleetOperations;
using ConnectedOps.Application.Vehicles;
using ConnectedOps.Domain.Auditing;
using ConnectedOps.Domain.Drivers;
using ConnectedOps.Domain.FleetOperations;
using ConnectedOps.Domain.Vehicles;
using ConnectedOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ConnectedOps.Infrastructure.FleetOperations;

public sealed class VehicleHandoverService : IVehicleHandoverService
{
    private readonly ConnectedOpsDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IAuditLogService _auditLogService;
    private readonly IVehicleOdometerService _odometerService;
    private readonly IDriverEligibilityService _driverEligibilityService;

    public VehicleHandoverService(
        ConnectedOpsDbContext dbContext,
        ICurrentUserContext currentUserContext,
        IAuditLogService auditLogService,
        IVehicleOdometerService odometerService,
        IDriverEligibilityService driverEligibilityService)
    {
        _dbContext = dbContext;
        _currentUserContext = currentUserContext;
        _auditLogService = auditLogService;
        _odometerService = odometerService;
        _driverEligibilityService = driverEligibilityService;
    }

    public async Task<IReadOnlyCollection<VehicleHandoverDto>> GetHandoversAsync(
        Guid? vehicleId = null,
        Guid? driverId = null,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();

        var query = _dbContext.VehicleHandovers
            .AsNoTracking()
            .Include(h => h.Vehicle)
            .Include(h => h.FromDriver)
            .Include(h => h.ToDriver)
            .Where(h => h.TenantId == tenantId);

        if (vehicleId.HasValue)
            query = query.Where(h => h.VehicleId == vehicleId.Value);

        if (driverId.HasValue)
            query = query.Where(h => h.FromDriverId == driverId.Value || h.ToDriverId == driverId.Value);

        var handovers = await query
            .OrderByDescending(h => h.HandoverAtUtc)
            .ToListAsync(cancellationToken);

        return handovers.Select(MapToDto).ToList();
    }

    public async Task<VehicleHandoverDto> GetHandoverByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();

        var handover = await _dbContext.VehicleHandovers
            .AsNoTracking()
            .Include(h => h.Vehicle)
            .Include(h => h.FromDriver)
            .Include(h => h.ToDriver)
            .FirstOrDefaultAsync(h => h.Id == id && h.TenantId == tenantId, cancellationToken);

        if (handover is null)
            throw new KeyNotFoundException($"Vehicle handover '{id}' was not found.");

        return MapToDto(handover);
    }

    public async Task<VehicleHandoverDto> CreateHandoverAsync(
        CreateVehicleHandoverRequest request,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();
        var userId = _currentUserContext.UserId;

        var vehicle = await _dbContext.Vehicles
            .FirstOrDefaultAsync(v => v.Id == request.VehicleId && v.TenantId == tenantId, cancellationToken);
        if (vehicle is null)
            throw new KeyNotFoundException($"Vehicle '{request.VehicleId}' was not found.");

        if (vehicle.Status != VehicleStatus.Active)
            throw new InvalidOperationException($"Cannot perform handover for vehicle with status '{vehicle.Status}'.");

        var toDriver = await _dbContext.Drivers
            .FirstOrDefaultAsync(d => d.Id == request.ToDriverId && d.TenantId == tenantId, cancellationToken);
        if (toDriver is null)
            throw new KeyNotFoundException($"Driver '{request.ToDriverId}' was not found.");

        if (toDriver.Status != DriverStatus.Active)
            throw new InvalidOperationException($"Cannot handover vehicle to driver with status '{toDriver.Status}'. Driver must be Active.");

        // Check if toDriver is already in an open session with another vehicle
        var toDriverOpenSession = await _dbContext.VehicleUsageSessions
            .AnyAsync(s => s.TenantId == tenantId && s.DriverId == request.ToDriverId && s.Status == UsageSessionStatus.Open && s.VehicleId != request.VehicleId, cancellationToken);
        if (toDriverOpenSession)
            throw new InvalidOperationException($"Target driver '{toDriver.DisplayName}' already has an active session on another vehicle.");

        // Evaluate eligibility for target driver
        var eligibility = await _driverEligibilityService.EvaluateAsync(request.ToDriverId, request.VehicleId, cancellationToken);
        if (!eligibility.IsEligible)
        {
            var ex = new FleetOperationalException(
                tenantId,
                OperationalExceptionType.DriverUnavailable,
                OperationalExceptionSeverity.High,
                $"Driver '{toDriver.DisplayName}' ineligible for handover on vehicle '{vehicle.VehicleNumber}': {string.Join("; ", eligibility.Reasons)}",
                request.VehicleId,
                request.ToDriverId,
                null,
                DateTime.UtcNow);
            _dbContext.FleetOperationalExceptions.Add(ex);
            await _dbContext.SaveChangesAsync(cancellationToken);

            throw new InvalidOperationException($"Driver eligibility check failed: {string.Join("; ", eligibility.Reasons)}");
        }

        var handoverTime = request.HandoverAtUtc ?? DateTime.UtcNow;

        // Check for existing active session on this vehicle
        var existingOpenSession = await _dbContext.VehicleUsageSessions
            .Include(s => s.Driver)
            .FirstOrDefaultAsync(s => s.TenantId == tenantId && s.VehicleId == request.VehicleId && s.Status == UsageSessionStatus.Open, cancellationToken);

        Guid? fromDriverId = request.FromDriverId ?? existingOpenSession?.DriverId;
        Guid? fromSessionId = existingOpenSession?.Id;
        Guid? toSessionId = null;

        if (fromDriverId.HasValue && fromDriverId.Value == request.ToDriverId)
            throw new InvalidOperationException("Cannot handover vehicle to the same driver.");

        // Close the previous session if open
        if (existingOpenSession is not null)
        {
            if (request.Odometer < existingOpenSession.StartOdometer)
            {
                var discrepancyEx = new FleetOperationalException(
                    tenantId,
                    OperationalExceptionType.OdometerMismatch,
                    OperationalExceptionSeverity.High,
                    $"Handover odometer ({request.Odometer}) is less than session start odometer ({existingOpenSession.StartOdometer}) on vehicle '{vehicle.VehicleNumber}'.",
                    request.VehicleId,
                    fromDriverId,
                    existingOpenSession.Id,
                    DateTime.UtcNow);
                _dbContext.FleetOperationalExceptions.Add(discrepancyEx);
                await _dbContext.SaveChangesAsync(cancellationToken);

                throw new InvalidOperationException($"Handover odometer ({request.Odometer}) cannot be less than current session start odometer ({existingOpenSession.StartOdometer}).");
            }

            existingOpenSession.CheckIn(
                request.Odometer,
                request.LocationId,
                request.Condition,
                userId,
                $"Transferred custody via handover to {toDriver.DisplayName}. Notes: {request.Notes}",
                handoverTime);

            // Record check-in condition record
            var closeConditionRecord = new VehicleConditionRecord(
                tenantId,
                request.VehicleId,
                request.Condition,
                request.Odometer,
                existingOpenSession.DriverId,
                existingOpenSession.Id,
                userId,
                handoverTime,
                $"Check-in via handover to {toDriver.DisplayName}");
            _dbContext.VehicleConditionRecords.Add(closeConditionRecord);
        }

        // Open new session for target driver
        var newSession = new VehicleUsageSession(
            tenantId,
            request.VehicleId,
            request.ToDriverId,
            request.Odometer,
            request.OdometerUnit,
            handoverTime,
            userId,
            request.LocationId,
            null,
            null,
            request.Condition,
            "Handover transfer",
            null,
            request.Notes);

        _dbContext.VehicleUsageSessions.Add(newSession);
        toSessionId = newSession.Id;

        // Condition record for start of new session
        var openConditionRecord = new VehicleConditionRecord(
            tenantId,
            request.VehicleId,
            request.Condition,
            request.Odometer,
            request.ToDriverId,
            toSessionId,
            userId,
            handoverTime,
            $"Custody acquired via handover. Notes: {request.Notes}");
        _dbContext.VehicleConditionRecords.Add(openConditionRecord);

        // Record odometer reading via Phase 3 service
        await _odometerService.RecordOdometerAsync(
            request.VehicleId,
            new RecordVehicleOdometerRequest(
                request.Odometer,
                request.OdometerUnit,
                handoverTime,
                OdometerSource.Manual,
                $"Recorded during vehicle handover to driver {toDriver.DisplayName}"),
            cancellationToken);

        // Create Handover record
        var handover = new VehicleHandover(
            tenantId,
            request.VehicleId,
            request.ToDriverId,
            request.Odometer,
            request.OdometerUnit,
            fromDriverId,
            fromSessionId,
            toSessionId,
            handoverTime,
            request.LocationId,
            request.Condition,
            request.Notes,
            request.AcknowledgedByFromDriver,
            request.AcknowledgedByToDriver);

        _dbContext.VehicleHandovers.Add(handover);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(
            new CreateAuditLogRequest(
                AuditAction.VehicleHandedOver,
                "VehicleHandover",
                handover.Id.ToString(),
                $"Handed over vehicle '{vehicle.VehicleNumber}' from driver '{(fromDriverId.HasValue ? existingOpenSession?.Driver?.DisplayName ?? fromDriverId.ToString() : "None")}' to driver '{toDriver.DisplayName}'"),
            cancellationToken);

        return await GetHandoverByIdAsync(handover.Id, cancellationToken);
    }

    private static VehicleHandoverDto MapToDto(VehicleHandover h) =>
        new(
            h.Id,
            h.TenantId,
            h.VehicleId,
            h.Vehicle?.VehicleNumber ?? string.Empty,
            h.Vehicle?.DisplayName ?? string.Empty,
            h.FromDriverId,
            h.FromDriver?.DisplayName,
            h.ToDriverId,
            h.ToDriver?.DisplayName ?? string.Empty,
            h.FromUsageSessionId,
            h.ToUsageSessionId,
            h.HandoverAtUtc,
            h.Odometer,
            h.OdometerUnit,
            h.LocationId,
            h.Condition,
            h.Notes,
            h.AcknowledgedByFromDriver,
            h.AcknowledgedByToDriver,
            h.CreatedAtUtc);

    private Guid GetCurrentTenantId()
    {
        if (_currentUserContext.TenantId is not Guid tenantId)
            throw new UnauthorizedAccessException("Tenant ID is missing.");
        return tenantId;
    }
}
