using ConnectedOps.Domain.Telematics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConnectedOps.Infrastructure.Persistence.Configurations;

public sealed class TelematicsProviderConfigurationConfiguration : IEntityTypeConfiguration<TelematicsProviderConfiguration>
{
    public void Configure(EntityTypeBuilder<TelematicsProviderConfiguration> builder)
    {
        builder.ToTable("TelematicsProviderConfigurations");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.TenantId)
            .IsRequired();

        builder.Property(x => x.ProviderId)
            .IsRequired();

        builder.Property(x => x.Endpoint)
            .HasMaxLength(250);

        builder.Property(x => x.Username)
            .HasMaxLength(100);

        builder.Property(x => x.SecretReference)
            .HasMaxLength(250);

        builder.Property(x => x.ApiKeyHash)
            .HasMaxLength(250);

        builder.Property(x => x.IsActive)
            .IsRequired();

        builder.HasOne(x => x.Provider)
            .WithMany()
            .HasForeignKey(x => x.ProviderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.TenantId, x.ProviderId });
    }
}
