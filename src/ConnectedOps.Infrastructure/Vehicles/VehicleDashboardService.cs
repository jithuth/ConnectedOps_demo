using ConnectedOps.Application.Common.Interfaces;
using ConnectedOps.Application.Vehicles;
using ConnectedOps.Domain.Vehicles;
using ConnectedOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ConnectedOps.Infrastructure.Vehicles;

public sealed class VehicleDashboardService : IVehicleDashboardService
{
    private readonly ConnectedOpsDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IVehicleDocumentService _vehicleDocumentService;

    public VehicleDashboardService(
        ConnectedOpsDbContext dbContext,
        ICurrentUserContext currentUserContext,
        IVehicleDocumentService vehicleDocumentService)
    {
        _dbContext = dbContext;
        _currentUserContext = currentUserContext;
        _vehicleDocumentService = vehicleDocumentService;
    }

    public async Task<VehicleDashboardStatsDto> GetDashboardStatsAsync(
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var thirtyDaysFromNow = today.AddDays(30);

        var vehiclesQuery = _dbContext.Vehicles
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId);

        var totalVehicles = await vehiclesQuery.CountAsync(cancellationToken);
        var activeVehicles = await vehiclesQuery.CountAsync(x => x.IsActive, cancellationToken);
        var inServiceVehicles = await vehiclesQuery.CountAsync(x => x.Status == VehicleStatus.InService, cancellationToken);
        var outOfServiceVehicles = await vehiclesQuery.CountAsync(x => x.Status == VehicleStatus.OutOfService, cancellationToken);
        var underMaintenanceVehicles = await vehiclesQuery.CountAsync(x => x.Status == VehicleStatus.UnderMaintenance, cancellationToken);
        var reservedVehicles = await vehiclesQuery.CountAsync(x => x.Status == VehicleStatus.Reserved, cancellationToken);
        var inactiveVehicles = await vehiclesQuery.CountAsync(x => x.Status == VehicleStatus.Inactive || x.Status == VehicleStatus.Retired || x.Status == VehicleStatus.Sold || x.Status == VehicleStatus.Scrapped, cancellationToken);

        var totalOdometerKm = await vehiclesQuery.SumAsync(x => (decimal?)x.CurrentOdometer, cancellationToken) ?? 0m;

        var expiringDocumentsCount = await _dbContext.VehicleDocuments
            .AsNoTracking()
            .CountAsync(d => d.TenantId == tenantId && d.ExpiryDate != null && d.ExpiryDate <= thirtyDaysFromNow, cancellationToken);

        var byCategoryData = await vehiclesQuery
            .GroupBy(x => x.VehicleCategory.Name)
            .Select(g => new { Category = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Category, x => x.Count, cancellationToken);

        var byBranchData = await vehiclesQuery
            .Where(x => x.Branch != null)
            .GroupBy(x => x.Branch!.Name)
            .Select(g => new { Branch = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Branch, x => x.Count, cancellationToken);

        var byFuelTypeData = await vehiclesQuery
            .GroupBy(x => x.FuelType)
            .Select(g => new { FuelType = g.Key.ToString(), Count = g.Count() })
            .ToDictionaryAsync(x => x.FuelType, x => x.Count, cancellationToken);

        var byOwnershipTypeData = await vehiclesQuery
            .GroupBy(x => x.OwnershipType)
            .Select(g => new { OwnershipType = g.Key.ToString(), Count = g.Count() })
            .ToDictionaryAsync(x => x.OwnershipType, x => x.Count, cancellationToken);

        var recentVehiclesList = await vehiclesQuery
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(5)
            .Select(x => new
            {
                Vehicle = x,
                CategoryName = x.VehicleCategory.Name,
                MakeName = x.VehicleMake.Name,
                ModelName = x.VehicleModel.Name,
                BranchName = x.Branch != null ? x.Branch.Name : null,
                LocationName = x.Location != null ? x.Location.Name : null,
                DocCount = x.Documents.Count,
                ExpiringDocCount = x.Documents.Count(d => d.ExpiryDate != null && d.ExpiryDate <= thirtyDaysFromNow)
            })
            .ToListAsync(cancellationToken);

        var recentVehicles = recentVehiclesList.Select(x => new VehicleListItemDto(
            x.Vehicle.Id,
            x.Vehicle.VehicleNumber,
            x.Vehicle.DisplayName,
            x.Vehicle.RegistrationNumber,
            x.Vehicle.VIN,
            x.Vehicle.VehicleCategoryId,
            x.CategoryName,
            x.Vehicle.VehicleMakeId,
            x.MakeName,
            x.Vehicle.VehicleModelId,
            x.ModelName,
            x.Vehicle.ModelYear,
            x.Vehicle.FuelType,
            x.Vehicle.FuelType.ToString(),
            x.Vehicle.TransmissionType,
            x.Vehicle.TransmissionType.ToString(),
            x.Vehicle.OwnershipType,
            x.Vehicle.OwnershipType.ToString(),
            x.Vehicle.Status,
            x.Vehicle.Status.ToString(),
            x.Vehicle.BranchId,
            x.BranchName,
            x.Vehicle.LocationId,
            x.LocationName,
            x.Vehicle.CurrentOdometer,
            x.Vehicle.OdometerUnit,
            x.Vehicle.IsActive,
            x.Vehicle.PrimaryImageObjectKey,
            x.Vehicle.CreatedAtUtc,
            x.DocCount,
            x.ExpiringDocCount)).ToList();

        var criticalAlerts = await _vehicleDocumentService.GetExpiringDocumentsAsync(30, cancellationToken);

        return new VehicleDashboardStatsDto(
            totalVehicles,
            activeVehicles,
            inServiceVehicles,
            outOfServiceVehicles,
            underMaintenanceVehicles,
            reservedVehicles,
            inactiveVehicles,
            totalOdometerKm,
            expiringDocumentsCount,
            byCategoryData,
            byBranchData,
            byFuelTypeData,
            byOwnershipTypeData,
            recentVehicles,
            criticalAlerts);
    }

    private Guid GetCurrentTenantId()
    {
        if (_currentUserContext.TenantId is not Guid tenantId)
            throw new UnauthorizedAccessException("Tenant ID is missing.");

        return tenantId;
    }
}
