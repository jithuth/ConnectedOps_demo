using ConnectedOps.Domain.Drivers;
using ConnectedOps.Domain.Organization;
using ConnectedOps.Domain.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConnectedOps.Infrastructure.Persistence.Configurations;

public sealed class DriverConfiguration : IEntityTypeConfiguration<Driver>
{
    public void Configure(EntityTypeBuilder<Driver> builder)
    {
        builder.ToTable("Drivers");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.TenantId)
            .IsRequired();

        builder.Property(x => x.DriverNumber)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.FirstName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.MiddleName)
            .HasMaxLength(100);

        builder.Property(x => x.LastName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.DisplayName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.Email)
            .HasMaxLength(150);

        builder.Property(x => x.Phone)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.AlternatePhone)
            .HasMaxLength(50);

        builder.Property(x => x.DriverType)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.Status)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.NationalityCode)
            .HasMaxLength(10);

        builder.Property(x => x.PrimaryLicenseNumber)
            .HasMaxLength(50);

        builder.Property(x => x.PreferredLanguage)
            .HasMaxLength(20);

        builder.Property(x => x.ProfileImageObjectKey)
            .HasMaxLength(500);

        builder.Property(x => x.Notes)
            .HasMaxLength(2000);

        builder.Property(x => x.IsActive)
            .IsRequired();

        // Indexes
        builder.HasIndex(x => new { x.TenantId, x.DriverNumber })
            .IsUnique();

        builder.HasIndex(x => new { x.TenantId, x.EmployeeId })
            .IsUnique()
            .HasFilter("[EmployeeId] IS NOT NULL");

        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => new { x.TenantId, x.Status });
        builder.HasIndex(x => new { x.TenantId, x.DriverType });
        builder.HasIndex(x => new { x.TenantId, x.BranchId });
        builder.HasIndex(x => new { x.TenantId, x.DepartmentId });

        // Relationships
        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Employee)
            .WithMany()
            .HasForeignKey(x => x.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Branch)
            .WithMany()
            .HasForeignKey(x => x.BranchId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Department)
            .WithMany()
            .HasForeignKey(x => x.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Licenses)
            .WithOne(x => x.Driver)
            .HasForeignKey(x => x.DriverId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Certifications)
            .WithOne(x => x.Driver)
            .HasForeignKey(x => x.DriverId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Documents)
            .WithOne(x => x.Driver)
            .HasForeignKey(x => x.DriverId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.VehicleAssignments)
            .WithOne(x => x.Driver)
            .HasForeignKey(x => x.DriverId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.EmergencyContacts)
            .WithOne(x => x.Driver)
            .HasForeignKey(x => x.DriverId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.NotesList)
            .WithOne(x => x.Driver)
            .HasForeignKey(x => x.DriverId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
