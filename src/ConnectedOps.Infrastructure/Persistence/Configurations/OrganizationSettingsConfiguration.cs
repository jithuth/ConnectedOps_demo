using ConnectedOps.Domain.Organization;
using ConnectedOps.Domain.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConnectedOps.Infrastructure.Persistence.Configurations;

public sealed class OrganizationSettingsConfiguration : IEntityTypeConfiguration<OrganizationSettings>
{
    public void Configure(EntityTypeBuilder<OrganizationSettings> builder)
    {
        builder.ToTable("OrganizationSettings");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.TenantId)
            .IsRequired();

        builder.Property(x => x.EnforceBranchAssignment)
            .IsRequired();

        builder.Property(x => x.EnforceDepartmentAssignment)
            .IsRequired();

        builder.Property(x => x.AutoCreateEmployeeForUser)
            .IsRequired();

        builder.Property(x => x.FiscalYearStartMonth)
            .IsRequired();

        builder.Property(x => x.DefaultWorkingDaysJson)
            .HasMaxLength(500);

        builder.HasIndex(x => x.TenantId)
            .IsUnique();

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
