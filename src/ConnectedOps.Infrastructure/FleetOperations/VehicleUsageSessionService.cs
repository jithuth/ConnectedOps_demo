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

public sealed class VehicleUsageSessionService : IVehicleUsageSessionService
{
    private readonly ConnectedOpsDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IAuditLogService _auditLogService;
    private readonly IVehicleOdometerService _odometerService;
    private readonly IDriverEligibilityService _driverEligibilityService;

    public VehicleUsageSessionService(
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

    public async Task<PagedResult<VehicleUsageSessionDto>> GetSessionsPagedAsync(
        UsageSessionQueryParameters parameters,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();

        var query = _dbContext.VehicleUsageSessions
            .AsNoTracking()
            .Include(s => s.Vehicle)
            .Include(s => s.Driver)
            .Where(s => s.TenantId == tenantId);

        if (parameters.VehicleId.HasValue)
            query = query.Where(s => s.VehicleId == parameters.VehicleId.Value);

        if (parameters.DriverId.HasValue)
            query = query.Where(s => s.DriverId == parameters.DriverId.Value);

        if (parameters.BranchId.HasValue)
            query = query.Where(s => s.Vehicle.BranchId == parameters.BranchId.Value);

        if (parameters.Status.HasValue)
            query = query.Where(s => s.Status == parameters.Status.Value);

        if (parameters.FromUtc.HasValue)
            query = query.Where(s => s.CheckedOutAtUtc >= parameters.FromUtc.Value);

        if (parameters.ToUtc.HasValue)
            query = query.Where(s => s.CheckedOutAtUtc <= parameters.ToUtc.Value);

        var totalCount = await query.CountAsync(cancellationToken);

        var pageNumber = parameters.PageNumber < 1 ? 1 : parameters.PageNumber;
        var pageSize = parameters.PageSize is < 1 or > 100 ? 20 : parameters.PageSize;

        var items = await query
            .OrderByDescending(s => s.CheckedOutAtUtc)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var dtos = items.Select(MapToDto).ToList();
        return new PagedResult<VehicleUsageSessionDto>(dtos, totalCount, pageNumber, pageSize);
    }

    public async Task<VehicleUsageSessionDto> GetSessionByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();

        var session = await _dbContext.VehicleUsageSessions
            .AsNoTracking()
            .Include(s => s.Vehicle)
            .Include(s => s.Driver)
            .FirstOrDefaultAsync(s => s.Id == id && s.TenantId == tenantId, cancellationToken);

        if (session is null)
            throw new KeyNotFoundException($"Vehicle usage session '{id}' was not found.");

        return MapToDto(session);
    }

    public async Task<VehicleUsageSessionDto?> GetActiveSessionForVehicleAsync(
        Guid vehicleId,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();

        var session = await _dbContext.VehicleUsageSessions
            .AsNoTracking()
            .Include(s => s.Vehicle)
            .Include(s => s.Driver)
            .FirstOrDefaultAsync(s => s.VehicleId == vehicleId && s.TenantId == tenantId && s.Status == UsageSessionStatus.Open, cancellationToken);

        return session is null ? null : MapToDto(session);
    }

    public async Task<VehicleUsageSessionDto?> GetActiveSessionForDriverAsync(
        Guid driverId,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();

        var session = await _dbContext.VehicleUsageSessions
            .AsNoTracking()
            .Include(s => s.Vehicle)
            .Include(s => s.Driver)
            .FirstOrDefaultAsync(s => s.DriverId == driverId && s.TenantId == tenantId && s.Status == UsageSessionStatus.Open, cancellationToken);

        return session is null ? null : MapToDto(session);
    }

    public async Task<IReadOnlyCollection<VehicleUsageSessionDto>> GetVehicleUsageHistoryAsync(
        Guid vehicleId,
        int limit = 50,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();

        var sessions = await _dbContext.VehicleUsageSessions
            .AsNoTracking()
            .Include(s => s.Vehicle)
            .Include(s => s.Driver)
            .Where(s => s.VehicleId == vehicleId && s.TenantId == tenantId)
            .OrderByDescending(s => s.CheckedOutAtUtc)
            .Take(limit)
            .ToListAsync(cancellationToken);

        return sessions.Select(MapToDto).ToList();
    }

    public async Task<IReadOnlyCollection<VehicleUsageSessionDto>> GetDriverUsageHistoryAsync(
        Guid driverId,
        int limit = 50,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();

        var sessions = await _dbContext.VehicleUsageSessions
            .AsNoTracking()
            .Include(s => s.Vehicle)
            .Include(s => s.Driver)
            .Where(s => s.DriverId == driverId && s.TenantId == tenantId)
            .OrderByDescending(s => s.CheckedOutAtUtc)
            .Take(limit)
            .ToListAsync(cancellationToken);

        return sessions.Select(MapToDto).ToList();
    }

    public async Task<VehicleUsageSessionDto> CheckoutVehicleAsync(
        CreateCheckoutRequest request,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();
        var userId = _currentUserContext.UserId;

        var vehicle = await _dbContext.Vehicles
            .FirstOrDefaultAsync(v => v.Id == request.VehicleId && v.TenantId == tenantId, cancellationToken);
        if (vehicle is null)
            throw new KeyNotFoundException($"Vehicle '{request.VehicleId}' was not found.");

        if (vehicle.Status != VehicleStatus.Active)
            throw new InvalidOperationException($"Cannot checkout vehicle with status '{vehicle.Status}'. Vehicle must be Active.");

        var driver = await _dbContext.Drivers
            .FirstOrDefaultAsync(d => d.Id == request.DriverId && d.TenantId == tenantId, cancellationToken);
        if (driver is null)
            throw new KeyNotFoundException($"Driver '{request.DriverId}' was not found.");

        if (driver.Status != DriverStatus.Active)
            throw new InvalidOperationException($"Cannot checkout to driver with status '{driver.Status}'. Driver must be Active.");

        // Evaluate driver eligibility
        var eligibility = await _driverEligibilityService.EvaluateAsync(request.DriverId, request.VehicleId, cancellationToken);
        if (!eligibility.IsEligible)
        {
            var exceptionDescription = $"Driver '{driver.DisplayName}' ineligible for vehicle '{vehicle.VehicleNumber}': {string.Join("; ", eligibility.Reasons)}";
            var ex = new FleetOperationalException(
                tenantId,
                OperationalExceptionType.DriverUnavailable,
                OperationalExceptionSeverity.High,
                exceptionDescription,
                request.VehicleId,
                request.DriverId,
                null,
                DateTime.UtcNow);

            _dbContext.FleetOperationalExceptions.Add(ex);
            await _dbContext.SaveChangesAsync(cancellationToken);

            throw new InvalidOperationException($"Driver eligibility check failed: {string.Join("; ", eligibility.Reasons)}");
        }

        // Check if vehicle is already checked out
        var vehicleAlreadyOpen = await _dbContext.VehicleUsageSessions
            .AnyAsync(s => s.TenantId == tenantId && s.VehicleId == request.VehicleId && s.Status == UsageSessionStatus.Open, cancellationToken);
        if (vehicleAlreadyOpen)
            throw new InvalidOperationException($"Vehicle '{vehicle.VehicleNumber}' is already checked out on an active session.");

        // Check if driver is already on a trip
        var driverAlreadyOpen = await _dbContext.VehicleUsageSessions
            .AnyAsync(s => s.TenantId == tenantId && s.DriverId == request.DriverId && s.Status == UsageSessionStatus.Open, cancellationToken);
        if (driverAlreadyOpen)
            throw new InvalidOperationException($"Driver '{driver.DisplayName}' already has an active checked-out session.");

        var checkedOutTime = request.CheckedOutAtUtc ?? DateTime.UtcNow;

        var session = new VehicleUsageSession(
            tenantId,
            request.VehicleId,
            request.DriverId,
            request.StartOdometer,
            request.OdometerUnit,
            checkedOutTime,
            userId,
            request.StartLocationId,
            request.DriverVehicleAssignmentId,
            request.FleetShiftAssignmentId,
            request.CheckoutCondition,
            request.Purpose,
            request.Reference,
            request.Notes);

        _dbContext.VehicleUsageSessions.Add(session);

        // Condition record for checkout
        var conditionRecord = new VehicleConditionRecord(
            tenantId,
            request.VehicleId,
            request.CheckoutCondition,
            request.StartOdometer,
            request.DriverId,
            session.Id,
            userId,
            checkedOutTime,
            request.Notes);
        _dbContext.VehicleConditionRecords.Add(conditionRecord);

        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(
            new CreateAuditLogRequest(
                AuditAction.VehicleCheckedOut,
                "VehicleUsageSession",
                session.Id.ToString(),
                $"Checked out vehicle '{vehicle.VehicleNumber}' to driver '{driver.DisplayName}' at odometer {request.StartOdometer}"),
            cancellationToken);

        return await GetSessionByIdAsync(session.Id, cancellationToken);
    }

    public async Task<VehicleUsageSessionDto> CheckInVehicleAsync(
        Guid sessionId,
        CheckInSessionRequest request,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();
        var userId = _currentUserContext.UserId;

        var session = await _dbContext.VehicleUsageSessions
            .Include(s => s.Vehicle)
            .Include(s => s.Driver)
            .FirstOrDefaultAsync(s => s.Id == sessionId && s.TenantId == tenantId, cancellationToken);

        if (session is null)
            throw new KeyNotFoundException($"Vehicle usage session '{sessionId}' was not found.");

        if (session.Status != UsageSessionStatus.Open)
            throw new InvalidOperationException($"Cannot check in session with status '{session.Status}'. Session must be Open.");

        // Discrepancy validation
        if (request.EndOdometer < session.StartOdometer)
        {
            var discrepancyException = new FleetOperationalException(
                tenantId,
                OperationalExceptionType.OdometerMismatch,
                OperationalExceptionSeverity.High,
                $"End odometer ({request.EndOdometer}) is less than start odometer ({session.StartOdometer}) for session {sessionId}.",
                session.VehicleId,
                session.DriverId,
                sessionId,
                DateTime.UtcNow);
            _dbContext.FleetOperationalExceptions.Add(discrepancyException);
            await _dbContext.SaveChangesAsync(cancellationToken);

            throw new InvalidOperationException($"End odometer ({request.EndOdometer}) cannot be less than start odometer ({session.StartOdometer}).");
        }

        // Flag AttentionRequired or Unsafe condition
        if (request.Condition is VehicleCondition.AttentionRequired or VehicleCondition.Unsafe)
        {
            var conditionException = new FleetOperationalException(
                tenantId,
                OperationalExceptionType.VehicleConditionIssue,
                OperationalExceptionSeverity.High,
                $"Vehicle '{session.Vehicle.VehicleNumber}' returned in {request.Condition} condition. Notes: {request.Notes}",
                session.VehicleId,
                session.DriverId,
                sessionId,
                DateTime.UtcNow);
            _dbContext.FleetOperationalExceptions.Add(conditionException);
        }

        var checkedInTime = request.CheckedInAtUtc ?? DateTime.UtcNow;

        session.CheckIn(
            request.EndOdometer,
            request.EndLocationId,
            request.Condition,
            userId,
            request.Notes,
            checkedInTime);

        // Record condition record for check-in
        var conditionRecord = new VehicleConditionRecord(
            tenantId,
            session.VehicleId,
            request.Condition,
            request.EndOdometer,
            session.DriverId,
            session.Id,
            userId,
            checkedInTime,
            request.Notes);
        _dbContext.VehicleConditionRecords.Add(conditionRecord);

        // Record odometer reading via Phase 3 service
        await _odometerService.RecordOdometerAsync(
            session.VehicleId,
            new RecordVehicleOdometerRequest(
                request.EndOdometer,
                session.OdometerUnit,
                checkedInTime,
                OdometerSource.Manual,
                $"Recorded during check-in of session {session.Id}"),
            cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(
            new CreateAuditLogRequest(
                AuditAction.VehicleCheckedIn,
                "VehicleUsageSession",
                session.Id.ToString(),
                $"Checked in vehicle '{session.Vehicle.VehicleNumber}' from driver '{session.Driver.DisplayName}' at odometer {request.EndOdometer}"),
            cancellationToken);

        return MapToDto(session);
    }

    public async Task CancelSessionAsync(
        Guid sessionId,
        string? reason,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();
        var userId = _currentUserContext.UserId;

        var session = await _dbContext.VehicleUsageSessions
            .Include(s => s.Vehicle)
            .Include(s => s.Driver)
            .FirstOrDefaultAsync(s => s.Id == sessionId && s.TenantId == tenantId, cancellationToken);

        if (session is null)
            throw new KeyNotFoundException($"Vehicle usage session '{sessionId}' was not found.");

        session.Cancel(userId, reason);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(
            new CreateAuditLogRequest(
                AuditAction.VehicleUsageSessionCancelled,
                "VehicleUsageSession",
                session.Id.ToString(),
                $"Cancelled usage session {sessionId}: {reason}"),
            cancellationToken);
    }

    private static VehicleUsageSessionDto MapToDto(VehicleUsageSession s) =>
        new(
            s.Id,
            s.TenantId,
            s.VehicleId,
            s.Vehicle?.VehicleNumber ?? string.Empty,
            s.Vehicle?.DisplayName ?? string.Empty,
            s.DriverId,
            s.Driver?.DriverNumber ?? string.Empty,
            s.Driver?.DisplayName ?? string.Empty,
            s.DriverVehicleAssignmentId,
            s.FleetShiftAssignmentId,
            s.CheckedOutAtUtc,
            s.CheckedOutByUserId,
            s.StartOdometer,
            s.OdometerUnit,
            s.StartLocationId,
            s.Purpose,
            s.Reference,
            s.Status,
            s.Status.ToString(),
            s.CheckedInAtUtc,
            s.CheckedInByUserId,
            s.EndOdometer,
            s.DistanceTraveled,
            s.EndLocationId,
            s.CheckoutCondition,
            s.CheckInCondition,
            s.Notes,
            s.CreatedAtUtc);

    private Guid GetCurrentTenantId()
    {
        if (_currentUserContext.TenantId is not Guid tenantId)
            throw new UnauthorizedAccessException("Tenant ID is missing.");
        return tenantId;
    }
}
