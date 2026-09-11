using ConnectedOps.Domain.Organization;
using ConnectedOps.Domain.Tenancy;
using ConnectedOps.Domain.Vehicles;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConnectedOps.Infrastructure.Persistence.Configurations;

public sealed class VehicleConfiguration : IEntityTypeConfiguration<Vehicle>
{
    public void Configure(EntityTypeBuilder<Vehicle> builder)
    {
        builder.ToTable("Vehicles");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.TenantId)
            .IsRequired();

        // Identifiers
        builder.Property(x => x.VehicleNumber)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.DisplayName)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(x => x.InternalCode)
            .HasMaxLength(50);

        builder.Property(x => x.RegistrationNumber)
            .HasMaxLength(50);

        builder.Property(x => x.VIN)
            .HasMaxLength(50);

        builder.Property(x => x.ChassisNumber)
            .HasMaxLength(50);

        builder.Property(x => x.EngineNumber)
            .HasMaxLength(50);

        // Classification
        builder.Property(x => x.VehicleCategoryId)
            .IsRequired();

        builder.Property(x => x.VehicleMakeId)
            .IsRequired();

        builder.Property(x => x.VehicleModelId)
            .IsRequired();

        // Engineering & Specs
        builder.Property(x => x.FuelType)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.TransmissionType)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.Color)
            .HasMaxLength(50);

        builder.Property(x => x.GrossVehicleWeight)
            .HasPrecision(18, 2);

        builder.Property(x => x.PayloadCapacity)
            .HasPrecision(18, 2);

        // Ownership
        builder.Property(x => x.OwnershipType)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.OwnerName)
            .HasMaxLength(150);

        builder.Property(x => x.LeaseCompany)
            .HasMaxLength(150);

        builder.Property(x => x.MonthlyLeaseCost)
            .HasPrecision(18, 2);

        builder.Property(x => x.PurchasePrice)
            .HasPrecision(18, 2);

        builder.Property(x => x.CurrencyCode)
            .HasMaxLength(10);

        // Odometer
        builder.Property(x => x.CurrentOdometer)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(x => x.OdometerUnit)
            .HasConversion<int>()
            .IsRequired();

        // Lifecycle & Status
        builder.Property(x => x.Status)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.IsActive)
            .IsRequired();

        // Media & Notes
        builder.Property(x => x.PrimaryImageObjectKey)
            .HasMaxLength(500);

        builder.Property(x => x.Notes)
            .HasMaxLength(2000);

        // Indexes
        builder.HasIndex(x => new { x.TenantId, x.VehicleNumber })
            .IsUnique();

        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => new { x.TenantId, x.Status });
        builder.HasIndex(x => new { x.TenantId, x.VehicleCategoryId });
        builder.HasIndex(x => new { x.TenantId, x.VehicleMakeId });
        builder.HasIndex(x => new { x.TenantId, x.BranchId });
        builder.HasIndex(x => new { x.TenantId, x.RegistrationNumber });
        builder.HasIndex(x => new { x.TenantId, x.VIN });

        // Foreign Keys
        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.VehicleCategory)
            .WithMany()
            .HasForeignKey(x => x.VehicleCategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.VehicleMake)
            .WithMany()
            .HasForeignKey(x => x.VehicleMakeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.VehicleModel)
            .WithMany()
            .HasForeignKey(x => x.VehicleModelId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Branch)
            .WithMany()
            .HasForeignKey(x => x.BranchId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Location)
            .WithMany()
            .HasForeignKey(x => x.LocationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Specification)
            .WithOne(x => x.Vehicle)
            .HasForeignKey<VehicleSpecification>(x => x.VehicleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Registrations)
            .WithOne(x => x.Vehicle)
            .HasForeignKey(x => x.VehicleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.OdometerEntries)
            .WithOne(x => x.Vehicle)
            .HasForeignKey(x => x.VehicleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Documents)
            .WithOne(x => x.Vehicle)
            .HasForeignKey(x => x.VehicleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.NotesList)
            .WithOne(x => x.Vehicle)
            .HasForeignKey(x => x.VehicleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
