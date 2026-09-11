using ConnectedOps.Domain.Telematics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConnectedOps.Infrastructure.Persistence.Configurations;

public sealed class TrackingDeviceVehicleAssignmentConfiguration : IEntityTypeConfiguration<TrackingDeviceVehicleAssignment>
{
    public void Configure(EntityTypeBuilder<TrackingDeviceVehicleAssignment> builder)
    {
        builder.ToTable("TrackingDeviceVehicleAssignments");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.TenantId)
            .IsRequired();

        builder.Property(x => x.TrackingDeviceId)
            .IsRequired();

        builder.Property(x => x.VehicleId)
            .IsRequired();

        builder.Property(x => x.AssignedFromUtc)
            .IsRequired();

        builder.Property(x => x.IsPrimary)
            .IsRequired();

        builder.Property(x => x.IsActive)
            .IsRequired();

        builder.Property(x => x.Notes)
            .HasMaxLength(1000);

        builder.HasOne(x => x.TrackingDevice)
            .WithMany()
            .HasForeignKey(x => x.TrackingDeviceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Vehicle)
            .WithMany()
            .HasForeignKey(x => x.VehicleId)
            .OnDelete(DeleteBehavior.Restrict);

        // Fast active lookups
        builder.HasIndex(x => new { x.TenantId, x.TrackingDeviceId, x.IsActive });
        builder.HasIndex(x => new { x.TenantId, x.VehicleId, x.IsActive });
        builder.HasIndex(x => new { x.TenantId, x.VehicleId, x.IsPrimary, x.IsActive });
    }
}
