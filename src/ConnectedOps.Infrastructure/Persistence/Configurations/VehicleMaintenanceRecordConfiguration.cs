using ConnectedOps.Domain.Maintenance;
using ConnectedOps.Domain.Tenancy;
using ConnectedOps.Domain.Vehicles;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConnectedOps.Infrastructure.Persistence.Configurations;

public sealed class VehicleMaintenanceRecordConfiguration : IEntityTypeConfiguration<VehicleMaintenanceRecord>
{
    public void Configure(EntityTypeBuilder<VehicleMaintenanceRecord> builder)
    {
        builder.ToTable("VehicleMaintenanceRecords");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.TenantId)
            .IsRequired();

        builder.Property(x => x.VehicleId)
            .IsRequired();

        builder.Property(x => x.MaintenanceServiceTypeId)
            .IsRequired();

        builder.Property(x => x.ServiceDateUtc)
            .IsRequired();

        builder.Property(x => x.OdometerReading)
            .HasPrecision(18, 2);

        builder.Property(x => x.OdometerUnit)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.EngineHours)
            .HasPrecision(18, 2);

        builder.Property(x => x.MaintenanceType)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.Status)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.ReferenceNumber)
            .HasMaxLength(100);

        builder.Property(x => x.Description)
            .HasMaxLength(1000);

        builder.Property(x => x.TechnicianNotes)
            .HasMaxLength(2000);

        builder.Property(x => x.TotalPartsCost)
            .HasPrecision(18, 2);

        builder.Property(x => x.TotalLabourCost)
            .HasPrecision(18, 2);

        builder.Property(x => x.OtherCost)
            .HasPrecision(18, 2);

        builder.Property(x => x.TotalCost)
            .HasPrecision(18, 2);

        builder.Property(x => x.CurrencyCode)
            .HasMaxLength(10);

        builder.HasIndex(x => new { x.TenantId, x.VehicleId, x.ServiceDateUtc });
        builder.HasIndex(x => new { x.TenantId, x.MaintenanceServiceTypeId, x.ServiceDateUtc });
        builder.HasIndex(x => new { x.TenantId, x.Status });
        builder.HasIndex(x => new { x.TenantId, x.MaintenanceProviderId });

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Vehicle)
            .WithMany()
            .HasForeignKey(x => x.VehicleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.MaintenanceServiceType)
            .WithMany()
            .HasForeignKey(x => x.MaintenanceServiceTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.MaintenancePlan)
            .WithMany()
            .HasForeignKey(x => x.MaintenancePlanId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.MaintenancePlanRule)
            .WithMany()
            .HasForeignKey(x => x.MaintenancePlanRuleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.MaintenanceProvider)
            .WithMany()
            .HasForeignKey(x => x.MaintenanceProviderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Tasks)
            .WithOne(x => x.MaintenanceRecord)
            .HasForeignKey(x => x.MaintenanceRecordId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Parts)
            .WithOne(x => x.MaintenanceRecord)
            .HasForeignKey(x => x.MaintenanceRecordId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Labour)
            .WithOne(x => x.MaintenanceRecord)
            .HasForeignKey(x => x.MaintenanceRecordId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Expenses)
            .WithOne(x => x.MaintenanceRecord)
            .HasForeignKey(x => x.MaintenanceRecordId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Documents)
            .WithOne(x => x.MaintenanceRecord)
            .HasForeignKey(x => x.MaintenanceRecordId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.DowntimeRecords)
            .WithOne(x => x.MaintenanceRecord)
            .HasForeignKey(x => x.MaintenanceRecordId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
