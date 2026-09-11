using ConnectedOps.Domain.Organization;
using ConnectedOps.Domain.Tenancy;
using ConnectedOps.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConnectedOps.Infrastructure.Persistence.Configurations;

public sealed class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> builder)
    {
        builder.ToTable("Employees");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.TenantId)
            .IsRequired();

        builder.Property(x => x.EmployeeNumber)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.FirstName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.LastName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Ignore(x => x.FullName);

        builder.Property(x => x.Email)
            .HasMaxLength(250)
            .IsRequired();

        builder.Property(x => x.Phone)
            .HasMaxLength(50);

        builder.Property(x => x.JobTitle)
            .HasMaxLength(150);

        builder.Property(x => x.EmploymentType)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.EmploymentStatus)
            .HasConversion<int>()
            .IsRequired();

        builder.Ignore(x => x.IsActive);

        builder.Property(x => x.HireDate);

        builder.Property(x => x.TerminationDate);

        builder.HasIndex(x => new { x.TenantId, x.EmployeeNumber })
            .IsUnique();

        builder.HasIndex(x => new { x.TenantId, x.UserId })
            .HasFilter("[UserId] IS NOT NULL AND [IsDeleted] = 0")
            .IsUnique();

        builder.HasIndex(x => x.TenantId);

        builder.HasIndex(x => x.BranchId);

        builder.HasIndex(x => x.DepartmentId);

        builder.HasIndex(x => x.TeamId);

        builder.HasIndex(x => x.ManagerEmployeeId);

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Branch)
            .WithMany(x => x.Employees)
            .HasForeignKey(x => x.BranchId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Department)
            .WithMany(x => x.Employees)
            .HasForeignKey(x => x.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Team)
            .WithMany(x => x.Employees)
            .HasForeignKey(x => x.TeamId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Manager)
            .WithMany(x => x.DirectReports)
            .HasForeignKey(x => x.ManagerEmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
