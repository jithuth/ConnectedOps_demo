using ConnectedOps.Application.Common.Interfaces;
using ConnectedOps.Application.Gamification;
using ConnectedOps.Application.Vehicles;
using ConnectedOps.Domain.Gamification;
using ConnectedOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ConnectedOps.Infrastructure.Gamification;

public sealed class GamificationService : IGamificationService
{
    private readonly ConnectedOpsDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly ILogger<GamificationService> _logger;

    public GamificationService(
        ConnectedOpsDbContext dbContext,
        ICurrentUserContext currentUserContext,
        ILogger<GamificationService> logger)
    {
        _dbContext = dbContext;
        _currentUserContext = currentUserContext;
        _logger = logger;
    }

    private Guid RequireTenantId()
    {
        return _currentUserContext.TenantId
            ?? throw new InvalidOperationException("Tenant context is required for gamification operations.");
    }

    public async Task<FleetLeaderboardDto> GetLeaderboardAsync(int? month = null, int? year = null, CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();
        var targetYear = year ?? DateTime.UtcNow.Year;
        var targetMonth = month ?? DateTime.UtcNow.Month;

        var scorecards = await _dbContext.DriverScorecards
            .AsNoTracking()
            .Include(s => s.Driver)
            .Where(s => s.TenantId == tenantId && s.PeriodYear == targetYear && s.PeriodMonth == targetMonth)
            .OrderByDescending(s => s.OverallScore)
            .ToListAsync(cancellationToken);

        var rank = 1;
        var rankings = new List<DriverScorecardDto>();

        foreach (var sc in scorecards)
        {
            var driverName = sc.Driver != null ? $"{sc.Driver.FirstName} {sc.Driver.LastName}".Trim() : "Unknown Driver";
            rankings.Add(new DriverScorecardDto(
                Id: sc.Id,
                DriverId: sc.DriverId,
                DriverName: driverName,
                PeriodMonth: sc.PeriodMonth,
                PeriodYear: sc.PeriodYear,
                OverallScore: sc.OverallScore,
                SafetyScore: sc.SafetyScore,
                EcoScore: sc.EcoScore,
                ComplianceScore: sc.ComplianceScore,
                DispatchScore: sc.DispatchScore,
                HarshBrakingCount: sc.HarshBrakingCount,
                HarshAccelerationCount: sc.HarshAccelerationCount,
                SpeedingEventsCount: sc.SpeedingEventsCount,
                IdleHours: sc.IdleHours,
                Tier: sc.Tier,
                TierName: sc.Tier.ToString(),
                RankInFleet: rank++));
        }

        var topThree = rankings.Take(3).ToList();
        var fleetAverage = scorecards.Count > 0 ? Math.Round(scorecards.Average(s => s.OverallScore), 1) : 0;

        return new FleetLeaderboardDto(
            PeriodMonth: targetMonth,
            PeriodYear: targetYear,
            TotalRankedDrivers: rankings.Count,
            FleetAverageScore: fleetAverage,
            TopThreePodium: topThree,
            Rankings: rankings);
    }

    public async Task<PagedResult<DriverScorecardDto>> GetScorecardsPagedAsync(ScorecardFilterRequest request, CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();
        var targetYear = request.Year ?? DateTime.UtcNow.Year;
        var targetMonth = request.Month ?? DateTime.UtcNow.Month;

        var query = _dbContext.DriverScorecards
            .AsNoTracking()
            .Include(s => s.Driver)
            .Where(s => s.TenantId == tenantId && s.PeriodYear == targetYear && s.PeriodMonth == targetMonth);

        if (request.Tier.HasValue)
        {
            query = query.Where(s => s.Tier == request.Tier.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var list = await query
            .OrderByDescending(s => s.OverallScore)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var dtos = list.Select((sc, idx) => new DriverScorecardDto(
            Id: sc.Id,
            DriverId: sc.DriverId,
            DriverName: sc.Driver != null ? $"{sc.Driver.FirstName} {sc.Driver.LastName}".Trim() : "Unknown Driver",
            PeriodMonth: sc.PeriodMonth,
            PeriodYear: sc.PeriodYear,
            OverallScore: sc.OverallScore,
            SafetyScore: sc.SafetyScore,
            EcoScore: sc.EcoScore,
            ComplianceScore: sc.ComplianceScore,
            DispatchScore: sc.DispatchScore,
            HarshBrakingCount: sc.HarshBrakingCount,
            HarshAccelerationCount: sc.HarshAccelerationCount,
            SpeedingEventsCount: sc.SpeedingEventsCount,
            IdleHours: sc.IdleHours,
            Tier: sc.Tier,
            TierName: sc.Tier.ToString(),
            RankInFleet: ((request.PageNumber - 1) * request.PageSize) + idx + 1
        )).ToList();

        return new PagedResult<DriverScorecardDto>(dtos, totalCount, request.PageNumber, request.PageSize);
    }

    public async Task<DriverScorecardDto?> GetDriverScorecardAsync(Guid driverId, int? month = null, int? year = null, CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();
        var targetYear = year ?? DateTime.UtcNow.Year;
        var targetMonth = month ?? DateTime.UtcNow.Month;

        var sc = await _dbContext.DriverScorecards
            .AsNoTracking()
            .Include(s => s.Driver)
            .FirstOrDefaultAsync(s => s.TenantId == tenantId && s.DriverId == driverId && s.PeriodYear == targetYear && s.PeriodMonth == targetMonth, cancellationToken);

        if (sc == null) return null;

        var rank = await _dbContext.DriverScorecards
            .CountAsync(s => s.TenantId == tenantId && s.PeriodYear == targetYear && s.PeriodMonth == targetMonth && s.OverallScore > sc.OverallScore, cancellationToken) + 1;

        return new DriverScorecardDto(
            Id: sc.Id,
            DriverId: sc.DriverId,
            DriverName: sc.Driver != null ? $"{sc.Driver.FirstName} {sc.Driver.LastName}".Trim() : "Unknown Driver",
            PeriodMonth: sc.PeriodMonth,
            PeriodYear: sc.PeriodYear,
            OverallScore: sc.OverallScore,
            SafetyScore: sc.SafetyScore,
            EcoScore: sc.EcoScore,
            ComplianceScore: sc.ComplianceScore,
            DispatchScore: sc.DispatchScore,
            HarshBrakingCount: sc.HarshBrakingCount,
            HarshAccelerationCount: sc.HarshAccelerationCount,
            SpeedingEventsCount: sc.SpeedingEventsCount,
            IdleHours: sc.IdleHours,
            Tier: sc.Tier,
            TierName: sc.Tier.ToString(),
            RankInFleet: rank);
    }

    public async Task<IReadOnlyCollection<DriverBadgeDto>> GetDriverBadgesAsync(Guid driverId, CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();

        var badges = await _dbContext.DriverBadges
            .AsNoTracking()
            .Include(b => b.Driver)
            .Where(b => b.TenantId == tenantId && b.DriverId == driverId)
            .OrderByDescending(b => b.EarnedAtUtc)
            .Select(b => new DriverBadgeDto(
                b.Id,
                b.DriverId,
                b.Driver != null ? $"{b.Driver.FirstName} {b.Driver.LastName}".Trim() : "Driver",
                b.BadgeType,
                b.BadgeType.ToString(),
                b.Title,
                b.Description,
                b.Points,
                b.EarnedAtUtc))
            .ToListAsync(cancellationToken);

        return badges;
    }

    public async Task<DriverBadgeDto> AwardBadgeAsync(AwardBadgeRequest request, CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();

        var driver = await _dbContext.Drivers
            .FirstOrDefaultAsync(d => d.TenantId == tenantId && d.Id == request.DriverId, cancellationToken)
            ?? throw new KeyNotFoundException($"Driver with ID {request.DriverId} was not found.");

        var badge = new DriverBadge(
            tenantId,
            request.DriverId,
            request.BadgeType,
            request.Title,
            request.Description,
            request.Points);

        _dbContext.DriverBadges.Add(badge);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Awarded badge {Title} to driver {DriverId} (+{Points} pts)", badge.Title, driver.Id, badge.Points);

        return new DriverBadgeDto(
            badge.Id,
            badge.DriverId,
            $"{driver.FirstName} {driver.LastName}".Trim(),
            badge.BadgeType,
            badge.BadgeType.ToString(),
            badge.Title,
            badge.Description,
            badge.Points,
            badge.EarnedAtUtc);
    }

    public async Task<int> RecalculateAllScorecardsAsync(int month, int year, CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();

        var drivers = await _dbContext.Drivers
            .Where(d => d.TenantId == tenantId)
            .ToListAsync(cancellationToken);

        var startOfMonth = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        var endOfMonth = startOfMonth.AddMonths(1);

        var violations = await _dbContext.HosViolations
            .Where(v => v.TenantId == tenantId && v.OccurredAtUtc >= startOfMonth && v.OccurredAtUtc < endOfMonth)
            .ToListAsync(cancellationToken);

        var count = 0;

        foreach (var driver in drivers)
        {
            var driverViolations = violations.Count(v => v.DriverId == driver.Id);

            var complianceDeduction = driverViolations * 15.0;
            var complianceScore = Math.Max(0.0, 100.0 - complianceDeduction);
            var safetyScore = Math.Clamp(95.0 - (driverViolations * 5), 50.0, 100.0);
            var ecoScore = 90.0;
            var dispatchScore = 96.0;

            var scorecard = await _dbContext.DriverScorecards
                .FirstOrDefaultAsync(s => s.TenantId == tenantId && s.DriverId == driver.Id && s.PeriodYear == year && s.PeriodMonth == month, cancellationToken);

            if (scorecard == null)
            {
                scorecard = new DriverScorecard(tenantId, driver.Id, month, year, safetyScore, ecoScore, complianceScore, dispatchScore);
                _dbContext.DriverScorecards.Add(scorecard);
            }
            else
            {
                scorecard.UpdateMetrics(
                    safetyScore: safetyScore,
                    ecoScore: ecoScore,
                    complianceScore: complianceScore,
                    dispatchScore: dispatchScore,
                    harshBraking: 1,
                    harshAccel: 1,
                    speeding: 1,
                    idleHours: 2.5);
            }

            if (scorecard.OverallScore >= 90.0)
            {
                var alreadyHasBadge = await _dbContext.DriverBadges
                    .AnyAsync(b => b.TenantId == tenantId && b.DriverId == driver.Id && b.BadgeType == BadgeType.TopPerformer && b.EarnedAtUtc >= startOfMonth, cancellationToken);

                if (!alreadyHasBadge)
                {
                    _dbContext.DriverBadges.Add(new DriverBadge(
                        tenantId,
                        driver.Id,
                        BadgeType.TopPerformer,
                        "Monthly Top Performer",
                        $"Achieved top tier score of {scorecard.OverallScore}% for {year}-{month:D2}",
                        300));
                }
            }

            count++;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Recalculated {Count} scorecards for month {Month}/{Year}", count, month, year);
        return count;
    }
}
