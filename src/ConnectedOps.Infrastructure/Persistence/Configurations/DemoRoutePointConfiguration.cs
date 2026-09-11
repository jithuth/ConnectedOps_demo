using ConnectedOps.Domain.Demo;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConnectedOps.Infrastructure.Persistence.Configurations;

public sealed class DemoRoutePointConfiguration : IEntityTypeConfiguration<DemoRoutePoint>
{
    public void Configure(EntityTypeBuilder<DemoRoutePoint> builder)
    {
        builder.ToTable("DemoRoutePoints");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.SpeedKph)
            .HasPrecision(6, 2);

        builder.HasIndex(x => new { x.DemoRouteId, x.Sequence });
    }
}
