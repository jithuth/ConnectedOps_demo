using ConnectedOps.Domain.Tenancy;
using ConnectedOps.Domain.Vehicles;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConnectedOps.Infrastructure.Persistence.Configurations;

public sealed class VehicleNoteConfiguration : IEntityTypeConfiguration<VehicleNote>
{
    public void Configure(EntityTypeBuilder<VehicleNote> builder)
    {
        builder.ToTable("VehicleNotes");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.TenantId)
            .IsRequired();

        builder.Property(x => x.VehicleId)
            .IsRequired();

        builder.Property(x => x.NoteText)
            .HasMaxLength(2000)
            .IsRequired();

        builder.Property(x => x.CreatedByUserName)
            .HasMaxLength(150);

        builder.HasIndex(x => new { x.TenantId, x.VehicleId });

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
