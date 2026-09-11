using ConnectedOps.Domain.Telematics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConnectedOps.Infrastructure.Persistence.Configurations;

public sealed class TelemetryIngestionFailureConfiguration : IEntityTypeConfiguration<TelemetryIngestionFailure>
{
    public void Configure(EntityTypeBuilder<TelemetryIngestionFailure> builder)
    {
        builder.ToTable("TelemetryIngestionFailures");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.DeviceIdentifier)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.Provider)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.Reason)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(x => x.TraceId)
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(x => x.PayloadReference)
            .HasMaxLength(250);

        builder.Property(x => x.ReceivedAtUtc)
            .IsRequired();

        builder.Property(x => x.CreatedAtUtc)
            .IsRequired();

        builder.HasIndex(x => new { x.TenantId, x.ReceivedAtUtc });
        builder.HasIndex(x => x.DeviceIdentifier);
    }
}
