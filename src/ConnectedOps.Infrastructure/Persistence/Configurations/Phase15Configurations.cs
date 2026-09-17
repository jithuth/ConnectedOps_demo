using ConnectedOps.Domain.Expenses;
using ConnectedOps.Domain.Gamification;
using ConnectedOps.Domain.Hos;
using ConnectedOps.Domain.Tracking;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConnectedOps.Infrastructure.Persistence.Configurations;

public sealed class HosRuleConfigurationConfiguration : IEntityTypeConfiguration<HosRuleConfiguration>
{
    public void Configure(EntityTypeBuilder<HosRuleConfiguration> builder)
    {
        builder.ToTable("HosRuleConfigurations");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.TenantId).IsUnique();
        builder.Property(x => x.PresetType).IsRequired();
    }
}

public sealed class HosLogEntryConfiguration : IEntityTypeConfiguration<HosLogEntry>
{
    public void Configure(EntityTypeBuilder<HosLogEntry> builder)
    {
        builder.ToTable("HosLogEntries");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.TenantId, x.DriverId, x.StartedAtUtc });

        builder.HasOne(x => x.Driver)
            .WithMany()
            .HasForeignKey(x => x.DriverId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Vehicle)
            .WithMany()
            .HasForeignKey(x => x.VehicleId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Property(x => x.StartOdometer).HasPrecision(18, 2);
        builder.Property(x => x.EndOdometer).HasPrecision(18, 2);
        builder.Property(x => x.LocationName).HasMaxLength(250);
        builder.Property(x => x.Notes).HasMaxLength(1000);
    }
}

public sealed class HosViolationConfiguration : IEntityTypeConfiguration<HosViolation>
{
    public void Configure(EntityTypeBuilder<HosViolation> builder)
    {
        builder.ToTable("HosViolations");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.TenantId, x.DriverId, x.OccurredAtUtc });

        builder.HasOne(x => x.Driver)
            .WithMany()
            .HasForeignKey(x => x.DriverId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(x => x.Notes).HasMaxLength(1000);
    }
}

public sealed class DriverScorecardConfiguration : IEntityTypeConfiguration<DriverScorecard>
{
    public void Configure(EntityTypeBuilder<DriverScorecard> builder)
    {
        builder.ToTable("DriverScorecards");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.TenantId, x.DriverId, x.PeriodYear, x.PeriodMonth }).IsUnique();

        builder.HasOne(x => x.Driver)
            .WithMany()
            .HasForeignKey(x => x.DriverId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class DriverBadgeConfiguration : IEntityTypeConfiguration<DriverBadge>
{
    public void Configure(EntityTypeBuilder<DriverBadge> builder)
    {
        builder.ToTable("DriverBadges");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.TenantId, x.DriverId, x.BadgeType });

        builder.HasOne(x => x.Driver)
            .WithMany()
            .HasForeignKey(x => x.DriverId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(x => x.Title).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(500).IsRequired();
    }
}

public sealed class DriverTripExpenseConfiguration : IEntityTypeConfiguration<DriverTripExpense>
{
    public void Configure(EntityTypeBuilder<DriverTripExpense> builder)
    {
        builder.ToTable("DriverTripExpenses");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.TenantId, x.DriverId, x.IncurredAtUtc });

        builder.HasOne(x => x.Driver)
            .WithMany()
            .HasForeignKey(x => x.DriverId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Vehicle)
            .WithMany()
            .HasForeignKey(x => x.VehicleId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Property(x => x.Amount).HasPrecision(18, 2);
        builder.Property(x => x.Currency).HasMaxLength(10).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(500).IsRequired();
        builder.Property(x => x.ReceiptImageKey).HasMaxLength(250);
        builder.Property(x => x.RejectionReason).HasMaxLength(500);
        builder.Property(x => x.ReimbursementReference).HasMaxLength(100);
    }
}

public sealed class PublicTrackingTokenConfiguration : IEntityTypeConfiguration<PublicTrackingToken>
{
    public void Configure(EntityTypeBuilder<PublicTrackingToken> builder)
    {
        builder.ToTable("PublicTrackingTokens");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.Token).IsUnique();
        builder.HasIndex(x => new { x.TenantId, x.DispatchJobId });

        builder.HasOne(x => x.DispatchJob)
            .WithMany()
            .HasForeignKey(x => x.DispatchJobId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(x => x.Token).HasMaxLength(64).IsRequired();
        builder.Property(x => x.CustomerName).HasMaxLength(150).IsRequired();
        builder.Property(x => x.CustomerPhone).HasMaxLength(50).IsRequired();
        builder.Property(x => x.FeedbackComment).HasMaxLength(1000);
    }
}
