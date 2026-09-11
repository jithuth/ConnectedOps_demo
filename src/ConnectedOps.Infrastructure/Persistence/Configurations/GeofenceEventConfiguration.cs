using ConnectedOps.Domain.Geofences;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConnectedOps.Infrastructure.Persistence.Configurations;

public sealed class GeofenceEventConfiguration : IEntityTypeConfiguration<GeofenceEvent>
{
    public void Configure(EntityTypeBuilder<GeofenceEvent> builder)
    {
        builder.ToTable("GeofenceEvents");

        builder.HasKey(x => x.Id);

        builder.HasIndex(x => new { x.TenantId, x.OccurredAtUtc });

        builder.HasIndex(x => new { x.TenantId, x.GeofenceId, x.OccurredAtUtc });

        builder.HasIndex(x => new { x.TenantId, x.VehicleId, x.OccurredAtUtc });

        builder.HasOne(x => x.Vehicle)
            .WithMany()
            .HasForeignKey(x => x.VehicleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Geofence)
            .WithMany()
            .HasForeignKey(x => x.GeofenceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.TrackingDevice)
            .WithMany()
            .HasForeignKey(x => x.TrackingDeviceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Driver)
            .WithMany()
            .HasForeignKey(x => x.DriverId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.TelemetryRecord)
            .WithMany()
            .HasForeignKey(x => x.TelemetryRecordId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
