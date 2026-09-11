using ConnectedOps.Domain.Drivers;
using ConnectedOps.Domain.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConnectedOps.Infrastructure.Persistence.Configurations;

public sealed class DriverLicenseCategoryConfiguration : IEntityTypeConfiguration<DriverLicenseCategory>
{
    public void Configure(EntityTypeBuilder<DriverLicenseCategory> builder)
    {
        builder.ToTable("DriverLicenseCategories");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.TenantId)
            .IsRequired();

        builder.Property(x => x.DriverLicenseId)
            .IsRequired();

        builder.Property(x => x.CategoryCode)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.Description)
            .HasMaxLength(250);

        builder.Property(x => x.IsActive)
            .IsRequired();

        // Unique category per license
        builder.HasIndex(x => new { x.TenantId, x.DriverLicenseId, x.CategoryCode })
            .IsUnique();

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
