using ConnectedOps.Domain.Drivers;
using ConnectedOps.Domain.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConnectedOps.Infrastructure.Persistence.Configurations;

public sealed class DriverDocumentConfiguration : IEntityTypeConfiguration<DriverDocument>
{
    public void Configure(EntityTypeBuilder<DriverDocument> builder)
    {
        builder.ToTable("DriverDocuments");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.TenantId)
            .IsRequired();

        builder.Property(x => x.DriverId)
            .IsRequired();

        builder.Property(x => x.DocumentType)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.Title)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.DocumentNumber)
            .HasMaxLength(100);

        builder.Property(x => x.IssuingAuthority)
            .HasMaxLength(150);

        builder.Property(x => x.FileObjectKey)
            .HasMaxLength(500);

        builder.Property(x => x.FileName)
            .HasMaxLength(255);

        builder.Property(x => x.ContentType)
            .HasMaxLength(100);

        builder.Property(x => x.Notes)
            .HasMaxLength(1000);

        builder.Property(x => x.IsActive)
            .IsRequired();

        builder.HasIndex(x => new { x.TenantId, x.DriverId });
        builder.HasIndex(x => new { x.TenantId, x.ExpiryDate });
        builder.HasIndex(x => new { x.TenantId, x.DocumentType });

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
