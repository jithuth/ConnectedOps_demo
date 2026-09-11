using ConnectedOps.Application.Common.Interfaces;
using ConnectedOps.Application.FleetOperations;
using ConnectedOps.Domain.FleetOperations;
using ConnectedOps.Domain.Vehicles;
using ConnectedOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ConnectedOps.Infrastructure.FleetOperations;

public sealed class FleetOperationsDashboardService : IFleetOperationsDashboardService
{
    private readonly ConnectedOpsDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IFleetAvailabilityService _availabilityService;

    public FleetOperationsDashboardService(
        ConnectedOpsDbContext dbContext,
        ICurrentUserContext currentUserContext,
        IFleetAvailabilityService availabilityService)
    {
        _dbContext = dbContext;
        _currentUserContext = currentUserContext;
        _availabilityService = availabilityService;
    }

    public async Task<FleetOperationsDashboardDto> GetDashboardAsync(
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();
        var today = DateTime.UtcNow.Date;

        var availability = await _availabilityService.GetFleetAvailabilityAsync(null, cancellationToken);

        var openSessionsCount = await _dbContext.VehicleUsageSessions
            .CountAsync(s => s.TenantId == tenantId && s.Status == UsageSessionStatus.Open, cancellationToken);

        var checkoutsToday = await _dbContext.VehicleUsageSessions
            .CountAsync(s => s.TenantId == tenantId && s.CheckedOutAtUtc >= today, cancellationToken);

        var checkInsToday = await _dbContext.VehicleUsageSessions
            .CountAsync(s => s.TenantId == tenantId && s.Status == UsageSessionStatus.Completed && s.CheckedInAtUtc >= today, cancellationToken);

        var handoversToday = await _dbContext.VehicleHandovers
            .CountAsync(h => h.TenantId == tenantId && h.HandoverAtUtc >= today, cancellationToken);

        var openExceptionsCount = await _dbContext.FleetOperationalExceptions
            .CountAsync(e => e.TenantId == tenantId && e.Status == OperationalExceptionStatus.Open, cancellationToken);

        var recentCheckouts = await _dbContext.VehicleUsageSessions
            .AsNoTracking()
            .Include(s => s.Vehicle)
            .Include(s => s.Driver)
            .Where(s => s.TenantId == tenantId)
            .OrderByDescending(s => s.CheckedOutAtUtc)
            .Take(5)
            .ToListAsync(cancellationToken);

        var recentCheckIns = await _dbContext.VehicleUsageSessions
            .AsNoTracking()
            .Include(s => s.Vehicle)
            .Include(s => s.Driver)
            .Where(s => s.TenantId == tenantId && s.Status == UsageSessionStatus.Completed && s.CheckedInAtUtc.HasValue)
            .OrderByDescending(s => s.CheckedInAtUtc)
            .Take(5)
            .ToListAsync(cancellationToken);

        var recentHandovers = await _dbContext.VehicleHandovers
            .AsNoTracking()
            .Include(h => h.Vehicle)
            .Include(h => h.FromDriver)
            .Include(h => h.ToDriver)
            .Where(h => h.TenantId == tenantId)
            .OrderByDescending(h => h.HandoverAtUtc)
            .Take(5)
            .ToListAsync(cancellationToken);

        var openExceptions = await _dbContext.FleetOperationalExceptions
            .AsNoTracking()
            .Include(e => e.Vehicle)
            .Include(e => e.Driver)
            .Where(e => e.TenantId == tenantId && e.Status == OperationalExceptionStatus.Open)
            .OrderByDescending(e => e.OccurredAtUtc)
            .Take(5)
            .ToListAsync(cancellationToken);

        // Groupings for charts
        var vehiclesByStatus = availability.Vehicles
            .GroupBy(v => v.AvailabilityStatusName)
            .ToDictionary(g => g.Key, g => g.Count());

        var driversByStatus = availability.Drivers
            .GroupBy(d => d.AvailabilityStatusName)
            .ToDictionary(g => g.Key, g => g.Count());

        var operationsByBranch = availability.Vehicles
            .GroupBy(v => string.IsNullOrWhiteSpace(v.BranchName) ? "Unassigned" : v.BranchName)
            .ToDictionary(g => g.Key, g => g.Count());

        return new FleetOperationsDashboardDto(
            availability.TotalVehicles,
            availability.AvailableVehicles,
            availability.CheckedOutVehicles,
            availability.AssignedVehicles,
            availability.UnavailableVehicles,
            availability.TotalDrivers,
            availability.AvailableDrivers,
            availability.AssignedDrivers,
            availability.UnavailableDrivers,
            openSessionsCount,
            checkInsToday,
            checkoutsToday,
            checkInsToday,
            handoversToday,
            openExceptionsCount,
            vehiclesByStatus,
            driversByStatus,
            operationsByBranch,
            recentCheckouts.Select(MapSessionToDto).ToList(),
            recentCheckIns.Select(MapSessionToDto).ToList(),
            recentHandovers.Select(MapHandoverToDto).ToList(),
            openExceptions.Select(MapExceptionToDto).ToList());
    }

    public async Task<FleetOperationsBoardDto> GetBoardAsync(
        FleetAvailabilityFilter? filter = null,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();

        var availability = await _availabilityService.GetFleetAvailabilityAsync(filter, cancellationToken);

        var vehicleIds = availability.Vehicles.Select(v => v.VehicleId).ToList();

        var openExceptionsVehicleIds = await _dbContext.FleetOperationalExceptions
            .Where(e => e.TenantId == tenantId && e.Status == OperationalExceptionStatus.Open && e.VehicleId.HasValue && vehicleIds.Contains(e.VehicleId.Value))
            .Select(e => e.VehicleId!.Value)
            .Distinct()
            .ToListAsync(cancellationToken);

        var openExceptionsSet = new HashSet<Guid>(openExceptionsVehicleIds);

        var vehicles = await _dbContext.Vehicles
            .AsNoTracking()
            .Include(v => v.VehicleMake)
            .Include(v => v.VehicleModel)
            .Where(v => v.TenantId == tenantId && vehicleIds.Contains(v.Id))
            .ToDictionaryAsync(v => v.Id, cancellationToken);

        var boardItems = new List<FleetOperationsBoardItemDto>();
        foreach (var v in availability.Vehicles)
        {
            vehicles.TryGetValue(v.VehicleId, out var vehEntity);
            var makeModel = vehEntity is not null
                ? $"{vehEntity.VehicleMake?.Name} {vehEntity.VehicleModel?.Name}".Trim()
                : null;

            boardItems.Add(new FleetOperationsBoardItemDto(
                v.VehicleId,
                v.VehicleNumber,
                v.RegistrationNumber,
                v.DisplayName,
                string.IsNullOrWhiteSpace(makeModel) ? null : makeModel,
                v.BranchName,
                v.AvailabilityStatus,
                v.AvailabilityStatusName,
                v.LifecycleStatus.ToString(),
                v.CurrentDriverId,
                null,
                v.CurrentDriverName,
                v.CurrentShiftId,
                v.CurrentShiftName,
                v.CurrentSessionId,
                v.CheckedOutAtUtc,
                v.CurrentOdometer,
                openExceptionsSet.Contains(v.VehicleId)));
        }

        var availableCount = boardItems.Count(i => i.AvailabilityStatus == VehicleAvailabilityStatus.Available);
        var checkedOutCount = boardItems.Count(i => i.AvailabilityStatus == VehicleAvailabilityStatus.CheckedOut);
        var assignedCount = boardItems.Count(i => i.AvailabilityStatus == VehicleAvailabilityStatus.Assigned);
        var unavailableCount = boardItems.Count(i => i.AvailabilityStatus == VehicleAvailabilityStatus.Unavailable);
        var availableDriversCount = availability.AvailableDrivers;
        var openExceptionsCount = openExceptionsSet.Count;

        return new FleetOperationsBoardDto(
            availableCount,
            checkedOutCount,
            assignedCount,
            unavailableCount,
            availableDriversCount,
            openExceptionsCount,
            boardItems);
    }

    private static VehicleUsageSessionDto MapSessionToDto(VehicleUsageSession s) =>
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

    private static VehicleHandoverDto MapHandoverToDto(VehicleHandover h) =>
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

    private static FleetOperationalExceptionDto MapExceptionToDto(FleetOperationalException e) =>
        new(
            e.Id,
            e.TenantId,
            e.VehicleId,
            e.Vehicle?.VehicleNumber,
            e.Vehicle?.DisplayName,
            e.DriverId,
            e.Driver?.DriverNumber,
            e.Driver?.DisplayName,
            e.UsageSessionId,
            e.ExceptionType,
            e.ExceptionType.ToString(),
            e.Severity,
            e.Severity.ToString(),
            e.Description,
            e.OccurredAtUtc,
            e.ResolvedAtUtc,
            e.ResolvedByUserId,
            null,
            e.ResolutionNotes,
            e.Status,
            e.Status.ToString(),
            e.CreatedAtUtc);

    private Guid GetCurrentTenantId()
    {
        if (_currentUserContext.TenantId is not Guid tenantId)
            throw new UnauthorizedAccessException("Tenant ID is missing.");
        return tenantId;
    }
}
