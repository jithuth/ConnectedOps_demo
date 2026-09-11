using ConnectedOps.Domain.Telematics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConnectedOps.Infrastructure.Persistence.Configurations;

public sealed class VehicleTelemetryStateConfiguration : IEntityTypeConfiguration<VehicleTelemetryState>
{
    public void Configure(EntityTypeBuilder<VehicleTelemetryState> builder)
    {
        builder.ToTable("VehicleTelemetryStates");

        builder.HasKey(x => x.VehicleId);

        builder.Property(x => x.TenantId)
            .IsRequired();

        builder.Property(x => x.TrackingDeviceId)
            .IsRequired();

        builder.Property(x => x.RecordedAtUtc)
            .IsRequired();

        builder.Property(x => x.ReceivedAtUtc)
            .IsRequired();

        builder.Property(x => x.Latitude)
            .IsRequired();

        builder.Property(x => x.Longitude)
            .IsRequired();

        builder.Property(x => x.SpeedKph)
            .HasPrecision(8, 2);

        builder.Property(x => x.HeadingDegrees)
            .HasPrecision(5, 2);

        builder.Property(x => x.OdometerKm)
            .HasPrecision(18, 2);

        builder.Property(x => x.EngineHours)
            .HasPrecision(10, 2);

        builder.Property(x => x.FuelLevelPercent)
            .HasPrecision(5, 2);

        builder.Property(x => x.BatteryVoltage)
            .HasPrecision(6, 2);

        builder.Property(x => x.ExternalPowerVoltage)
            .HasPrecision(6, 2);

        builder.Property(x => x.LastUpdatedAtUtc)
            .IsRequired();

        builder.HasOne(x => x.Vehicle)
            .WithOne()
            .HasForeignKey<VehicleTelemetryState>(x => x.VehicleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.TrackingDevice)
            .WithMany()
            .HasForeignKey(x => x.TrackingDeviceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.TenantId, x.VehicleId })
            .IsUnique();

        builder.HasIndex(x => new { x.TenantId, x.TrackingDeviceId });
    }
}
