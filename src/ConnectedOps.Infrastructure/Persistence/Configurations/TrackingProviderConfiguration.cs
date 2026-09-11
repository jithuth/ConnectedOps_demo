using ConnectedOps.Domain.Telematics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConnectedOps.Infrastructure.Persistence.Configurations;

public sealed class TrackingProviderConfiguration : IEntityTypeConfiguration<TrackingProvider>
{
    public void Configure(EntityTypeBuilder<TrackingProvider> builder)
    {
        builder.ToTable("TrackingProviders");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.Code)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.ProviderType)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.Description)
            .HasMaxLength(500);

        builder.Property(x => x.IsActive)
            .IsRequired();

        builder.HasIndex(x => x.Code)
            .IsUnique();

        builder.HasIndex(x => x.TenantId);
    }
}
