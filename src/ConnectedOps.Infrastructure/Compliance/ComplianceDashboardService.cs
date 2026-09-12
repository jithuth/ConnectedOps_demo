using ConnectedOps.Application.Common.Interfaces;
using ConnectedOps.Application.Compliance;
using ConnectedOps.Domain.Compliance;
using ConnectedOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ConnectedOps.Infrastructure.Compliance;

public sealed class ComplianceDashboardService : IComplianceDashboardService
{
    private readonly ConnectedOpsDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IComplianceEvaluationService _evaluationService;
    private readonly IComplianceScoreService _scoreService;

    public ComplianceDashboardService(
        ConnectedOpsDbContext dbContext,
        ICurrentUserContext currentUserContext,
        IComplianceEvaluationService evaluationService,
        IComplianceScoreService scoreService)
    {
        _dbContext = dbContext;
        _currentUserContext = currentUserContext;
        _evaluationService = evaluationService;
        _scoreService = scoreService;
    }

    private Guid GetTenantId() =>
        _currentUserContext.TenantId ?? throw new UnauthorizedAccessException("Tenant context is required.");

    public async Task<ComplianceDashboardDto> GetDashboardMetricsAsync(CancellationToken cancellationToken = default)
    {
        var tenantId = GetTenantId();
        var allSummaries = await _evaluationService.EvaluateAllSubjectsAsync(cancellationToken: cancellationToken);

        var vehicleSummaries = allSummaries.Where(s => s.SubjectType == ComplianceSubjectType.Vehicle).ToList();
        var driverSummaries = allSummaries.Where(s => s.SubjectType == ComplianceSubjectType.Driver).ToList();
        var assetSummaries = allSummaries.Where(s => s.SubjectType == ComplianceSubjectType.Asset).ToList();

        int totalVehicles = vehicleSummaries.Count;
        int compliantVehicles = vehicleSummaries.Count(s => s.OverallStatus == ComplianceStatus.Valid || s.OverallStatus == ComplianceStatus.ExpiringSoon);
        int nonCompliantVehicles = totalVehicles - compliantVehicles;
        double vehicleRate = totalVehicles > 0 ? Math.Round((double)compliantVehicles / totalVehicles * 100, 1) : 100.0;

        int totalDrivers = driverSummaries.Count;
        int compliantDrivers = driverSummaries.Count(s => s.OverallStatus == ComplianceStatus.Valid || s.OverallStatus == ComplianceStatus.ExpiringSoon);
        int nonCompliantDrivers = totalDrivers - compliantDrivers;
        double driverRate = totalDrivers > 0 ? Math.Round((double)compliantDrivers / totalDrivers * 100, 1) : 100.0;

        int totalAssets = assetSummaries.Count;
        int compliantAssets = assetSummaries.Count(s => s.OverallStatus == ComplianceStatus.Valid || s.OverallStatus == ComplianceStatus.ExpiringSoon);
        int nonCompliantAssets = totalAssets - compliantAssets;
        double assetRate = totalAssets > 0 ? Math.Round((double)compliantAssets / totalAssets * 100, 1) : 100.0;

        int overallScore = allSummaries.Count > 0 ? (int)Math.Round(allSummaries.Average(s => s.Score)) : 100;
        var overallGrade = _scoreService.MapScoreToGrade(overallScore);

        var now = DateTime.UtcNow;
        var in7Days = now.AddDays(7);
        var in30Days = now.AddDays(30);

        var activeRecords = await _dbContext.ComplianceRecords
            .AsNoTracking()
            .Include(x => x.ComplianceRequirement)
            .Include(x => x.Vehicle)
            .Include(x => x.Driver)
            .Include(x => x.Asset)
            .Where(x => x.TenantId == tenantId)
            .ToListAsync(cancellationToken);

        int expiringIn7Days = activeRecords.Count(r => r.ExpiryDateUtc.HasValue && r.ExpiryDateUtc.Value >= now && r.ExpiryDateUtc.Value <= in7Days);
        int expiringIn30Days = activeRecords.Count(r => r.ExpiryDateUtc.HasValue && r.ExpiryDateUtc.Value >= now && r.ExpiryDateUtc.Value <= in30Days);
        int expiredItems = activeRecords.Count(r => r.ExpiryDateUtc.HasValue && r.ExpiryDateUtc.Value < now);
        int missingItems = allSummaries.Sum(s => s.MissingCount);
        int pendingVerificationItems = activeRecords.Count(r => r.Status == ComplianceStatus.PendingVerification);

        int activeExceptions = await _dbContext.ComplianceExceptions
            .AsNoTracking()
            .CountAsync(x => x.TenantId == tenantId && x.Status == ComplianceExceptionStatus.Approved && x.EffectiveFromUtc <= now && now <= x.EffectiveToUtc, cancellationToken);

        var subjectTypeCounts = new List<ComplianceSubjectStatusCountDto>
        {
            new(
                ComplianceSubjectType.Vehicle,
                totalVehicles,
                compliantVehicles,
                vehicleSummaries.Sum(s => s.ExpiringCount),
                vehicleSummaries.Sum(s => s.ExpiredCount),
                vehicleSummaries.Sum(s => s.MissingCount)),
            new(
                ComplianceSubjectType.Driver,
                totalDrivers,
                compliantDrivers,
                driverSummaries.Sum(s => s.ExpiringCount),
                driverSummaries.Sum(s => s.ExpiredCount),
                driverSummaries.Sum(s => s.MissingCount)),
            new(
                ComplianceSubjectType.Asset,
                totalAssets,
                compliantAssets,
                assetSummaries.Sum(s => s.ExpiringCount),
                assetSummaries.Sum(s => s.ExpiredCount),
                assetSummaries.Sum(s => s.MissingCount))
        };

        var branches = await _dbContext.Branches
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.IsActive)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);

        var branchMetrics = new List<ComplianceByBranchDto>();
        foreach (var b in branches)
        {
            var branchSubjects = allSummaries.Where(s => s.BranchId == b.Id).ToList();
            int total = branchSubjects.Count;
            int compliant = branchSubjects.Count(s => s.OverallStatus == ComplianceStatus.Valid || s.OverallStatus == ComplianceStatus.ExpiringSoon);
            int nonCompliant = total - compliant;
            double rate = total > 0 ? Math.Round((double)compliant / total * 100, 1) : 100.0;
            branchMetrics.Add(new ComplianceByBranchDto(b.Id, b.Name, total, compliant, nonCompliant, rate));
        }

        var unassignedSubjects = allSummaries.Where(s => !s.BranchId.HasValue).ToList();
        if (unassignedSubjects.Count > 0)
        {
            int total = unassignedSubjects.Count;
            int compliant = unassignedSubjects.Count(s => s.OverallStatus == ComplianceStatus.Valid || s.OverallStatus == ComplianceStatus.ExpiringSoon);
            int nonCompliant = total - compliant;
            double rate = Math.Round((double)compliant / total * 100, 1);
            branchMetrics.Add(new ComplianceByBranchDto(null, "Unassigned", total, compliant, nonCompliant, rate));
        }

        var recentlyExpired = activeRecords
            .Where(r => r.ExpiryDateUtc.HasValue && r.ExpiryDateUtc.Value < now)
            .OrderByDescending(r => r.ExpiryDateUtc)
            .Take(10)
            .Select(r => new ComplianceExpiringItemDto(
                r.VehicleId ?? r.DriverId ?? r.AssetId ?? r.Id,
                r.SubjectType,
                r.Vehicle?.RegistrationNumber ?? r.Vehicle?.VehicleNumber ?? r.Driver?.DriverNumber ?? r.Asset?.AssetNumber ?? "N/A",
                r.Vehicle?.DisplayName ?? r.Driver?.DisplayName ?? r.Asset?.Name ?? "N/A",
                r.ComplianceRequirementId,
                r.ComplianceRequirement?.Code ?? string.Empty,
                r.ComplianceRequirement?.Name ?? string.Empty,
                r.ExpiryDateUtc,
                r.ExpiryDateUtc.HasValue ? (int)(r.ExpiryDateUtc.Value.Date - now.Date).TotalDays : null,
                ComplianceStatus.Expired))
            .ToList();

        var upcomingExpiries = activeRecords
            .Where(r => r.ExpiryDateUtc.HasValue && r.ExpiryDateUtc.Value >= now && r.ExpiryDateUtc.Value <= in30Days)
            .OrderBy(r => r.ExpiryDateUtc)
            .Take(10)
            .Select(r => new ComplianceExpiringItemDto(
                r.VehicleId ?? r.DriverId ?? r.AssetId ?? r.Id,
                r.SubjectType,
                r.Vehicle?.RegistrationNumber ?? r.Vehicle?.VehicleNumber ?? r.Driver?.DriverNumber ?? r.Asset?.AssetNumber ?? "N/A",
                r.Vehicle?.DisplayName ?? r.Driver?.DisplayName ?? r.Asset?.Name ?? "N/A",
                r.ComplianceRequirementId,
                r.ComplianceRequirement?.Code ?? string.Empty,
                r.ComplianceRequirement?.Name ?? string.Empty,
                r.ExpiryDateUtc,
                r.ExpiryDateUtc.HasValue ? (int)(r.ExpiryDateUtc.Value.Date - now.Date).TotalDays : null,
                ComplianceStatus.ExpiringSoon))
            .ToList();

        var lowestComplianceSubjects = allSummaries
            .OrderBy(s => s.Score)
            .ThenByDescending(s => s.ExpiredCount)
            .ThenByDescending(s => s.MissingCount)
            .Take(10)
            .ToList();

        return new ComplianceDashboardDto(
            totalVehicles,
            compliantVehicles,
            nonCompliantVehicles,
            vehicleRate,
            totalDrivers,
            compliantDrivers,
            nonCompliantDrivers,
            driverRate,
            totalAssets,
            compliantAssets,
            nonCompliantAssets,
            assetRate,
            overallScore,
            overallGrade,
            expiringIn7Days,
            expiringIn30Days,
            expiredItems,
            missingItems,
            pendingVerificationItems,
            activeExceptions,
            subjectTypeCounts,
            branchMetrics,
            recentlyExpired,
            upcomingExpiries,
            lowestComplianceSubjects);
    }
}
