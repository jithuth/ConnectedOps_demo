using ConnectedOps.Domain.Drivers;
using ConnectedOps.Domain.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConnectedOps.Infrastructure.Persistence.Configurations;

public sealed class DriverEmergencyContactConfiguration : IEntityTypeConfiguration<DriverEmergencyContact>
{
    public void Configure(EntityTypeBuilder<DriverEmergencyContact> builder)
    {
        builder.ToTable("DriverEmergencyContacts");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.TenantId)
            .IsRequired();

        builder.Property(x => x.DriverId)
            .IsRequired();

        builder.Property(x => x.Name)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(x => x.Relationship)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.Phone)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.AlternatePhone)
            .HasMaxLength(50);

        builder.Property(x => x.IsPrimary)
            .IsRequired();

        builder.HasIndex(x => new { x.TenantId, x.DriverId });

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
