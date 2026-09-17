using ConnectedOps.Domain.Ev;
using ConnectedOps.Domain.Optimization;
using ConnectedOps.Domain.WhiteLabel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConnectedOps.Infrastructure.Persistence.Configurations;

public sealed class ChargingStationConfiguration : IEntityTypeConfiguration<ChargingStation>
{
    public void Configure(EntityTypeBuilder<ChargingStation> builder)
    {
        builder.ToTable("ChargingStations");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Code)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.Address)
            .HasMaxLength(500);

        builder.Property(x => x.Currency)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(x => x.MaxPowerKw)
            .HasPrecision(18, 2);

        builder.Property(x => x.OffPeakRatePerKwh)
            .HasPrecision(18, 4);

        builder.Property(x => x.PeakRatePerKwh)
            .HasPrecision(18, 4);

        builder.HasIndex(x => new { x.TenantId, x.Code })
            .IsUnique();

        builder.HasIndex(x => new { x.TenantId, x.IsActive });
    }
}

public sealed class VehicleBatteryStateConfiguration : IEntityTypeConfiguration<VehicleBatteryState>
{
    public void Configure(EntityTypeBuilder<VehicleBatteryState> builder)
    {
        builder.ToTable("VehicleBatteryStates");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.BatteryCapacityKwh)
            .HasPrecision(18, 2);

        builder.Property(x => x.StateOfChargePercent)
            .HasPrecision(5, 2);

        builder.Property(x => x.StateOfHealthPercent)
            .HasPrecision(5, 2);

        builder.Property(x => x.RemainingRangeKm)
            .HasPrecision(18, 2);

        builder.Property(x => x.BatteryPackTempCelsius)
            .HasPrecision(5, 2);

        builder.Property(x => x.ActiveChargingPowerKw)
            .HasPrecision(18, 2);

        builder.HasOne(x => x.Vehicle)
            .WithMany()
            .HasForeignKey(x => x.VehicleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.TenantId, x.VehicleId })
            .IsUnique();

        builder.HasIndex(x => new { x.TenantId, x.ChargingStatus });
    }
}

public sealed class ChargingSessionConfiguration : IEntityTypeConfiguration<ChargingSession>
{
    public void Configure(EntityTypeBuilder<ChargingSession> builder)
    {
        builder.ToTable("ChargingSessions");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.StartSocPercent)
            .HasPrecision(5, 2);

        builder.Property(x => x.EndSocPercent)
            .HasPrecision(5, 2);

        builder.Property(x => x.EnergyDeliveredKwh)
            .HasPrecision(18, 2);

        builder.Property(x => x.TotalCost)
            .HasPrecision(18, 2);

        builder.Property(x => x.Co2SavedKg)
            .HasPrecision(18, 2);

        builder.Property(x => x.Currency)
            .IsRequired()
            .HasMaxLength(10);

        builder.HasOne(x => x.Vehicle)
            .WithMany()
            .HasForeignKey(x => x.VehicleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.ChargingStation)
            .WithMany()
            .HasForeignKey(x => x.ChargingStationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.TenantId, x.VehicleId });
        builder.HasIndex(x => new { x.TenantId, x.ChargingStationId });
        builder.HasIndex(x => new { x.TenantId, x.IsActive });
    }
}

public sealed class RouteOptimizationRunConfiguration : IEntityTypeConfiguration<RouteOptimizationRun>
{
    public void Configure(EntityTypeBuilder<RouteOptimizationRun> builder)
    {
        builder.ToTable("RouteOptimizationRuns");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.RunNumber)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.TotalDistanceKm)
            .HasPrecision(18, 2);

        builder.Property(x => x.TotalPayloadWeightKg)
            .HasPrecision(18, 2);

        builder.Property(x => x.EfficiencyScorePercent)
            .HasPrecision(5, 2);

        builder.HasMany(x => x.RoutePlans)
            .WithOne(x => x.OptimizationRun)
            .HasForeignKey(x => x.OptimizationRunId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.TenantId, x.RunNumber })
            .IsUnique();

        builder.HasIndex(x => new { x.TenantId, x.Status });
    }
}

public sealed class OptimizedRoutePlanConfiguration : IEntityTypeConfiguration<OptimizedRoutePlan>
{
    public void Configure(EntityTypeBuilder<OptimizedRoutePlan> builder)
    {
        builder.ToTable("OptimizedRoutePlans");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.RouteName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.DistanceKm)
            .HasPrecision(18, 2);

        builder.Property(x => x.PayloadWeightKg)
            .HasPrecision(18, 2);

        builder.HasOne(x => x.Vehicle)
            .WithMany()
            .HasForeignKey(x => x.VehicleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Driver)
            .WithMany()
            .HasForeignKey(x => x.DriverId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(x => x.Stops)
            .WithOne(x => x.RoutePlan)
            .HasForeignKey(x => x.OptimizedRoutePlanId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.TenantId, x.OptimizationRunId });
    }
}

public sealed class OptimizedStopSequenceConfiguration : IEntityTypeConfiguration<OptimizedStopSequence>
{
    public void Configure(EntityTypeBuilder<OptimizedStopSequence> builder)
    {
        builder.ToTable("OptimizedStopSequences");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.LocationName)
            .IsRequired()
            .HasMaxLength(250);

        builder.Property(x => x.Address)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(x => x.CustomerContact)
            .HasMaxLength(200);

        builder.Property(x => x.DemandWeightKg)
            .HasPrecision(18, 2);

        builder.HasIndex(x => new { x.TenantId, x.OptimizedRoutePlanId, x.SequenceOrder });
    }
}

public sealed class TenantBrandingConfiguration : IEntityTypeConfiguration<TenantBranding>
{
    public void Configure(EntityTypeBuilder<TenantBranding> builder)
    {
        builder.ToTable("TenantBrandings");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.PlatformTitle)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(x => x.LogoUrl)
            .HasMaxLength(1000);

        builder.Property(x => x.FaviconUrl)
            .HasMaxLength(1000);

        builder.Property(x => x.PrimaryAccentColor)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.SecondaryAccentColor)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.SupportEmail)
            .HasMaxLength(250);

        builder.Property(x => x.CustomLoginBannerUrl)
            .HasMaxLength(1000);

        builder.Property(x => x.CustomFooterText)
            .HasMaxLength(500);

        builder.HasIndex(x => x.TenantId)
            .IsUnique();
    }
}

public sealed class TenantCustomDomainConfiguration : IEntityTypeConfiguration<TenantCustomDomain>
{
    public void Configure(EntityTypeBuilder<TenantCustomDomain> builder)
    {
        builder.ToTable("TenantCustomDomains");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Hostname)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(x => x.VerificationToken)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex(x => x.Hostname)
            .IsUnique();

        builder.HasIndex(x => new { x.TenantId, x.Status });
    }
}

public sealed class AuditCompliancePackageConfiguration : IEntityTypeConfiguration<AuditCompliancePackage>
{
    public void Configure(EntityTypeBuilder<AuditCompliancePackage> builder)
    {
        builder.ToTable("AuditCompliancePackages");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.PackageNumber)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.ChecksumSha256)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex(x => new { x.TenantId, x.PackageNumber })
            .IsUnique();

        builder.HasIndex(x => new { x.TenantId, x.PackageType });
    }
}
