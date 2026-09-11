using ConnectedOps.Domain.Drivers;
using ConnectedOps.Domain.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConnectedOps.Infrastructure.Persistence.Configurations;

public sealed class DriverNoteConfiguration : IEntityTypeConfiguration<DriverNote>
{
    public void Configure(EntityTypeBuilder<DriverNote> builder)
    {
        builder.ToTable("DriverNotes");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.TenantId)
            .IsRequired();

        builder.Property(x => x.DriverId)
            .IsRequired();

        builder.Property(x => x.NoteText)
            .HasMaxLength(2000)
            .IsRequired();

        builder.Property(x => x.CreatedByUserName)
            .HasMaxLength(150);

        builder.HasIndex(x => new { x.TenantId, x.DriverId });

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
