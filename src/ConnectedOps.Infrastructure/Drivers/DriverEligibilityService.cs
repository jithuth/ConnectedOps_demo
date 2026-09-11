using ConnectedOps.Application.Common.Interfaces;
using ConnectedOps.Application.Drivers;
using ConnectedOps.Domain.Drivers;
using ConnectedOps.Domain.Vehicles;
using ConnectedOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ConnectedOps.Infrastructure.Drivers;

public sealed class DriverEligibilityService : IDriverEligibilityService
{
    private readonly ConnectedOpsDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;

    public DriverEligibilityService(
        ConnectedOpsDbContext dbContext,
        ICurrentUserContext currentUserContext)
    {
        _dbContext = dbContext;
        _currentUserContext = currentUserContext;
    }

    public async Task<DriverEligibilityResult> EvaluateAsync(
        Guid driverId,
        Guid vehicleId,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();
        var reasons = new List<string>();

        var driver = await _dbContext.Drivers
            .AsNoTracking()
            .Include(d => d.Licenses)
                .ThenInclude(l => l.Categories)
            .Include(d => d.VehicleAssignments)
            .FirstOrDefaultAsync(d => d.Id == driverId && d.TenantId == tenantId, cancellationToken);

        if (driver is null)
        {
            reasons.Add("Driver was not found in the current tenant.");
            return new DriverEligibilityResult(false, reasons, driverId, "Unknown Driver", vehicleId, "Unknown Vehicle");
        }

        var vehicle = await _dbContext.Vehicles
            .AsNoTracking()
            .FirstOrDefaultAsync(v => v.Id == vehicleId && v.TenantId == tenantId, cancellationToken);

        if (vehicle is null)
        {
            reasons.Add("Vehicle was not found in the current tenant.");
            return new DriverEligibilityResult(false, reasons, driverId, driver.DisplayName, vehicleId, "Unknown Vehicle");
        }

        // 1. Driver operational status
        if (driver.Status != DriverStatus.Active)
        {
            reasons.Add($"Driver is currently {driver.Status} and not operational.");
        }

        // 2. Vehicle operational status
        if (vehicle.Status != VehicleStatus.Active && vehicle.Status != VehicleStatus.InService)
        {
            reasons.Add($"Vehicle is currently {vehicle.Status} and cannot be assigned for operations.");
        }

        // 3. Driver Primary License check
        var primaryLicense = driver.Licenses.FirstOrDefault(l => l.IsPrimary && l.IsActive)
            ?? driver.Licenses.FirstOrDefault(l => l.IsActive);

        if (primaryLicense is null)
        {
            reasons.Add("Driver does not have an active driver's license on file.");
        }
        else if (primaryLicense.IsExpired())
        {
            reasons.Add($"Driver's license ({primaryLicense.LicenseNumber}) expired on {primaryLicense.ExpiryDate:yyyy-MM-dd}.");
        }

        var now = DateTime.UtcNow;

        // 4. Check if vehicle already has an active Primary driver
        var vehicleAlreadyAssigned = await _dbContext.DriverVehicleAssignments
            .AnyAsync(a => a.TenantId == tenantId &&
                           a.VehicleId == vehicleId &&
                           a.DriverId != driverId &&
                           a.IsActive &&
                           a.IsPrimary &&
                           a.AssignedFromUtc <= now &&
                           (!a.AssignedToUtc.HasValue || a.AssignedToUtc.Value > now),
                      cancellationToken);

        if (vehicleAlreadyAssigned)
        {
            reasons.Add($"Vehicle {vehicle.VehicleNumber} already has an active primary driver assigned.");
        }

        // 5. Check if driver already has an active Primary vehicle
        var driverAlreadyAssigned = await _dbContext.DriverVehicleAssignments
            .AnyAsync(a => a.TenantId == tenantId &&
                           a.DriverId == driverId &&
                           a.VehicleId != vehicleId &&
                           a.IsActive &&
                           a.IsPrimary &&
                           a.AssignedFromUtc <= now &&
                           (!a.AssignedToUtc.HasValue || a.AssignedToUtc.Value > now),
                      cancellationToken);

        if (driverAlreadyAssigned)
        {
            reasons.Add($"Driver {driver.DisplayName} is already actively assigned as primary operator for another vehicle.");
        }

        return new DriverEligibilityResult(
            reasons.Count == 0,
            reasons,
            driver.Id,
            driver.DisplayName,
            vehicle.Id,
            vehicle.VehicleNumber);
    }

    private Guid GetCurrentTenantId()
    {
        if (_currentUserContext.TenantId is not Guid tenantId)
            throw new UnauthorizedAccessException("Tenant ID is missing.");
        return tenantId;
    }
}
