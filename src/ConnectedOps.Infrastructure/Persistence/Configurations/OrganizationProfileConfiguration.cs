using ConnectedOps.Domain.Organization;
using ConnectedOps.Domain.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConnectedOps.Infrastructure.Persistence.Configurations;

public sealed class OrganizationProfileConfiguration : IEntityTypeConfiguration<OrganizationProfile>
{
    public void Configure(EntityTypeBuilder<OrganizationProfile> builder)
    {
        builder.ToTable("OrganizationProfiles");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.TenantId)
            .IsRequired();

        builder.Property(x => x.LegalName)
            .HasMaxLength(250)
            .IsRequired();

        builder.Property(x => x.TradeName)
            .HasMaxLength(250);

        builder.Property(x => x.RegistrationNumber)
            .HasMaxLength(100);

        builder.Property(x => x.TaxNumber)
            .HasMaxLength(100);

        builder.Property(x => x.Website)
            .HasMaxLength(250);

        builder.Property(x => x.PrimaryContactEmail)
            .HasMaxLength(250);

        builder.Property(x => x.PrimaryContactPhone)
            .HasMaxLength(50);

        builder.Property(x => x.AddressLine1)
            .HasMaxLength(250);

        builder.Property(x => x.AddressLine2)
            .HasMaxLength(250);

        builder.Property(x => x.City)
            .HasMaxLength(100);

        builder.Property(x => x.StateOrProvince)
            .HasMaxLength(100);

        builder.Property(x => x.PostalCode)
            .HasMaxLength(20);

        builder.Property(x => x.CountryCode)
            .HasMaxLength(10);

        builder.Property(x => x.CurrencyCode)
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(x => x.TimeZoneId)
            .HasMaxLength(100)
            .IsRequired();

        builder.HasIndex(x => x.TenantId)
            .IsUnique();

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
