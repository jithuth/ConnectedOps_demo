using ConnectedOps.Domain.Fuel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConnectedOps.Infrastructure.Persistence.Configurations;

public sealed class FuelTypeDefinitionConfiguration : IEntityTypeConfiguration<FuelTypeDefinition>
{
    public void Configure(EntityTypeBuilder<FuelTypeDefinition> builder)
    {
        builder.ToTable("FuelTypeDefinitions");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Code)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.EnergyType)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.Density)
            .HasPrecision(18, 4);

        builder.HasIndex(x => new { x.TenantId, x.Code });
    }
}

public sealed class FuelStationConfiguration : IEntityTypeConfiguration<FuelStation>
{
    public void Configure(EntityTypeBuilder<FuelStation> builder)
    {
        builder.ToTable("FuelStations");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Code)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.VendorName)
            .HasMaxLength(200);

        builder.Property(x => x.Address)
            .HasMaxLength(500);

        builder.Property(x => x.ContactPhone)
            .HasMaxLength(50);

        builder.Property(x => x.Notes)
            .HasMaxLength(2000);

        builder.HasOne(x => x.Branch)
            .WithMany()
            .HasForeignKey(x => x.BranchId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(x => new { x.TenantId, x.Code })
            .IsUnique();
    }
}

public sealed class FuelCardConfiguration : IEntityTypeConfiguration<FuelCard>
{
    public void Configure(EntityTypeBuilder<FuelCard> builder)
    {
        builder.ToTable("FuelCards");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.CardNumberMasked)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.CardReference)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.ProviderName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.SpendingLimit)
            .HasPrecision(18, 2);

        builder.Property(x => x.CurrencyCode)
            .IsRequired()
            .HasMaxLength(3);

        builder.Property(x => x.Notes)
            .HasMaxLength(2000);

        builder.HasOne(x => x.Vehicle)
            .WithMany()
            .HasForeignKey(x => x.VehicleId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(x => x.Driver)
            .WithMany()
            .HasForeignKey(x => x.DriverId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(x => new { x.TenantId, x.VehicleId });
        builder.HasIndex(x => new { x.TenantId, x.DriverId });
        builder.HasIndex(x => new { x.TenantId, x.CardReference });
    }
}

public sealed class FuelTransactionConfiguration : IEntityTypeConfiguration<FuelTransaction>
{
    public void Configure(EntityTypeBuilder<FuelTransaction> builder)
    {
        builder.ToTable("FuelTransactions");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Quantity)
            .HasPrecision(18, 3);

        builder.Property(x => x.UnitPrice)
            .HasPrecision(18, 3);

        builder.Property(x => x.TotalCost)
            .HasPrecision(18, 3);

        builder.Property(x => x.OdometerReading)
            .HasPrecision(18, 2);

        builder.Property(x => x.EngineHours)
            .HasPrecision(18, 2);

        builder.Property(x => x.CurrencyCode)
            .IsRequired()
            .HasMaxLength(3);

        builder.Property(x => x.TransactionReference)
            .HasMaxLength(100);

        builder.Property(x => x.ReceiptNumber)
            .HasMaxLength(100);

        builder.Property(x => x.PaymentMethod)
            .HasMaxLength(100);

        builder.Property(x => x.ExternalTransactionId)
            .HasMaxLength(200);

        builder.Property(x => x.Notes)
            .HasMaxLength(2000);

        builder.HasOne(x => x.Vehicle)
            .WithMany()
            .HasForeignKey(x => x.VehicleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Driver)
            .WithMany()
            .HasForeignKey(x => x.DriverId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(x => x.FuelStation)
            .WithMany()
            .HasForeignKey(x => x.FuelStationId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(x => x.FuelCard)
            .WithMany()
            .HasForeignKey(x => x.FuelCardId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(x => x.Documents)
            .WithOne(x => x.FuelTransaction)
            .HasForeignKey(x => x.FuelTransactionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Anomalies)
            .WithOne(x => x.FuelTransaction)
            .HasForeignKey(x => x.FuelTransactionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.TenantId, x.VehicleId, x.TransactionDateUtc });
        builder.HasIndex(x => new { x.TenantId, x.DriverId, x.TransactionDateUtc });
        builder.HasIndex(x => new { x.TenantId, x.FuelStationId, x.TransactionDateUtc });
        builder.HasIndex(x => new { x.TenantId, x.Source });
        builder.HasIndex(x => new { x.TenantId, x.TransactionDateUtc });
        builder.HasIndex(x => new { x.TenantId, x.Source, x.ExternalTransactionId });
    }
}

public sealed class FuelTransactionDocumentConfiguration : IEntityTypeConfiguration<FuelTransactionDocument>
{
    public void Configure(EntityTypeBuilder<FuelTransactionDocument> builder)
    {
        builder.ToTable("FuelTransactionDocuments");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.FileObjectKey)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(x => x.FileName)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(x => x.ContentType)
            .HasMaxLength(100);

        builder.Property(x => x.Notes)
            .HasMaxLength(1000);

        builder.HasIndex(x => new { x.TenantId, x.FuelTransactionId });
    }
}

public sealed class FuelImportBatchConfiguration : IEntityTypeConfiguration<FuelImportBatch>
{
    public void Configure(EntityTypeBuilder<FuelImportBatch> builder)
    {
        builder.ToTable("FuelImportBatches");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.FileName)
            .HasMaxLength(255);

        builder.Property(x => x.ProviderName)
            .HasMaxLength(200);

        builder.HasMany(x => x.Errors)
            .WithOne(x => x.FuelImportBatch)
            .HasForeignKey(x => x.FuelImportBatchId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(x => x.Errors)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(x => new { x.TenantId, x.StartedAtUtc });
    }
}

public sealed class FuelImportErrorConfiguration : IEntityTypeConfiguration<FuelImportError>
{
    public void Configure(EntityTypeBuilder<FuelImportError> builder)
    {
        builder.ToTable("FuelImportErrors");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.ErrorCode)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.ErrorMessage)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(x => x.ExternalReference)
            .HasMaxLength(200);

        builder.HasIndex(x => new { x.TenantId, x.FuelImportBatchId });
    }
}

public sealed class FuelAnomalyConfiguration : IEntityTypeConfiguration<FuelAnomaly>
{
    public void Configure(EntityTypeBuilder<FuelAnomaly> builder)
    {
        builder.ToTable("FuelAnomalies");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Description)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(x => x.ResolutionNotes)
            .HasMaxLength(1000);

        builder.HasOne(x => x.Vehicle)
            .WithMany()
            .HasForeignKey(x => x.VehicleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.TenantId, x.Status, x.DetectedAtUtc });
        builder.HasIndex(x => new { x.TenantId, x.VehicleId, x.DetectedAtUtc });
    }
}
