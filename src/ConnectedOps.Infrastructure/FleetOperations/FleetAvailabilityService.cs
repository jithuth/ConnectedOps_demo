using ConnectedOps.Application.Common.Interfaces;
using ConnectedOps.Application.FleetOperations;
using ConnectedOps.Domain.Drivers;
using ConnectedOps.Domain.FleetOperations;
using ConnectedOps.Domain.Vehicles;
using ConnectedOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ConnectedOps.Infrastructure.FleetOperations;

public sealed class FleetAvailabilityService : IFleetAvailabilityService
{
    private readonly ConnectedOpsDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;

    public FleetAvailabilityService(
        ConnectedOpsDbContext dbContext,
        ICurrentUserContext currentUserContext)
    {
        _dbContext = dbContext;
        _currentUserContext = currentUserContext;
    }

    public async Task<VehicleAvailabilityDto> GetVehicleAvailabilityAsync(
        Guid vehicleId,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();

        var vehicle = await _dbContext.Vehicles
            .AsNoTracking()
            .Include(v => v.Branch)
            .FirstOrDefaultAsync(v => v.Id == vehicleId && v.TenantId == tenantId, cancellationToken);

        if (vehicle is null)
            throw new KeyNotFoundException($"Vehicle '{vehicleId}' was not found.");

        var openSession = await _dbContext.VehicleUsageSessions
            .AsNoTracking()
            .Include(s => s.Driver)
            .FirstOrDefaultAsync(s => s.TenantId == tenantId && s.VehicleId == vehicleId && s.Status == UsageSessionStatus.Open, cancellationToken);

        var activeAssignment = await _dbContext.DriverVehicleAssignments
            .AsNoTracking()
            .Include(a => a.Driver)
            .FirstOrDefaultAsync(a => a.TenantId == tenantId && a.VehicleId == vehicleId && a.IsActive, cancellationToken);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var todayShiftAssignment = await _dbContext.FleetShiftAssignments
            .AsNoTracking()
            .Include(sa => sa.FleetShift)
            .Include(sa => sa.Driver)
            .FirstOrDefaultAsync(sa => sa.TenantId == tenantId && sa.VehicleId == vehicleId && sa.AssignmentDate == today && sa.AssignmentStatus != ShiftAssignmentStatus.Cancelled, cancellationToken);

        return ComputeVehicleAvailability(vehicle, openSession, activeAssignment, todayShiftAssignment);
    }

    public async Task<DriverAvailabilityResultDto> GetDriverAvailabilityAsync(
        Guid driverId,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();

        var driver = await _dbContext.Drivers
            .AsNoTracking()
            .Include(d => d.Branch)
            .FirstOrDefaultAsync(d => d.Id == driverId && d.TenantId == tenantId, cancellationToken);

        if (driver is null)
            throw new KeyNotFoundException($"Driver '{driverId}' was not found.");

        var openSession = await _dbContext.VehicleUsageSessions
            .AsNoTracking()
            .Include(s => s.Vehicle)
            .FirstOrDefaultAsync(s => s.TenantId == tenantId && s.DriverId == driverId && s.Status == UsageSessionStatus.Open, cancellationToken);

        var activeAssignment = await _dbContext.DriverVehicleAssignments
            .AsNoTracking()
            .Include(a => a.Vehicle)
            .FirstOrDefaultAsync(a => a.TenantId == tenantId && a.DriverId == driverId && a.IsActive, cancellationToken);

        return ComputeDriverAvailability(driver, openSession, activeAssignment);
    }

    public async Task<FleetAvailabilitySummaryDto> GetFleetAvailabilityAsync(
        FleetAvailabilityFilter? filter = null,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();

        var vehiclesQuery = _dbContext.Vehicles
            .AsNoTracking()
            .Include(v => v.Branch)
            .Where(v => v.TenantId == tenantId);

        if (filter?.BranchId.HasValue == true)
            vehiclesQuery = vehiclesQuery.Where(v => v.BranchId == filter.BranchId.Value);

        var vehicles = await vehiclesQuery.ToListAsync(cancellationToken);

        var driversQuery = _dbContext.Drivers
            .AsNoTracking()
            .Include(d => d.Branch)
            .Where(d => d.TenantId == tenantId);

        if (filter?.BranchId.HasValue == true)
            driversQuery = driversQuery.Where(d => d.BranchId == filter.BranchId.Value);

        var drivers = await driversQuery.ToListAsync(cancellationToken);

        var openSessions = await _dbContext.VehicleUsageSessions
            .AsNoTracking()
            .Include(s => s.Driver)
            .Include(s => s.Vehicle)
            .Where(s => s.TenantId == tenantId && s.Status == UsageSessionStatus.Open)
            .ToListAsync(cancellationToken);

        var activeAssignments = await _dbContext.DriverVehicleAssignments
            .AsNoTracking()
            .Include(a => a.Driver)
            .Include(a => a.Vehicle)
            .Where(a => a.TenantId == tenantId && a.IsActive)
            .ToListAsync(cancellationToken);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var todayShiftAssignments = await _dbContext.FleetShiftAssignments
            .AsNoTracking()
            .Include(sa => sa.FleetShift)
            .Include(sa => sa.Driver)
            .Where(sa => sa.TenantId == tenantId && sa.AssignmentDate == today && sa.AssignmentStatus != ShiftAssignmentStatus.Cancelled)
            .ToListAsync(cancellationToken);

        var openSessionByVehicle = openSessions.ToDictionary(s => s.VehicleId);
        var openSessionByDriver = openSessions.ToDictionary(s => s.DriverId);
        var activeAssignmentByVehicle = activeAssignments.ToDictionary(a => a.VehicleId);
        var activeAssignmentByDriver = activeAssignments.ToDictionary(a => a.DriverId);
        var todayShiftByVehicle = todayShiftAssignments.Where(sa => sa.VehicleId.HasValue).ToDictionary(sa => sa.VehicleId!.Value);

        var vehicleDtos = new List<VehicleAvailabilityDto>();
        foreach (var vehicle in vehicles)
        {
            openSessionByVehicle.TryGetValue(vehicle.Id, out var session);
            activeAssignmentByVehicle.TryGetValue(vehicle.Id, out var assignment);
            todayShiftByVehicle.TryGetValue(vehicle.Id, out var shift);

            var dto = ComputeVehicleAvailability(vehicle, session, assignment, shift);

            if (filter?.VehicleStatus.HasValue == true && dto.AvailabilityStatus != filter.VehicleStatus.Value)
                continue;

            if (!string.IsNullOrWhiteSpace(filter?.SearchTerm))
            {
                var term = filter.SearchTerm.Trim().ToLowerInvariant();
                var match = dto.VehicleNumber.ToLowerInvariant().Contains(term)
                    || dto.DisplayName.ToLowerInvariant().Contains(term)
                    || (dto.RegistrationNumber?.ToLowerInvariant().Contains(term) == true)
                    || (dto.CurrentDriverName?.ToLowerInvariant().Contains(term) == true);

                if (!match) continue;
            }

            vehicleDtos.Add(dto);
        }

        var driverDtos = new List<DriverAvailabilityResultDto>();
        foreach (var driver in drivers)
        {
            openSessionByDriver.TryGetValue(driver.Id, out var session);
            activeAssignmentByDriver.TryGetValue(driver.Id, out var assignment);

            var dto = ComputeDriverAvailability(driver, session, assignment);

            if (filter?.DriverStatus.HasValue == true && dto.AvailabilityStatus != filter.DriverStatus.Value)
                continue;

            if (!string.IsNullOrWhiteSpace(filter?.SearchTerm))
            {
                var term = filter.SearchTerm.Trim().ToLowerInvariant();
                var match = dto.DriverNumber.ToLowerInvariant().Contains(term)
                    || dto.DisplayName.ToLowerInvariant().Contains(term)
                    || (dto.CurrentVehicleNumber?.ToLowerInvariant().Contains(term) == true);

                if (!match) continue;
            }

            driverDtos.Add(dto);
        }

        var totalVehicles = vehicles.Count;
        var availableVehicles = vehicleDtos.Count(v => v.AvailabilityStatus == VehicleAvailabilityStatus.Available);
        var assignedVehicles = vehicleDtos.Count(v => v.AvailabilityStatus == VehicleAvailabilityStatus.Assigned);
        var checkedOutVehicles = vehicleDtos.Count(v => v.AvailabilityStatus == VehicleAvailabilityStatus.CheckedOut);
        var reservedVehicles = vehicleDtos.Count(v => v.AvailabilityStatus == VehicleAvailabilityStatus.Reserved);
        var unavailableVehicles = vehicleDtos.Count(v => v.AvailabilityStatus == VehicleAvailabilityStatus.Unavailable);

        var totalDrivers = drivers.Count;
        var availableDrivers = driverDtos.Count(d => d.AvailabilityStatus == DriverAvailabilityStatus.Available);
        var assignedDrivers = driverDtos.Count(d => d.AvailabilityStatus == DriverAvailabilityStatus.Assigned);
        var unavailableDrivers = driverDtos.Count(d => d.AvailabilityStatus == DriverAvailabilityStatus.Unavailable);

        return new FleetAvailabilitySummaryDto(
            totalVehicles,
            availableVehicles,
            assignedVehicles,
            checkedOutVehicles,
            reservedVehicles,
            unavailableVehicles,
            totalDrivers,
            availableDrivers,
            assignedDrivers,
            unavailableDrivers,
            vehicleDtos,
            driverDtos);
    }

    private static VehicleAvailabilityDto ComputeVehicleAvailability(
        Vehicle vehicle,
        VehicleUsageSession? openSession,
        DriverVehicleAssignment? activeAssignment,
        FleetShiftAssignment? shiftAssignment)
    {
        if (vehicle.Status != VehicleStatus.Active)
        {
            return new VehicleAvailabilityDto(
                vehicle.Id,
                vehicle.VehicleNumber,
                vehicle.DisplayName,
                vehicle.RegistrationNumber,
                vehicle.BranchId,
                vehicle.Branch?.Name,
                vehicle.Status,
                VehicleAvailabilityStatus.Unavailable,
                VehicleAvailabilityStatus.Unavailable.ToString(),
                false,
                $"Vehicle lifecycle status is {vehicle.Status}",
                null,
                null,
                null,
                null,
                null,
                null,
                vehicle.CurrentOdometer);
        }

        if (openSession is not null)
        {
            return new VehicleAvailabilityDto(
                vehicle.Id,
                vehicle.VehicleNumber,
                vehicle.DisplayName,
                vehicle.RegistrationNumber,
                vehicle.BranchId,
                vehicle.Branch?.Name,
                vehicle.Status,
                VehicleAvailabilityStatus.CheckedOut,
                "Checked Out",
                false,
                "Vehicle is currently checked out on an active trip",
                openSession.DriverId,
                openSession.Driver?.DisplayName,
                null,
                null,
                openSession.Id,
                openSession.CheckedOutAtUtc,
                vehicle.CurrentOdometer);
        }

        if (shiftAssignment is not null)
        {
            return new VehicleAvailabilityDto(
                vehicle.Id,
                vehicle.VehicleNumber,
                vehicle.DisplayName,
                vehicle.RegistrationNumber,
                vehicle.BranchId,
                vehicle.Branch?.Name,
                vehicle.Status,
                VehicleAvailabilityStatus.Reserved,
                VehicleAvailabilityStatus.Reserved.ToString(),
                false,
                $"Reserved for shift '{shiftAssignment.FleetShift?.Name}'",
                shiftAssignment.DriverId,
                shiftAssignment.Driver?.DisplayName,
                shiftAssignment.FleetShiftId,
                shiftAssignment.FleetShift?.Name,
                null,
                null,
                vehicle.CurrentOdometer);
        }

        if (activeAssignment is not null)
        {
            return new VehicleAvailabilityDto(
                vehicle.Id,
                vehicle.VehicleNumber,
                vehicle.DisplayName,
                vehicle.RegistrationNumber,
                vehicle.BranchId,
                vehicle.Branch?.Name,
                vehicle.Status,
                VehicleAvailabilityStatus.Assigned,
                VehicleAvailabilityStatus.Assigned.ToString(),
                true,
                $"Assigned to driver {activeAssignment.Driver?.DisplayName}",
                activeAssignment.DriverId,
                activeAssignment.Driver?.DisplayName,
                null,
                null,
                null,
                null,
                vehicle.CurrentOdometer);
        }

        return new VehicleAvailabilityDto(
            vehicle.Id,
            vehicle.VehicleNumber,
            vehicle.DisplayName,
            vehicle.RegistrationNumber,
            vehicle.BranchId,
            vehicle.Branch?.Name,
            vehicle.Status,
            VehicleAvailabilityStatus.Available,
            VehicleAvailabilityStatus.Available.ToString(),
            true,
            "Vehicle is available for operations",
            null,
            null,
            null,
            null,
            null,
            null,
            vehicle.CurrentOdometer);
    }

    private static DriverAvailabilityResultDto ComputeDriverAvailability(
        Driver driver,
        VehicleUsageSession? openSession,
        DriverVehicleAssignment? activeAssignment)
    {
        if (driver.Status != DriverStatus.Active)
        {
            return new DriverAvailabilityResultDto(
                driver.Id,
                driver.DriverNumber,
                driver.DisplayName,
                driver.BranchId,
                driver.Branch?.Name,
                driver.Status,
                DriverAvailabilityStatus.Unavailable,
                DriverAvailabilityStatus.Unavailable.ToString(),
                false,
                $"Driver status is {driver.Status}",
                null,
                null,
                null,
                null);
        }

        if (openSession is not null)
        {
            return new DriverAvailabilityResultDto(
                driver.Id,
                driver.DriverNumber,
                driver.DisplayName,
                driver.BranchId,
                driver.Branch?.Name,
                driver.Status,
                DriverAvailabilityStatus.Assigned,
                "On Trip",
                false,
                "Driver is currently operating a checked-out vehicle",
                openSession.VehicleId,
                openSession.Vehicle?.VehicleNumber,
                openSession.Id,
                openSession.CheckedOutAtUtc);
        }

        if (activeAssignment is not null)
        {
            return new DriverAvailabilityResultDto(
                driver.Id,
                driver.DriverNumber,
                driver.DisplayName,
                driver.BranchId,
                driver.Branch?.Name,
                driver.Status,
                DriverAvailabilityStatus.Assigned,
                DriverAvailabilityStatus.Assigned.ToString(),
                true,
                $"Assigned to vehicle {activeAssignment.Vehicle?.VehicleNumber}",
                activeAssignment.VehicleId,
                activeAssignment.Vehicle?.VehicleNumber,
                null,
                null);
        }

        return new DriverAvailabilityResultDto(
            driver.Id,
            driver.DriverNumber,
            driver.DisplayName,
            driver.BranchId,
            driver.Branch?.Name,
            driver.Status,
            DriverAvailabilityStatus.Available,
            DriverAvailabilityStatus.Available.ToString(),
            true,
            "Driver is available for assignment",
            null,
            null,
            null,
            null);
    }

    private Guid GetCurrentTenantId()
    {
        if (_currentUserContext.TenantId is not Guid tenantId)
            throw new UnauthorizedAccessException("Tenant ID is missing.");
        return tenantId;
    }
}
