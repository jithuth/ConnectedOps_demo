using ConnectedOps.Domain.Telematics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConnectedOps.Infrastructure.Persistence.Configurations;

public sealed class TelemetryRecordConfiguration : IEntityTypeConfiguration<TelemetryRecord>
{
    public void Configure(EntityTypeBuilder<TelemetryRecord> builder)
    {
        builder.ToTable("TelemetryRecords");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.TenantId)
            .IsRequired();

        builder.Property(x => x.TrackingDeviceId)
            .IsRequired();

        builder.Property(x => x.RecordedAtUtc)
            .IsRequired();

        builder.Property(x => x.ReceivedAtUtc)
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

        builder.Property(x => x.FuelVolumeLiters)
            .HasPrecision(8, 2);

        builder.Property(x => x.BatteryVoltage)
            .HasPrecision(6, 2);

        builder.Property(x => x.ExternalPowerVoltage)
            .HasPrecision(6, 2);

        builder.Property(x => x.TemperatureCelsius)
            .HasPrecision(6, 2);

        builder.Property(x => x.EventType)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.SourceProvider)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.ProviderMessageId)
            .HasMaxLength(100);

        builder.Property(x => x.CreatedAtUtc)
            .IsRequired();

        builder.HasOne(x => x.TrackingDevice)
            .WithMany()
            .HasForeignKey(x => x.TrackingDeviceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Vehicle)
            .WithMany()
            .HasForeignKey(x => x.VehicleId)
            .OnDelete(DeleteBehavior.SetNull);

        // High volume performance indexes
        builder.HasIndex(x => new { x.TenantId, x.VehicleId, x.RecordedAtUtc });
        builder.HasIndex(x => new { x.TenantId, x.TrackingDeviceId, x.RecordedAtUtc });
        builder.HasIndex(x => new { x.TrackingDeviceId, x.RecordedAtUtc });
        builder.HasIndex(x => new { x.TenantId, x.RecordedAtUtc });
        builder.HasIndex(x => new { x.TenantId, x.ProviderMessageId })
            .HasFilter("[ProviderMessageId] IS NOT NULL");
    }
}
