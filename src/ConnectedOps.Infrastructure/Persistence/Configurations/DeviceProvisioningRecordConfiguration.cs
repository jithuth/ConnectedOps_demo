using ConnectedOps.Domain.Telematics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConnectedOps.Infrastructure.Persistence.Configurations;

public sealed class DeviceProvisioningRecordConfiguration : IEntityTypeConfiguration<DeviceProvisioningRecord>
{
    public void Configure(EntityTypeBuilder<DeviceProvisioningRecord> builder)
    {
        builder.ToTable("DeviceProvisioningRecords");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.TenantId)
            .IsRequired();

        builder.Property(x => x.TrackingDeviceId)
            .IsRequired();

        builder.Property(x => x.ProvisioningStatus)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.ProviderReference)
            .HasMaxLength(150);

        builder.Property(x => x.Notes)
            .HasMaxLength(1000);

        builder.HasOne(x => x.TrackingDevice)
            .WithMany()
            .HasForeignKey(x => x.TrackingDeviceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.TenantId, x.TrackingDeviceId });
        builder.HasIndex(x => new { x.TenantId, x.ProvisioningStatus });
    }
}
