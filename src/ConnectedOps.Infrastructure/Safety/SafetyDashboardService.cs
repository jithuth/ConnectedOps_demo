using System.Globalization;
using ConnectedOps.Application.Common.Interfaces;
using ConnectedOps.Application.Safety;
using ConnectedOps.Domain.Safety;
using ConnectedOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ConnectedOps.Infrastructure.Safety;

public sealed class SafetyDashboardService : ISafetyDashboardService
{
    private readonly ConnectedOpsDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly ISafetyScoreService _safetyScoreService;

    public SafetyDashboardService(
        ConnectedOpsDbContext dbContext,
        ICurrentUserContext currentUserContext,
        ISafetyScoreService safetyScoreService)
    {
        _dbContext = dbContext;
        _currentUserContext = currentUserContext;
        _safetyScoreService = safetyScoreService;
    }

    private Guid GetTenantId() =>
        _currentUserContext.TenantId ?? throw new UnauthorizedAccessException("Tenant context is required.");

    public async Task<SafetyDashboardDto> GetDashboardMetricsAsync(CancellationToken cancellationToken = default)
    {
        var tenantId = GetTenantId();
        var now = DateTime.UtcNow;
        var startOfMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var incidents = await _dbContext.SafetyIncidents
            .AsNoTracking()
            .Include(x => x.Branch)
            .Include(x => x.Location)
            .Include(x => x.Investigation)
            .Include(x => x.Participants)
            .Include(x => x.Vehicles)
            .Include(x => x.Assets)
            .Include(x => x.Evidence)
            .Include(x => x.CorrectiveActions)
            .Where(x => x.TenantId == tenantId)
            .ToListAsync(cancellationToken);

        int openIncidents = incidents.Count(x => x.Status != SafetyIncidentStatus.Closed && x.Status != SafetyIncidentStatus.Cancelled);
        int criticalIncidents = incidents.Count(x => x.Severity == SafetyIncidentSeverity.Critical && x.Status != SafetyIncidentStatus.Closed && x.Status != SafetyIncidentStatus.Cancelled);
        int incidentsThisMonth = incidents.Count(x => x.OccurredAtUtc >= startOfMonth);
        int nearMissesThisMonth = incidents.Count(x => x.IncidentType == SafetyIncidentType.NearMiss && x.OccurredAtUtc >= startOfMonth);

        var violations = await _dbContext.SafetyViolations
            .AsNoTracking()
            .Include(x => x.Driver)
            .Include(x => x.Employee)
            .Include(x => x.Vehicle)
            .Include(x => x.SafetyIncident)
            .Where(x => x.TenantId == tenantId)
            .ToListAsync(cancellationToken);

        int openViolations = violations.Count(x => !x.IsResolved);

        var correctiveActions = await _dbContext.CorrectiveActions
            .AsNoTracking()
            .Include(x => x.AssignedEmployee)
            .Include(x => x.SafetyIncident)
            .Where(x => x.TenantId == tenantId)
            .ToListAsync(cancellationToken);

        int openCorrectiveActions = correctiveActions.Count(x => x.Status == CorrectiveActionStatus.Open || x.Status == CorrectiveActionStatus.InProgress || x.Status == CorrectiveActionStatus.Overdue);
        int overdueCorrectiveActions = correctiveActions.Count(x => x.DueDateUtc.HasValue && x.DueDateUtc.Value < now &&
            (x.Status == CorrectiveActionStatus.Open || x.Status == CorrectiveActionStatus.InProgress || x.Status == CorrectiveActionStatus.Overdue));

        var scoreDto = await _safetyScoreService.CalculateTenantSafetyScoreAsync(cancellationToken);

        var incidentsByType = Enum.GetValues<SafetyIncidentType>()
            .Select(t => new IncidentsByTypeDto(t, t.ToString(), incidents.Count(i => i.IncidentType == t)))
            .Where(x => x.Count > 0)
            .OrderByDescending(x => x.Count)
            .ToList();

        var incidentsBySeverity = Enum.GetValues<SafetyIncidentSeverity>()
            .Select(s => new IncidentsBySeverityDto(s, s.ToString(), incidents.Count(i => i.Severity == s)))
            .ToList();

        var branches = await _dbContext.Branches
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.IsActive)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);

        var incidentsByBranch = branches
            .Select(b => new IncidentsByBranchDto(
                b.Id,
                b.Name,
                incidents.Count(i => i.BranchId == b.Id),
                violations.Count(v => v.Vehicle?.BranchId == b.Id || v.Driver?.BranchId == b.Id)))
            .ToList();

        // 6-month monthly trend
        var monthlyTrend = new List<MonthlyIncidentTrendDto>();
        for (int i = 5; i >= 0; i--)
        {
            var monthDate = now.AddMonths(-i);
            var mStart = new DateTime(monthDate.Year, monthDate.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var mEnd = mStart.AddMonths(1);

            int incCount = incidents.Count(x => x.OccurredAtUtc >= mStart && x.OccurredAtUtc < mEnd);
            int accCount = incidents.Count(x => x.OccurredAtUtc >= mStart && x.OccurredAtUtc < mEnd && x.IncidentType == SafetyIncidentType.VehicleAccident);
            int nearMissCount = incidents.Count(x => x.OccurredAtUtc >= mStart && x.OccurredAtUtc < mEnd && x.IncidentType == SafetyIncidentType.NearMiss);

            monthlyTrend.Add(new MonthlyIncidentTrendDto(
                mStart.ToString("MMM yyyy", CultureInfo.InvariantCulture),
                mStart.Year,
                mStart.Month,
                incCount,
                accCount,
                nearMissCount));
        }

        var recentIncidents = incidents
            .OrderByDescending(x => x.OccurredAtUtc)
            .Take(5)
            .Select(i => new SafetyIncidentDto(
                i.Id,
                i.TenantId,
                i.IncidentNumber,
                i.IncidentType,
                i.Severity,
                i.Status,
                i.OccurredAtUtc,
                i.ReportedAtUtc,
                i.BranchId,
                i.Branch?.Name,
                i.LocationId,
                i.Location?.Name,
                i.Latitude,
                i.Longitude,
                i.Title,
                i.Description,
                i.ImmediateActionTaken,
                i.ReportedByUserId,
                null,
                i.InvestigationRequired,
                i.Investigation?.Status ?? SafetyInvestigationStatus.NotStarted,
                i.Participants.Count,
                i.Vehicles.Count,
                i.Assets.Count,
                i.Evidence.Count,
                i.CorrectiveActions.Count,
                i.CorrectiveActions.Count(c => c.Status == CorrectiveActionStatus.Open || c.Status == CorrectiveActionStatus.InProgress || c.Status == CorrectiveActionStatus.Overdue),
                i.ClosedAtUtc,
                i.ClosedByUserId,
                i.CreatedAtUtc))
            .ToList();

        var recentViolations = violations
            .OrderByDescending(x => x.OccurredAtUtc)
            .Take(5)
            .Select(v => new SafetyViolationDto(
                v.Id,
                v.TenantId,
                v.ViolationType,
                v.Severity,
                v.Source,
                v.OccurredAtUtc,
                v.Description,
                v.DriverId,
                v.Driver?.DisplayName,
                v.EmployeeId,
                v.Employee != null ? $"{v.Employee.FirstName} {v.Employee.LastName}" : null,
                v.VehicleId,
                v.Vehicle != null ? (v.Vehicle.RegistrationNumber ?? v.Vehicle.VehicleNumber) : null,
                v.SafetyIncidentId,
                v.SafetyIncident?.IncidentNumber,
                v.Reference,
                v.IsResolved,
                v.ResolvedAtUtc,
                v.ResolvedByUserId,
                null,
                v.ResolutionNotes,
                v.Notes,
                v.CreatedAtUtc))
            .ToList();

        var topRepeatedViolations = violations
            .GroupBy(v => v.ViolationType)
            .Select(g => new TopRepeatedViolationDto(g.Key, g.Key.ToString(), g.Count()))
            .OrderByDescending(x => x.Count)
            .Take(5)
            .ToList();

        return new SafetyDashboardDto(
            openIncidents,
            criticalIncidents,
            incidentsThisMonth,
            nearMissesThisMonth,
            openViolations,
            openCorrectiveActions,
            overdueCorrectiveActions,
            scoreDto.Score,
            scoreDto.RiskLevel,
            incidentsByType,
            incidentsBySeverity,
            incidentsByBranch,
            monthlyTrend,
            recentIncidents,
            recentViolations,
            topRepeatedViolations);
    }
}
