using ConnectedOps.Domain.Geofences;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConnectedOps.Infrastructure.Persistence.Configurations;

public sealed class VehicleGeofenceStateConfiguration : IEntityTypeConfiguration<VehicleGeofenceState>
{
    public void Configure(EntityTypeBuilder<VehicleGeofenceState> builder)
    {
        builder.ToTable("VehicleGeofenceStates");

        builder.HasKey(x => x.Id);

        builder.HasIndex(x => new { x.TenantId, x.VehicleId, x.GeofenceId })
            .IsUnique();

        builder.HasIndex(x => new { x.TenantId, x.GeofenceId, x.IsInside });

        builder.HasIndex(x => new { x.TenantId, x.VehicleId, x.IsInside });

        builder.HasOne(x => x.Vehicle)
            .WithMany()
            .HasForeignKey(x => x.VehicleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Geofence)
            .WithMany()
            .HasForeignKey(x => x.GeofenceId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
