using ConnectedOps.Domain.Telematics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConnectedOps.Infrastructure.Persistence.Configurations;

public sealed class TelematicsSettingsConfiguration : IEntityTypeConfiguration<TelematicsSettings>
{
    public void Configure(EntityTypeBuilder<TelematicsSettings> builder)
    {
        builder.ToTable("TelematicsSettings");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.TenantId)
            .IsRequired();

        builder.Property(x => x.OfflineThresholdMinutes)
            .IsRequired();

        builder.Property(x => x.TelemetryRetentionDays)
            .IsRequired();

        builder.Property(x => x.MaxAcceptedFutureMinutes)
            .IsRequired();

        builder.Property(x => x.MaxAcceptedPastDays)
            .IsRequired();

        builder.Property(x => x.OdometerUpdateThresholdKm)
            .HasPrecision(8, 2)
            .IsRequired();

        builder.Property(x => x.OdometerUpdateMinIntervalMinutes)
            .IsRequired();

        builder.HasIndex(x => x.TenantId)
            .IsUnique();
    }
}
