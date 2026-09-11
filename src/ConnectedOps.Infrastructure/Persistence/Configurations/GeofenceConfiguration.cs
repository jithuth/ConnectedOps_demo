using ConnectedOps.Domain.Geofences;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConnectedOps.Infrastructure.Persistence.Configurations;

public sealed class GeofenceConfiguration : IEntityTypeConfiguration<Geofence>
{
    public void Configure(EntityTypeBuilder<Geofence> builder)
    {
        builder.ToTable("Geofences");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(x => x.Code)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.Description)
            .HasMaxLength(500);

        builder.Property(x => x.ColorHex)
            .IsRequired()
            .HasMaxLength(10)
            .HasDefaultValue("#3B82F6");

        builder.Property(x => x.PolygonGeoJson)
            .HasColumnType("nvarchar(max)");

        builder.HasIndex(x => new { x.TenantId, x.Code })
            .IsUnique();

        builder.HasIndex(x => new { x.TenantId, x.IsActive, x.IsDeleted });

        builder.HasOne(x => x.Branch)
            .WithMany()
            .HasForeignKey(x => x.BranchId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(x => x.Location)
            .WithMany()
            .HasForeignKey(x => x.LocationId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
