using ConnectedOps.Domain.Platform;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConnectedOps.Infrastructure.Persistence.Configurations;

public sealed class PlatformSettingsConfiguration : IEntityTypeConfiguration<PlatformSettings>
{
    public void Configure(EntityTypeBuilder<PlatformSettings> builder)
    {
        builder.ToTable("PlatformSettings");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.PlatformName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.CompanyName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.SupportEmail)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.DefaultLanguage)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.DefaultTimeZone)
            .HasMaxLength(100)
            .IsRequired();

        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
