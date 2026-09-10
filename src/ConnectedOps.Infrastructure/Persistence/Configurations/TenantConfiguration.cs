using ConnectedOps.Domain.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConnectedOps.Infrastructure.Persistence.Configurations;

public sealed class TenantConfiguration :
    IEntityTypeConfiguration<Tenant>
{
    public void Configure(
        EntityTypeBuilder<Tenant> builder)
    {
        builder.ToTable("Tenants");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.Code)
            .HasMaxLength(50)
            .IsRequired();

        builder.HasIndex(x => x.Code)
            .IsUnique();

        builder.Property(x => x.Email)
            .HasMaxLength(255);

        builder.Property(x => x.Phone)
            .HasMaxLength(50);

        builder.Property(x => x.CountryCode)
            .HasMaxLength(10);

        builder.Property(x => x.Status)
            .HasConversion<int>()
            .IsRequired();

        builder.HasQueryFilter(x => !x.IsDeleted);

        builder.Ignore(x => x.IsActive);
    }
}