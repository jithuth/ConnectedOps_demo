using ConnectedOps.Domain.Maintenance;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConnectedOps.Infrastructure.Persistence.Configurations;

public sealed class VehicleMaintenancePartConfiguration : IEntityTypeConfiguration<VehicleMaintenancePart>
{
    public void Configure(EntityTypeBuilder<VehicleMaintenancePart> builder)
    {
        builder.ToTable("VehicleMaintenanceParts");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.TenantId)
            .IsRequired();

        builder.Property(x => x.MaintenanceRecordId)
            .IsRequired();

        builder.Property(x => x.PartNumber)
            .HasMaxLength(100);

        builder.Property(x => x.PartName)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(x => x.Quantity)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(x => x.Unit)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.UnitCost)
            .HasPrecision(18, 2);

        builder.Property(x => x.TotalCost)
            .HasPrecision(18, 2);

        builder.Property(x => x.Supplier)
            .HasMaxLength(150);

        builder.Property(x => x.Notes)
            .HasMaxLength(500);

        builder.HasIndex(x => new { x.TenantId, x.MaintenanceRecordId });

        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
