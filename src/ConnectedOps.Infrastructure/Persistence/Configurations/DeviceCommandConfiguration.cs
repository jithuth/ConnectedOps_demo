using ConnectedOps.Domain.Telematics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConnectedOps.Infrastructure.Persistence.Configurations;

public sealed class DeviceCommandConfiguration : IEntityTypeConfiguration<DeviceCommand>
{
    public void Configure(EntityTypeBuilder<DeviceCommand> builder)
    {
        builder.ToTable("DeviceCommands");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.TenantId)
            .IsRequired();

        builder.Property(x => x.TrackingDeviceId)
            .IsRequired();

        builder.Property(x => x.CommandType)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.ParametersJson)
            .HasMaxLength(2000);

        builder.Property(x => x.Status)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.ResponsePayload)
            .HasMaxLength(2000);

        builder.Property(x => x.FailureReason)
            .HasMaxLength(500);

        builder.HasOne(x => x.TrackingDevice)
            .WithMany()
            .HasForeignKey(x => x.TrackingDeviceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.TenantId, x.TrackingDeviceId });
        builder.HasIndex(x => new { x.TenantId, x.Status });
    }
}
