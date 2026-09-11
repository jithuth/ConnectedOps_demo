using ConnectedOps.Domain.Demo;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConnectedOps.Infrastructure.Persistence.Configurations;

public sealed class DemoRouteConfiguration : IEntityTypeConfiguration<DemoRoute>
{
    public void Configure(EntityTypeBuilder<DemoRoute> builder)
    {
        builder.ToTable("DemoRoutes");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(x => x.Description)
            .HasMaxLength(500);

        builder.HasIndex(x => new { x.TenantId, x.Name });

        builder.HasMany(x => x.Points)
            .WithOne(x => x.DemoRoute)
            .HasForeignKey(x => x.DemoRouteId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
