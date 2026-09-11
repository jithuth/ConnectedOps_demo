using ConnectedOps.Domain.Drivers;
using ConnectedOps.Domain.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConnectedOps.Infrastructure.Persistence.Configurations;

public sealed class DriverCertificationConfiguration : IEntityTypeConfiguration<DriverCertification>
{
    public void Configure(EntityTypeBuilder<DriverCertification> builder)
    {
        builder.ToTable("DriverCertifications");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.TenantId)
            .IsRequired();

        builder.Property(x => x.DriverId)
            .IsRequired();

        builder.Property(x => x.CertificationType)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.Title)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.CertificateNumber)
            .HasMaxLength(100);

        builder.Property(x => x.IssuedBy)
            .HasMaxLength(150);

        builder.Property(x => x.FileObjectKey)
            .HasMaxLength(500);

        builder.Property(x => x.Notes)
            .HasMaxLength(1000);

        builder.Property(x => x.IsActive)
            .IsRequired();

        builder.HasIndex(x => new { x.TenantId, x.DriverId });
        builder.HasIndex(x => new { x.TenantId, x.ExpiryDate });

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
