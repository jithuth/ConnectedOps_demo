using ConnectedOps.Domain.Maintenance;
using ConnectedOps.Domain.Vehicles;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConnectedOps.Infrastructure.Persistence.Configurations;

public sealed class VehicleMaintenanceDueConfiguration : IEntityTypeConfiguration<VehicleMaintenanceDue>
{
    public void Configure(EntityTypeBuilder<VehicleMaintenanceDue> builder)
    {
        builder.ToTable("VehicleMaintenanceDues");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.TenantId)
            .IsRequired();

        builder.Property(x => x.VehicleId)
            .IsRequired();

        builder.Property(x => x.MaintenancePlanRuleId)
            .IsRequired();

        builder.Property(x => x.NextDueOdometer)
            .HasPrecision(18, 2);

        builder.Property(x => x.NextDueEngineHours)
            .HasPrecision(18, 2);

        builder.Property(x => x.DueStatus)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.CalculatedAtUtc)
            .IsRequired();

        builder.HasIndex(x => new { x.TenantId, x.VehicleId, x.MaintenancePlanRuleId })
            .IsUnique();

        builder.HasIndex(x => new { x.TenantId, x.DueStatus });
        builder.HasIndex(x => new { x.TenantId, x.NextDueDateUtc });

        builder.HasOne(x => x.Vehicle)
            .WithMany()
            .HasForeignKey(x => x.VehicleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.MaintenancePlanRule)
            .WithMany()
            .HasForeignKey(x => x.MaintenancePlanRuleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.LastMaintenanceRecord)
            .WithMany()
            .HasForeignKey(x => x.LastMaintenanceRecordId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
