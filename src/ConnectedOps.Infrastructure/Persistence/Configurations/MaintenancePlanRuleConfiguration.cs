using ConnectedOps.Domain.Maintenance;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConnectedOps.Infrastructure.Persistence.Configurations;

public sealed class MaintenancePlanRuleConfiguration : IEntityTypeConfiguration<MaintenancePlanRule>
{
    public void Configure(EntityTypeBuilder<MaintenancePlanRule> builder)
    {
        builder.ToTable("MaintenancePlanRules");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.TenantId)
            .IsRequired();

        builder.Property(x => x.MaintenancePlanId)
            .IsRequired();

        builder.Property(x => x.MaintenanceServiceTypeId)
            .IsRequired();

        builder.Property(x => x.ScheduleType)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.IntervalKilometers)
            .HasPrecision(18, 2);

        builder.Property(x => x.IntervalMiles)
            .HasPrecision(18, 2);

        builder.Property(x => x.IntervalEngineHours)
            .HasPrecision(18, 2);

        builder.Property(x => x.InitialDueKilometers)
            .HasPrecision(18, 2);

        builder.Property(x => x.InitialDueEngineHours)
            .HasPrecision(18, 2);

        builder.Property(x => x.ReminderBeforeKilometers)
            .HasPrecision(18, 2);

        builder.Property(x => x.ReminderBeforeEngineHours)
            .HasPrecision(18, 2);

        builder.Property(x => x.ToleranceKilometers)
            .HasPrecision(18, 2);

        builder.Property(x => x.ToleranceHours)
            .HasPrecision(18, 2);

        builder.Property(x => x.Notes)
            .HasMaxLength(500);

        builder.HasIndex(x => new { x.TenantId, x.MaintenancePlanId });
        builder.HasIndex(x => new { x.TenantId, x.MaintenanceServiceTypeId });

        builder.HasOne(x => x.MaintenanceServiceType)
            .WithMany()
            .HasForeignKey(x => x.MaintenanceServiceTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
