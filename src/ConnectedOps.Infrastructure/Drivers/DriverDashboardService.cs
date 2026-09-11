using ConnectedOps.Application.Common.Interfaces;
using ConnectedOps.Application.Drivers;
using ConnectedOps.Domain.Drivers;
using ConnectedOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ConnectedOps.Infrastructure.Drivers;

public sealed class DriverDashboardService : IDriverDashboardService
{
    private readonly ConnectedOpsDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;

    public DriverDashboardService(
        ConnectedOpsDbContext dbContext,
        ICurrentUserContext currentUserContext)
    {
        _dbContext = dbContext;
        _currentUserContext = currentUserContext;
    }

    public async Task<DriverDashboardSummaryDto> GetDashboardSummaryAsync(
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();
        var now = DateTime.UtcNow;
        var today = DateOnly.FromDateTime(now);
        var thirtyDaysOut = today.AddDays(30);

        var drivers = await _dbContext.Drivers
            .AsNoTracking()
            .Where(d => d.TenantId == tenantId)
            .Include(d => d.Branch)
            .Include(d => d.Department)
            .Include(d => d.Licenses)
            .Include(d => d.Certifications)
            .Include(d => d.Documents)
            .Include(d => d.VehicleAssignments)
                .ThenInclude(a => a.Vehicle)
            .ToListAsync(cancellationToken);

        var totalDrivers = drivers.Count;
        var activeDrivers = drivers.Count(d => d.Status == DriverStatus.Active);
        var onLeaveDrivers = drivers.Count(d => d.Status == DriverStatus.OnLeave);
        var suspendedDrivers = drivers.Count(d => d.Status == DriverStatus.Suspended);
        var inactiveDrivers = drivers.Count(d => d.Status == DriverStatus.Inactive || d.Status == DriverStatus.Terminated || d.Status == DriverStatus.Retired);

        var assignedDrivers = drivers.Count(d =>
            d.Status == DriverStatus.Active &&
            d.VehicleAssignments.Any(a => a.IsActive && a.IsPrimary && a.AssignedFromUtc <= now && (!a.AssignedToUtc.HasValue || a.AssignedToUtc.Value > now)));

        var availableDrivers = activeDrivers - assignedDrivers;
        if (availableDrivers < 0) availableDrivers = 0;

        // Licenses metrics
        var allLicenses = drivers.SelectMany(d => d.Licenses.Where(l => l.IsActive && !l.IsDeleted)).ToList();
        var expiredLicenses = allLicenses.Count(l => l.ExpiryDate.HasValue && l.ExpiryDate.Value < today);
        var licensesExpiringSoon = allLicenses.Count(l => l.ExpiryDate.HasValue && l.ExpiryDate.Value >= today && l.ExpiryDate.Value <= thirtyDaysOut);

        // Documents metrics
        var allDocs = drivers.SelectMany(d => d.Documents.Where(doc => doc.IsActive && !doc.IsDeleted)).ToList();
        var expiredDocs = allDocs.Count(doc => doc.ExpiryDate.HasValue && doc.ExpiryDate.Value < today);
        var docsExpiringSoon = allDocs.Count(doc => doc.ExpiryDate.HasValue && doc.ExpiryDate.Value >= today && doc.ExpiryDate.Value <= thirtyDaysOut);

        // Certifications metrics
        var allCerts = drivers.SelectMany(d => d.Certifications.Where(c => c.IsActive && !c.IsDeleted)).ToList();
        var expiredCerts = allCerts.Count(c => c.ExpiryDate.HasValue && c.ExpiryDate.Value < today);
        var certsExpiringSoon = allCerts.Count(c => c.ExpiryDate.HasValue && c.ExpiryDate.Value >= today && c.ExpiryDate.Value <= thirtyDaysOut);

        // Branch Distribution
        var driversByBranch = drivers
            .GroupBy(d => new { d.BranchId, BranchName = d.Branch != null ? d.Branch.Name : "Unassigned" })
            .Select(g => new DriverBranchMetricDto(g.Key.BranchId, g.Key.BranchName, g.Count()))
            .OrderByDescending(b => b.DriverCount)
            .ToList();

        // Type Distribution
        var driversByType = drivers
            .GroupBy(d => d.DriverType)
            .Select(g => new DriverTypeMetricDto(g.Key, g.Key.ToString(), g.Count()))
            .OrderByDescending(t => t.DriverCount)
            .ToList();

        // Status Distribution
        var driversByStatus = drivers
            .GroupBy(d => d.Status)
            .Select(g => new DriverStatusMetricDto(g.Key, g.Key.ToString(), g.Count()))
            .OrderByDescending(s => s.DriverCount)
            .ToList();

        // Recent Drivers (Top 5)
        var recentDrivers = drivers
            .OrderByDescending(d => d.CreatedAtUtc)
            .Take(5)
            .Select(d =>
            {
                var primaryLic = d.Licenses.FirstOrDefault(l => l.IsPrimary && l.IsActive) ?? d.Licenses.FirstOrDefault(l => l.IsActive);
                var isExpired = primaryLic != null && primaryLic.ExpiryDate.HasValue && primaryLic.ExpiryDate.Value < today;
                var isExpSoon = primaryLic != null && primaryLic.ExpiryDate.HasValue && !isExpired && primaryLic.ExpiryDate.Value <= thirtyDaysOut;

                var activeAssn = d.VehicleAssignments
                    .Where(a => a.IsActive && a.AssignedFromUtc <= now && (!a.AssignedToUtc.HasValue || a.AssignedToUtc.Value > now))
                    .OrderByDescending(a => a.IsPrimary)
                    .FirstOrDefault();

                var avail = d.Status != DriverStatus.Active
                    ? DriverAvailabilityStatus.Unavailable
                    : (activeAssn != null ? DriverAvailabilityStatus.Assigned : DriverAvailabilityStatus.Available);

                return new DriverListItemDto(
                    d.Id,
                    d.DriverNumber,
                    d.DisplayName,
                    d.FirstName,
                    d.LastName,
                    d.Phone,
                    d.Email,
                    d.DriverType,
                    d.DriverType.ToString(),
                    d.Status,
                    d.Status.ToString(),
                    avail,
                    avail.ToString(),
                    d.BranchId,
                    d.Branch?.Name,
                    d.DepartmentId,
                    d.Department?.Name,
                    primaryLic?.LicenseNumber ?? d.PrimaryLicenseNumber,
                    primaryLic?.ExpiryDate,
                    isExpired,
                    isExpSoon,
                    activeAssn?.VehicleId,
                    activeAssn?.Vehicle?.VehicleNumber,
                    activeAssn?.Vehicle?.DisplayName,
                    d.IsActive,
                    d.CreatedAtUtc,
                    d.Documents.Count,
                    d.Documents.Count(x => x.ExpiryDate.HasValue && x.ExpiryDate.Value <= thirtyDaysOut),
                    d.Certifications.Count,
                    d.Certifications.Count(x => x.ExpiryDate.HasValue && x.ExpiryDate.Value <= thirtyDaysOut));
            })
            .ToList();

        // Upcoming Expiries
        var expiries = new List<DriverExpiringItemDto>();

        foreach (var d in drivers)
        {
            foreach (var l in d.Licenses.Where(l => l.IsActive && l.ExpiryDate.HasValue && l.ExpiryDate.Value <= thirtyDaysOut))
            {
                var days = l.ExpiryDate!.Value.DayNumber - today.DayNumber;
                expiries.Add(new DriverExpiringItemDto(
                    d.Id, d.DriverNumber, d.DisplayName, "Driver License", l.LicenseNumber, l.ExpiryDate.Value, days, days < 0));
            }

            foreach (var doc in d.Documents.Where(doc => doc.IsActive && doc.ExpiryDate.HasValue && doc.ExpiryDate.Value <= thirtyDaysOut))
            {
                var days = doc.ExpiryDate!.Value.DayNumber - today.DayNumber;
                expiries.Add(new DriverExpiringItemDto(
                    d.Id, d.DriverNumber, d.DisplayName, doc.DocumentType.ToString(), doc.Title, doc.ExpiryDate.Value, days, days < 0));
            }

            foreach (var c in d.Certifications.Where(c => c.IsActive && c.ExpiryDate.HasValue && c.ExpiryDate.Value <= thirtyDaysOut))
            {
                var days = c.ExpiryDate!.Value.DayNumber - today.DayNumber;
                expiries.Add(new DriverExpiringItemDto(
                    d.Id, d.DriverNumber, d.DisplayName, "Certification", c.Title, c.ExpiryDate.Value, days, days < 0));
            }
        }

        var sortedExpiries = expiries
            .OrderBy(x => x.DaysRemaining)
            .Take(15)
            .ToList();

        return new DriverDashboardSummaryDto(
            totalDrivers,
            activeDrivers,
            availableDrivers,
            assignedDrivers,
            onLeaveDrivers,
            suspendedDrivers,
            inactiveDrivers,
            expiredLicenses,
            licensesExpiringSoon,
            expiredDocs,
            docsExpiringSoon,
            expiredCerts,
            certsExpiringSoon,
            driversByBranch,
            driversByType,
            driversByStatus,
            recentDrivers,
            sortedExpiries);
    }

    private Guid GetCurrentTenantId()
    {
        if (_currentUserContext.TenantId is not Guid tenantId)
            throw new UnauthorizedAccessException("Tenant ID is missing.");
        return tenantId;
    }
}
