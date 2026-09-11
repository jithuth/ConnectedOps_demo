using ConnectedOps.Domain.Assets;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConnectedOps.Infrastructure.Persistence.Configurations;

public sealed class AssetCategoryConfiguration : IEntityTypeConfiguration<AssetCategory>
{
    public void Configure(EntityTypeBuilder<AssetCategory> builder)
    {
        builder.ToTable("AssetCategories");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Code).IsRequired().HasMaxLength(50);
        builder.Property(x => x.Name).IsRequired().HasMaxLength(100);
        builder.Property(x => x.Description).HasMaxLength(500);

        builder.HasOne(x => x.ParentCategory)
            .WithMany(x => x.SubCategories)
            .HasForeignKey(x => x.ParentCategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.TenantId, x.Code }).IsUnique();
    }
}

public sealed class AssetTypeConfiguration : IEntityTypeConfiguration<AssetType>
{
    public void Configure(EntityTypeBuilder<AssetType> builder)
    {
        builder.ToTable("AssetTypes");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Code).IsRequired().HasMaxLength(50);
        builder.Property(x => x.Name).IsRequired().HasMaxLength(100);
        builder.Property(x => x.Description).HasMaxLength(500);

        builder.HasOne(x => x.AssetCategory)
            .WithMany()
            .HasForeignKey(x => x.AssetCategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.TenantId, x.Code }).IsUnique();
        builder.HasIndex(x => new { x.TenantId, x.AssetCategoryId });
    }
}

public sealed class AssetConfiguration : IEntityTypeConfiguration<Asset>
{
    public void Configure(EntityTypeBuilder<Asset> builder)
    {
        builder.ToTable("Assets");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.AssetNumber).IsRequired().HasMaxLength(50);
        builder.Property(x => x.Name).IsRequired().HasMaxLength(150);
        builder.Property(x => x.Description).HasMaxLength(1000);
        builder.Property(x => x.SerialNumber).HasMaxLength(100);
        builder.Property(x => x.InternalCode).HasMaxLength(50);
        builder.Property(x => x.Manufacturer).HasMaxLength(100);
        builder.Property(x => x.Model).HasMaxLength(100);
        builder.Property(x => x.SupplierName).HasMaxLength(200);
        builder.Property(x => x.PurchaseReference).HasMaxLength(100);
        builder.Property(x => x.CurrencyCode).HasMaxLength(10);
        builder.Property(x => x.WarrantyProvider).HasMaxLength(150);
        builder.Property(x => x.WarrantyReference).HasMaxLength(100);
        builder.Property(x => x.PrimaryImageObjectKey).HasMaxLength(500);
        builder.Property(x => x.Notes).HasMaxLength(2000);

        builder.Property(x => x.PurchaseCost).HasPrecision(18, 2);

        builder.HasOne(x => x.AssetCategory)
            .WithMany()
            .HasForeignKey(x => x.AssetCategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.AssetType)
            .WithMany()
            .HasForeignKey(x => x.AssetTypeId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(x => x.Branch)
            .WithMany()
            .HasForeignKey(x => x.BranchId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(x => x.Location)
            .WithMany()
            .HasForeignKey(x => x.LocationId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(x => x.Department)
            .WithMany()
            .HasForeignKey(x => x.DepartmentId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(x => x.Team)
            .WithMany()
            .HasForeignKey(x => x.TeamId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(x => x.CurrentCustodianEmployee)
            .WithMany()
            .HasForeignKey(x => x.CurrentCustodianEmployeeId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(x => x.CurrentAssignedVehicle)
            .WithMany()
            .HasForeignKey(x => x.CurrentAssignedVehicleId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(x => new { x.TenantId, x.AssetNumber }).IsUnique();
        builder.HasIndex(x => new { x.TenantId, x.SerialNumber }).HasFilter("[SerialNumber] IS NOT NULL");
        builder.HasIndex(x => new { x.TenantId, x.InternalCode }).HasFilter("[InternalCode] IS NOT NULL");
        builder.HasIndex(x => new { x.TenantId, x.Status });
        builder.HasIndex(x => new { x.TenantId, x.AssetCategoryId });
        builder.HasIndex(x => new { x.TenantId, x.AssetTypeId });
        builder.HasIndex(x => new { x.TenantId, x.BranchId });
        builder.HasIndex(x => new { x.TenantId, x.CurrentCustodianEmployeeId });
        builder.HasIndex(x => new { x.TenantId, x.CurrentAssignedVehicleId });
    }
}

public sealed class AssetLocationHistoryConfiguration : IEntityTypeConfiguration<AssetLocationHistory>
{
    public void Configure(EntityTypeBuilder<AssetLocationHistory> builder)
    {
        builder.ToTable("AssetLocationHistories");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Reason).HasMaxLength(500);

        builder.HasOne(x => x.Asset)
            .WithMany(x => x.LocationHistories)
            .HasForeignKey(x => x.AssetId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Branch)
            .WithMany()
            .HasForeignKey(x => x.BranchId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(x => x.Location)
            .WithMany()
            .HasForeignKey(x => x.LocationId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(x => new { x.TenantId, x.AssetId });
    }
}

public sealed class AssetEmployeeAssignmentConfiguration : IEntityTypeConfiguration<AssetEmployeeAssignment>
{
    public void Configure(EntityTypeBuilder<AssetEmployeeAssignment> builder)
    {
        builder.ToTable("AssetEmployeeAssignments");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Notes).HasMaxLength(1000);

        builder.HasOne(x => x.Asset)
            .WithMany(x => x.EmployeeAssignments)
            .HasForeignKey(x => x.AssetId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Employee)
            .WithMany()
            .HasForeignKey(x => x.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.TenantId, x.AssetId });
        builder.HasIndex(x => new { x.TenantId, x.EmployeeId });
        builder.HasIndex(x => new { x.TenantId, x.IsActive });
    }
}

public sealed class AssetVehicleAssignmentConfiguration : IEntityTypeConfiguration<AssetVehicleAssignment>
{
    public void Configure(EntityTypeBuilder<AssetVehicleAssignment> builder)
    {
        builder.ToTable("AssetVehicleAssignments");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Notes).HasMaxLength(1000);

        builder.HasOne(x => x.Asset)
            .WithMany(x => x.VehicleAssignments)
            .HasForeignKey(x => x.AssetId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Vehicle)
            .WithMany()
            .HasForeignKey(x => x.VehicleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.TenantId, x.AssetId });
        builder.HasIndex(x => new { x.TenantId, x.VehicleId });
        builder.HasIndex(x => new { x.TenantId, x.IsActive });
    }
}

public sealed class AssetTransferConfiguration : IEntityTypeConfiguration<AssetTransfer>
{
    public void Configure(EntityTypeBuilder<AssetTransfer> builder)
    {
        builder.ToTable("AssetTransfers");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Reason).HasMaxLength(500);
        builder.Property(x => x.Notes).HasMaxLength(1000);

        builder.HasOne(x => x.Asset)
            .WithMany(x => x.Transfers)
            .HasForeignKey(x => x.AssetId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.FromBranch)
            .WithMany()
            .HasForeignKey(x => x.FromBranchId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.ToBranch)
            .WithMany()
            .HasForeignKey(x => x.ToBranchId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.FromLocation)
            .WithMany()
            .HasForeignKey(x => x.FromLocationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.ToLocation)
            .WithMany()
            .HasForeignKey(x => x.ToLocationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.FromEmployee)
            .WithMany()
            .HasForeignKey(x => x.FromEmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.ToEmployee)
            .WithMany()
            .HasForeignKey(x => x.ToEmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.FromVehicle)
            .WithMany()
            .HasForeignKey(x => x.FromVehicleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.ToVehicle)
            .WithMany()
            .HasForeignKey(x => x.ToVehicleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.TenantId, x.AssetId });
        builder.HasIndex(x => new { x.TenantId, x.Status });
    }
}

public sealed class AssetUsageSessionConfiguration : IEntityTypeConfiguration<AssetUsageSession>
{
    public void Configure(EntityTypeBuilder<AssetUsageSession> builder)
    {
        builder.ToTable("AssetUsageSessions");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Purpose).HasMaxLength(500);
        builder.Property(x => x.Reference).HasMaxLength(100);
        builder.Property(x => x.Notes).HasMaxLength(1000);

        builder.HasOne(x => x.Asset)
            .WithMany(x => x.UsageSessions)
            .HasForeignKey(x => x.AssetId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Employee)
            .WithMany()
            .HasForeignKey(x => x.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Vehicle)
            .WithMany()
            .HasForeignKey(x => x.VehicleId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(x => new { x.TenantId, x.AssetId });
        builder.HasIndex(x => new { x.TenantId, x.EmployeeId });
        builder.HasIndex(x => new { x.TenantId, x.Status });
    }
}

public sealed class AssetConditionRecordConfiguration : IEntityTypeConfiguration<AssetConditionRecord>
{
    public void Configure(EntityTypeBuilder<AssetConditionRecord> builder)
    {
        builder.ToTable("AssetConditionRecords");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Description).HasMaxLength(1000);
        builder.Property(x => x.PhotoObjectKey).HasMaxLength(500);

        builder.HasOne(x => x.Asset)
            .WithMany(x => x.ConditionRecords)
            .HasForeignKey(x => x.AssetId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Employee)
            .WithMany()
            .HasForeignKey(x => x.EmployeeId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(x => x.UsageSession)
            .WithMany()
            .HasForeignKey(x => x.UsageSessionId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(x => new { x.TenantId, x.AssetId });
    }
}

public sealed class AssetInspectionConfiguration : IEntityTypeConfiguration<AssetInspection>
{
    public void Configure(EntityTypeBuilder<AssetInspection> builder)
    {
        builder.ToTable("AssetInspections");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Notes).HasMaxLength(2000);

        builder.HasOne(x => x.Asset)
            .WithMany(x => x.Inspections)
            .HasForeignKey(x => x.AssetId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.InspectorEmployee)
            .WithMany()
            .HasForeignKey(x => x.InspectorEmployeeId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(x => x.Items)
            .WithOne(x => x.AssetInspection)
            .HasForeignKey(x => x.AssetInspectionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.TenantId, x.AssetId });
        builder.HasIndex(x => new { x.TenantId, x.Status });
    }
}

public sealed class AssetInspectionItemConfiguration : IEntityTypeConfiguration<AssetInspectionItem>
{
    public void Configure(EntityTypeBuilder<AssetInspectionItem> builder)
    {
        builder.ToTable("AssetInspectionItems");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Description).HasMaxLength(500);
        builder.Property(x => x.Notes).HasMaxLength(1000);

        builder.HasIndex(x => new { x.TenantId, x.AssetInspectionId });
    }
}

public sealed class AssetCalibrationRecordConfiguration : IEntityTypeConfiguration<AssetCalibrationRecord>
{
    public void Configure(EntityTypeBuilder<AssetCalibrationRecord> builder)
    {
        builder.ToTable("AssetCalibrationRecords");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.CertificateNumber).HasMaxLength(100);
        builder.Property(x => x.Provider).HasMaxLength(150);
        builder.Property(x => x.Result).HasMaxLength(50);
        builder.Property(x => x.DocumentObjectKey).HasMaxLength(500);
        builder.Property(x => x.Notes).HasMaxLength(1000);

        builder.HasOne(x => x.Asset)
            .WithMany(x => x.CalibrationRecords)
            .HasForeignKey(x => x.AssetId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.TenantId, x.AssetId });
    }
}

public sealed class AssetDocumentConfiguration : IEntityTypeConfiguration<AssetDocument>
{
    public void Configure(EntityTypeBuilder<AssetDocument> builder)
    {
        builder.ToTable("AssetDocuments");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Title).IsRequired().HasMaxLength(200);
        builder.Property(x => x.DocumentNumber).HasMaxLength(100);
        builder.Property(x => x.FileName).IsRequired().HasMaxLength(255);
        builder.Property(x => x.ContentType).HasMaxLength(100);
        builder.Property(x => x.FileObjectKey).IsRequired().HasMaxLength(500);
        builder.Property(x => x.Notes).HasMaxLength(1000);

        builder.HasOne(x => x.Asset)
            .WithMany(x => x.Documents)
            .HasForeignKey(x => x.AssetId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.TenantId, x.AssetId });
    }
}

public sealed class AssetIdentifierConfiguration : IEntityTypeConfiguration<AssetIdentifier>
{
    public void Configure(EntityTypeBuilder<AssetIdentifier> builder)
    {
        builder.ToTable("AssetIdentifiers");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Value).IsRequired().HasMaxLength(200);
        builder.Property(x => x.PublicToken).IsRequired().HasMaxLength(100);

        builder.HasOne(x => x.Asset)
            .WithMany(x => x.Identifiers)
            .HasForeignKey(x => x.AssetId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.TenantId, x.AssetId });
        builder.HasIndex(x => x.PublicToken).IsUnique();
    }
}

public sealed class AssetNoteConfiguration : IEntityTypeConfiguration<AssetNote>
{
    public void Configure(EntityTypeBuilder<AssetNote> builder)
    {
        builder.ToTable("AssetNotes");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.NoteText).IsRequired().HasMaxLength(2000);
        builder.Property(x => x.CreatedByUserName).HasMaxLength(150);

        builder.HasOne(x => x.Asset)
            .WithMany(x => x.NotesList)
            .HasForeignKey(x => x.AssetId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.TenantId, x.AssetId });
    }
}
