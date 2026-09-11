using ConnectedOps.Application.Common.Interfaces;
using ConnectedOps.Application.Maintenance;
using ConnectedOps.Domain.Maintenance;
using ConnectedOps.Domain.Vehicles;
using ConnectedOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ConnectedOps.Infrastructure.Maintenance;

public sealed class MaintenanceDashboardService : IMaintenanceDashboardService
{
    private readonly ConnectedOpsDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IMaintenanceScheduleService _scheduleService;

    public MaintenanceDashboardService(
        ConnectedOpsDbContext dbContext,
        ICurrentUserContext currentUserContext,
        IMaintenanceScheduleService scheduleService)
    {
        _dbContext = dbContext;
        _currentUserContext = currentUserContext;
        _scheduleService = scheduleService;
    }

    public async Task<MaintenanceDashboardDto> GetDashboardMetricsAsync(
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();
        var now = DateTime.UtcNow;
        var startOfToday = new DateTime(now.Year, now.Month, now.Day, 0, 0, 0, DateTimeKind.Utc);
        var startOfMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var startOfYear = new DateTime(now.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        // Vehicles counts
        var totalVehicles = await _dbContext.Vehicles
            .AsNoTracking()
            .CountAsync(x => x.TenantId == tenantId && !x.IsDeleted, cancellationToken);

        var underMaintenanceVehicles = await _dbContext.Vehicles
            .AsNoTracking()
            .CountAsync(x => x.TenantId == tenantId && x.Status == VehicleStatus.UnderMaintenance && !x.IsDeleted, cancellationToken);

        // Due maintenance calculations
        var dueItems = await _scheduleService.CalculateAllVehiclesMaintenanceAsync(cancellationToken: cancellationToken);

        var upcomingVehicleIds = dueItems.Where(x => x.DueStatus == MaintenanceDueStatus.Upcoming).Select(x => x.VehicleId).Distinct().Count();
        var dueVehicleIds = dueItems.Where(x => x.DueStatus == MaintenanceDueStatus.Due).Select(x => x.VehicleId).Distinct().Count();
        var overdueVehicleIds = dueItems.Where(x => x.DueStatus == MaintenanceDueStatus.Overdue).Select(x => x.VehicleId).Distinct().Count();

        // Service completion counts & costs
        var completedRecordsQuery = _dbContext.VehicleMaintenanceRecords
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.Status == MaintenanceRecordStatus.Completed);

        var completedToday = await completedRecordsQuery
            .CountAsync(x => (x.CompletedDateTimeUtc ?? x.ServiceDateUtc) >= startOfToday, cancellationToken);

        var completedThisMonthRecords = await completedRecordsQuery
            .Where(x => (x.CompletedDateTimeUtc ?? x.ServiceDateUtc) >= startOfMonth)
            .ToListAsync(cancellationToken);

        var completedThisMonth = completedThisMonthRecords.Count;
        var costThisMonth = completedThisMonthRecords.Sum(x => x.TotalCost ?? 0m);

        var costThisYear = await completedRecordsQuery
            .Where(x => (x.CompletedDateTimeUtc ?? x.ServiceDateUtc) >= startOfYear)
            .SumAsync(x => x.TotalCost ?? 0m, cancellationToken);

        // Average downtime
        var recordsWithDowntime = await completedRecordsQuery
            .Where(x => x.VehicleDowntimeMinutes.HasValue && x.VehicleDowntimeMinutes.Value > 0)
            .Select(x => x.VehicleDowntimeMinutes!.Value)
            .ToListAsync(cancellationToken);

        decimal avgDowntimeHours = recordsWithDowntime.Count > 0
            ? Math.Round((decimal)recordsWithDowntime.Average() / 60m, 1)
            : 0m;

        // Upcoming by service type
        var upcomingByServiceType = dueItems
            .Where(x => x.DueStatus is MaintenanceDueStatus.Upcoming or MaintenanceDueStatus.Due)
            .GroupBy(x => new { x.ServiceTypeId, x.ServiceTypeName, x.ServiceCategoryName })
            .Select(g => new MaintenanceServiceTypeCountDto(g.Key.ServiceTypeId, g.Key.ServiceTypeName, g.Key.ServiceCategoryName, g.Count()))
            .OrderByDescending(x => x.Count)
            .Take(6)
            .ToList();

        // Overdue by branch
        var overdueByBranch = dueItems
            .Where(x => x.DueStatus == MaintenanceDueStatus.Overdue)
            .GroupBy(x => new { x.BranchId, BranchName = x.BranchName ?? "Unassigned" })
            .Select(g => new MaintenanceBranchCountDto(g.Key.BranchId, g.Key.BranchName, g.Count()))
            .OrderByDescending(x => x.Count)
            .Take(6)
            .ToList();

        // Recent completed maintenance
        var recentCompleted = await _dbContext.VehicleMaintenanceRecords
            .AsNoTracking()
            .Include(x => x.Vehicle)
            .Include(x => x.MaintenanceServiceType)
            .Include(x => x.MaintenanceProvider)
            .Where(x => x.TenantId == tenantId && x.Status == MaintenanceRecordStatus.Completed)
            .OrderByDescending(x => x.CompletedDateTimeUtc ?? x.ServiceDateUtc)
            .Take(6)
            .Select(x => new RecentMaintenanceRecordDto(
                x.Id,
                x.VehicleId,
                x.Vehicle.VehicleNumber,
                x.Vehicle.RegistrationNumber,
                x.MaintenanceServiceType.Name,
                x.CompletedDateTimeUtc ?? x.ServiceDateUtc,
                x.TotalCost ?? 0m,
                x.CurrencyCode ?? "USD",
                x.MaintenanceProvider != null ? x.MaintenanceProvider.Name : null))
            .ToListAsync(cancellationToken);

        // Highest maintenance cost vehicles
        var completedRecords = await _dbContext.VehicleMaintenanceRecords
            .AsNoTracking()
            .Include(x => x.Vehicle)
                .ThenInclude(v => v.VehicleCategory)
            .Where(x => x.TenantId == tenantId && x.Status == MaintenanceRecordStatus.Completed)
            .ToListAsync(cancellationToken);

        var topCostVehicles = completedRecords
            .GroupBy(x => new
            {
                x.VehicleId,
                VehicleNumber = x.Vehicle?.VehicleNumber ?? string.Empty,
                RegistrationNumber = x.Vehicle?.RegistrationNumber,
                DisplayName = x.Vehicle?.DisplayName ?? string.Empty,
                CategoryName = x.Vehicle?.VehicleCategory?.Name ?? "General"
            })
            .Select(g => new TopMaintenanceCostVehicleDto(
                g.Key.VehicleId,
                g.Key.VehicleNumber,
                g.Key.RegistrationNumber,
                g.Key.DisplayName,
                g.Key.CategoryName,
                g.Sum(r => r.TotalCost ?? 0m),
                g.Count()))
            .OrderByDescending(x => x.TotalCost)
            .Take(5)
            .ToList();

        return new MaintenanceDashboardDto(
            totalVehicles,
            upcomingVehicleIds,
            dueVehicleIds,
            overdueVehicleIds,
            underMaintenanceVehicles,
            completedToday,
            completedThisMonth,
            costThisMonth,
            costThisYear,
            avgDowntimeHours,
            upcomingByServiceType,
            overdueByBranch,
            recentCompleted,
            topCostVehicles);
    }

    private Guid GetCurrentTenantId()
    {
        if (_currentUserContext.TenantId is not Guid tenantId)
            throw new UnauthorizedAccessException("Tenant ID is missing.");

        return tenantId;
    }
}
