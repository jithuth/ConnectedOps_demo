using ConnectedOps.Domain.Telematics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConnectedOps.Infrastructure.Persistence.Configurations;

public sealed class TrackingDeviceConfiguration : IEntityTypeConfiguration<TrackingDevice>
{
    public void Configure(EntityTypeBuilder<TrackingDevice> builder)
    {
        builder.ToTable("TrackingDevices");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.TenantId)
            .IsRequired();

        builder.Property(x => x.DeviceIdentifier)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.IMEI)
            .HasMaxLength(30);

        builder.Property(x => x.SerialNumber)
            .HasMaxLength(100);

        builder.Property(x => x.Name)
            .HasMaxLength(150);

        builder.Property(x => x.ProviderId)
            .IsRequired();

        builder.Property(x => x.DeviceTypeId)
            .IsRequired();

        builder.Property(x => x.Model)
            .HasMaxLength(100);

        builder.Property(x => x.Manufacturer)
            .HasMaxLength(100);

        builder.Property(x => x.FirmwareVersion)
            .HasMaxLength(50);

        builder.Property(x => x.SIMNumber)
            .HasMaxLength(50);

        builder.Property(x => x.SIMICCID)
            .HasMaxLength(50);

        builder.Property(x => x.PhoneNumber)
            .HasMaxLength(30);

        builder.Property(x => x.Status)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.LastKnownSpeedKph)
            .HasPrecision(8, 2);

        builder.Property(x => x.LastKnownHeadingDegrees)
            .HasPrecision(5, 2);

        builder.Property(x => x.ExternalPowerVoltage)
            .HasPrecision(6, 2);

        builder.Property(x => x.BatteryVoltage)
            .HasPrecision(6, 2);

        builder.Property(x => x.IsActive)
            .IsRequired();

        // Foreign keys
        builder.HasOne(x => x.Provider)
            .WithMany()
            .HasForeignKey(x => x.ProviderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.DeviceType)
            .WithMany()
            .HasForeignKey(x => x.DeviceTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        // Unique indexes
        builder.HasIndex(x => new { x.TenantId, x.DeviceIdentifier })
            .IsUnique();

        builder.HasIndex(x => new { x.TenantId, x.IMEI })
            .IsUnique()
            .HasFilter("[IMEI] IS NOT NULL");

        builder.HasIndex(x => new { x.TenantId, x.Status });
        builder.HasIndex(x => new { x.TenantId, x.ProviderId });
        builder.HasIndex(x => new { x.TenantId, x.LastSeenAtUtc });
    }
}
