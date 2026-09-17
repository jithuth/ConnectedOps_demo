using ConnectedOps.Domain.Common;
using ConnectedOps.Domain.Drivers;

namespace ConnectedOps.Domain.Gamification;

public sealed class DriverScorecard : BaseEntity
{
    private DriverScorecard()
    {
    }

    public DriverScorecard(
        Guid tenantId,
        Guid driverId,
        int periodMonth,
        int periodYear,
        double safetyScore,
        double ecoScore,
        double complianceScore,
        double dispatchScore,
        int harshBrakingCount = 0,
        int harshAccelerationCount = 0,
        int speedingEventsCount = 0,
        double idleHours = 0.0)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        if (driverId == Guid.Empty)
            throw new ArgumentException("DriverId is required.", nameof(driverId));

        TenantId = tenantId;
        DriverId = driverId;
        PeriodMonth = periodMonth;
        PeriodYear = periodYear;
        SafetyScore = Math.Clamp(safetyScore, 0.0, 100.0);
        EcoScore = Math.Clamp(ecoScore, 0.0, 100.0);
        ComplianceScore = Math.Clamp(complianceScore, 0.0, 100.0);
        DispatchScore = Math.Clamp(dispatchScore, 0.0, 100.0);
        HarshBrakingCount = Math.Max(0, harshBrakingCount);
        HarshAccelerationCount = Math.Max(0, harshAccelerationCount);
        SpeedingEventsCount = Math.Max(0, speedingEventsCount);
        IdleHours = Math.Max(0.0, idleHours);

        RecalculateOverall();
    }

    public Guid TenantId { get; private set; }

    public Guid DriverId { get; private set; }
    public Driver Driver { get; set; } = null!;

    public int PeriodMonth { get; private set; }
    public int PeriodYear { get; private set; }

    public double OverallScore { get; private set; }
    public double SafetyScore { get; private set; }
    public double EcoScore { get; private set; }
    public double ComplianceScore { get; private set; }
    public double DispatchScore { get; private set; }

    public int HarshBrakingCount { get; private set; }
    public int HarshAccelerationCount { get; private set; }
    public int SpeedingEventsCount { get; private set; }
    public double IdleHours { get; private set; }

    public DriverTier Tier { get; private set; }
    public int RankInFleet { get; private set; }

    public void RecalculateOverall()
    {
        // Weighted composite score: 35% Safety, 25% Eco, 20% Compliance, 20% Dispatch
        OverallScore = Math.Round((SafetyScore * 0.35) + (EcoScore * 0.25) + (ComplianceScore * 0.20) + (DispatchScore * 0.20), 1);

        Tier = OverallScore switch
        {
            >= 90.0 => DriverTier.Platinum,
            >= 80.0 => DriverTier.Gold,
            >= 70.0 => DriverTier.Silver,
            _ => DriverTier.Bronze
        };
    }

    public void UpdateMetrics(
        double safetyScore,
        double ecoScore,
        double complianceScore,
        double dispatchScore,
        int harshBraking = 0,
        int harshAccel = 0,
        int speeding = 0,
        double idleHours = 0.0)
    {
        SafetyScore = Math.Clamp(safetyScore, 0.0, 100.0);
        EcoScore = Math.Clamp(ecoScore, 0.0, 100.0);
        ComplianceScore = Math.Clamp(complianceScore, 0.0, 100.0);
        DispatchScore = Math.Clamp(dispatchScore, 0.0, 100.0);
        HarshBrakingCount = Math.Max(0, harshBraking);
        HarshAccelerationCount = Math.Max(0, harshAccel);
        SpeedingEventsCount = Math.Max(0, speeding);
        IdleHours = Math.Max(0.0, idleHours);
        RecalculateOverall();
    }

    public void SetRank(int rank)
    {
        RankInFleet = Math.Max(1, rank);
    }
}
