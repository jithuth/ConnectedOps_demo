using ConnectedOps.Domain.Maintenance;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConnectedOps.Infrastructure.Persistence.Configurations;

public sealed class VehicleMaintenanceDocumentConfiguration : IEntityTypeConfiguration<VehicleMaintenanceDocument>
{
    public void Configure(EntityTypeBuilder<VehicleMaintenanceDocument> builder)
    {
        builder.ToTable("VehicleMaintenanceDocuments");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.TenantId)
            .IsRequired();

        builder.Property(x => x.MaintenanceRecordId)
            .IsRequired();

        builder.Property(x => x.DocumentType)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.Title)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(x => x.FileObjectKey)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(x => x.FileName)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(x => x.ContentType)
            .HasMaxLength(100);

        builder.Property(x => x.Notes)
            .HasMaxLength(500);

        builder.HasIndex(x => new { x.TenantId, x.MaintenanceRecordId });

        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
