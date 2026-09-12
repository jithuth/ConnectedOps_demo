using ConnectedOps.Domain.Compliance;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConnectedOps.Infrastructure.Persistence.Configurations;

public sealed class ComplianceRequirementConfiguration : IEntityTypeConfiguration<ComplianceRequirement>
{
    public void Configure(EntityTypeBuilder<ComplianceRequirement> builder)
    {
        builder.ToTable("ComplianceRequirements");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Code).IsRequired().HasMaxLength(50);
        builder.Property(x => x.Name).IsRequired().HasMaxLength(150);
        builder.Property(x => x.Description).HasMaxLength(1000);

        builder.HasIndex(x => new { x.TenantId, x.Code }).IsUnique();
        builder.HasIndex(x => new { x.TenantId, x.AppliesTo, x.IsActive });
    }
}

public sealed class ComplianceRequirementRuleConfiguration : IEntityTypeConfiguration<ComplianceRequirementRule>
{
    public void Configure(EntityTypeBuilder<ComplianceRequirementRule> builder)
    {
        builder.ToTable("ComplianceRequirementRules");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.CountryCode).HasMaxLength(10);
        builder.Property(x => x.Notes).HasMaxLength(500);

        builder.HasOne(x => x.ComplianceRequirement)
            .WithMany(x => x.Rules)
            .HasForeignKey(x => x.ComplianceRequirementId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.VehicleCategory)
            .WithMany()
            .HasForeignKey(x => x.VehicleCategoryId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(x => x.AssetCategory)
            .WithMany()
            .HasForeignKey(x => x.AssetCategoryId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(x => x.Branch)
            .WithMany()
            .HasForeignKey(x => x.BranchId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(x => new { x.TenantId, x.ComplianceRequirementId });
    }
}

public sealed class ComplianceRecordConfiguration : IEntityTypeConfiguration<ComplianceRecord>
{
    public void Configure(EntityTypeBuilder<ComplianceRecord> builder)
    {
        builder.ToTable("ComplianceRecords");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.ReferenceNumber).HasMaxLength(100);
        builder.Property(x => x.Notes).HasMaxLength(2000);

        builder.HasOne(x => x.ComplianceRequirement)
            .WithMany(x => x.Records)
            .HasForeignKey(x => x.ComplianceRequirementId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Vehicle)
            .WithMany()
            .HasForeignKey(x => x.VehicleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Driver)
            .WithMany()
            .HasForeignKey(x => x.DriverId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Asset)
            .WithMany()
            .HasForeignKey(x => x.AssetId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.TenantId, x.SubjectType });
        builder.HasIndex(x => new { x.TenantId, x.VehicleId });
        builder.HasIndex(x => new { x.TenantId, x.DriverId });
        builder.HasIndex(x => new { x.TenantId, x.AssetId });
        builder.HasIndex(x => new { x.TenantId, x.ExpiryDateUtc });
        builder.HasIndex(x => new { x.TenantId, x.Status });
    }
}

public sealed class ComplianceDocumentConfiguration : IEntityTypeConfiguration<ComplianceDocument>
{
    public void Configure(EntityTypeBuilder<ComplianceDocument> builder)
    {
        builder.ToTable("ComplianceDocuments");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.DocumentType).IsRequired().HasMaxLength(50);
        builder.Property(x => x.Title).IsRequired().HasMaxLength(150);
        builder.Property(x => x.FileObjectKey).IsRequired().HasMaxLength(500);
        builder.Property(x => x.FileName).IsRequired().HasMaxLength(255);
        builder.Property(x => x.ContentType).IsRequired().HasMaxLength(100);
        builder.Property(x => x.Notes).HasMaxLength(1000);

        builder.HasOne(x => x.ComplianceRecord)
            .WithMany(x => x.Documents)
            .HasForeignKey(x => x.ComplianceRecordId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.TenantId, x.ComplianceRecordId });
    }
}

public sealed class ComplianceExceptionConfiguration : IEntityTypeConfiguration<ComplianceException>
{
    public void Configure(EntityTypeBuilder<ComplianceException> builder)
    {
        builder.ToTable("ComplianceExceptions");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Reason).IsRequired().HasMaxLength(500);

        builder.HasOne(x => x.ComplianceRequirement)
            .WithMany()
            .HasForeignKey(x => x.ComplianceRequirementId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Vehicle)
            .WithMany()
            .HasForeignKey(x => x.VehicleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Driver)
            .WithMany()
            .HasForeignKey(x => x.DriverId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Asset)
            .WithMany()
            .HasForeignKey(x => x.AssetId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.TenantId, x.ComplianceRequirementId });
        builder.HasIndex(x => new { x.TenantId, x.SubjectType, x.Status });
    }
}
