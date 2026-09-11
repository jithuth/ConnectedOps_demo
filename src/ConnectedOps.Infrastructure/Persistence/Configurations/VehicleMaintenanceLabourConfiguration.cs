using ConnectedOps.Domain.Maintenance;
using ConnectedOps.Domain.Organization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConnectedOps.Infrastructure.Persistence.Configurations;

public sealed class VehicleMaintenanceLabourConfiguration : IEntityTypeConfiguration<VehicleMaintenanceLabour>
{
    public void Configure(EntityTypeBuilder<VehicleMaintenanceLabour> builder)
    {
        builder.ToTable("VehicleMaintenanceLabours");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.TenantId)
            .IsRequired();

        builder.Property(x => x.MaintenanceRecordId)
            .IsRequired();

        builder.Property(x => x.Description)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.Hours)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(x => x.HourlyRate)
            .HasPrecision(18, 2);

        builder.Property(x => x.TotalCost)
            .HasPrecision(18, 2);

        builder.Property(x => x.TechnicianName)
            .HasMaxLength(150);

        builder.Property(x => x.Notes)
            .HasMaxLength(500);

        builder.HasIndex(x => new { x.TenantId, x.MaintenanceRecordId });
        builder.HasIndex(x => new { x.TenantId, x.EmployeeId });

        builder.HasOne(x => x.Employee)
            .WithMany()
            .HasForeignKey(x => x.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
