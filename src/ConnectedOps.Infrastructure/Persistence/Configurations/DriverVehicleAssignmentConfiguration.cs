using ConnectedOps.Domain.Drivers;
using ConnectedOps.Domain.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConnectedOps.Infrastructure.Persistence.Configurations;

public sealed class DriverVehicleAssignmentConfiguration : IEntityTypeConfiguration<DriverVehicleAssignment>
{
    public void Configure(EntityTypeBuilder<DriverVehicleAssignment> builder)
    {
        builder.ToTable("DriverVehicleAssignments");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.TenantId)
            .IsRequired();

        builder.Property(x => x.DriverId)
            .IsRequired();

        builder.Property(x => x.VehicleId)
            .IsRequired();

        builder.Property(x => x.AssignmentType)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.AssignedFromUtc)
            .IsRequired();

        builder.Property(x => x.IsPrimary)
            .IsRequired();

        builder.Property(x => x.IsActive)
            .IsRequired();

        builder.Property(x => x.Reason)
            .HasMaxLength(500);

        builder.Property(x => x.Notes)
            .HasMaxLength(1000);

        // Indexes
        builder.HasIndex(x => new { x.TenantId, x.DriverId });
        builder.HasIndex(x => new { x.TenantId, x.VehicleId });
        builder.HasIndex(x => new { x.TenantId, x.IsActive });
        builder.HasIndex(x => new { x.TenantId, x.AssignedFromUtc });

        // Relationships
        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Vehicle)
            .WithMany()
            .HasForeignKey(x => x.VehicleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
