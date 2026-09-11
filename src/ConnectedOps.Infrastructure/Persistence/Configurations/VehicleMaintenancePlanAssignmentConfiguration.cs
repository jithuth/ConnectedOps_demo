using ConnectedOps.Domain.Maintenance;
using ConnectedOps.Domain.Vehicles;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConnectedOps.Infrastructure.Persistence.Configurations;

public sealed class VehicleMaintenancePlanAssignmentConfiguration : IEntityTypeConfiguration<VehicleMaintenancePlanAssignment>
{
    public void Configure(EntityTypeBuilder<VehicleMaintenancePlanAssignment> builder)
    {
        builder.ToTable("VehicleMaintenancePlanAssignments");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.TenantId)
            .IsRequired();

        builder.Property(x => x.VehicleId)
            .IsRequired();

        builder.Property(x => x.MaintenancePlanId)
            .IsRequired();

        builder.Property(x => x.BaselineOdometer)
            .HasPrecision(18, 2);

        builder.Property(x => x.BaselineEngineHours)
            .HasPrecision(18, 2);

        builder.Property(x => x.Notes)
            .HasMaxLength(500);

        builder.Property(x => x.IsActive)
            .IsRequired();

        builder.HasIndex(x => new { x.TenantId, x.VehicleId, x.IsActive });
        builder.HasIndex(x => new { x.TenantId, x.MaintenancePlanId });

        builder.HasOne(x => x.Vehicle)
            .WithMany()
            .HasForeignKey(x => x.VehicleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.MaintenancePlan)
            .WithMany(x => x.Assignments)
            .HasForeignKey(x => x.MaintenancePlanId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
