using ConnectedOps.Domain.Maintenance;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConnectedOps.Infrastructure.Persistence.Configurations;

public sealed class VehicleMaintenanceTaskConfiguration : IEntityTypeConfiguration<VehicleMaintenanceTask>
{
    public void Configure(EntityTypeBuilder<VehicleMaintenanceTask> builder)
    {
        builder.ToTable("VehicleMaintenanceTasks");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.TenantId)
            .IsRequired();

        builder.Property(x => x.MaintenanceRecordId)
            .IsRequired();

        builder.Property(x => x.Name)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(x => x.Description)
            .HasMaxLength(500);

        builder.Property(x => x.Status)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.Notes)
            .HasMaxLength(500);

        builder.HasIndex(x => new { x.TenantId, x.MaintenanceRecordId });

        builder.HasOne(x => x.MaintenanceServiceType)
            .WithMany()
            .HasForeignKey(x => x.MaintenanceServiceTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
