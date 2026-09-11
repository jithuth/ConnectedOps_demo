using ConnectedOps.Domain.Drivers;
using ConnectedOps.Domain.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConnectedOps.Infrastructure.Persistence.Configurations;

public sealed class DriverLicenseConfiguration : IEntityTypeConfiguration<DriverLicense>
{
    public void Configure(EntityTypeBuilder<DriverLicense> builder)
    {
        builder.ToTable("DriverLicenses");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.TenantId)
            .IsRequired();

        builder.Property(x => x.DriverId)
            .IsRequired();

        builder.Property(x => x.LicenseNumber)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.LicenseCountryCode)
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(x => x.IssuingAuthority)
            .HasMaxLength(150);

        builder.Property(x => x.Notes)
            .HasMaxLength(1000);

        builder.Property(x => x.IsPrimary)
            .IsRequired();

        builder.Property(x => x.IsActive)
            .IsRequired();

        // Indexes
        builder.HasIndex(x => new { x.TenantId, x.DriverId });
        builder.HasIndex(x => new { x.TenantId, x.LicenseNumber });
        builder.HasIndex(x => new { x.TenantId, x.ExpiryDate });

        // Relationships
        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Categories)
            .WithOne(x => x.DriverLicense)
            .HasForeignKey(x => x.DriverLicenseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
