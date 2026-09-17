using ConnectedOps.Domain.Reports;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConnectedOps.Infrastructure.Persistence.Configurations;

public sealed class ReportDefinitionConfiguration : IEntityTypeConfiguration<ReportDefinition>
{
    public void Configure(EntityTypeBuilder<ReportDefinition> builder)
    {
        builder.ToTable("ReportDefinitions");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Code)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(r => r.Name)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(r => r.Category)
            .IsRequired();

        builder.Property(r => r.Description)
            .HasMaxLength(500);

        builder.Property(r => r.ParametersSchema)
            .HasMaxLength(4000);

        builder.HasIndex(r => new { r.TenantId, r.Code })
            .IsUnique();
    }
}

public sealed class ReportExecutionLogConfiguration : IEntityTypeConfiguration<ReportExecutionLog>
{
    public void Configure(EntityTypeBuilder<ReportExecutionLog> builder)
    {
        builder.ToTable("ReportExecutionLogs");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.ReportCode)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(r => r.ReportName)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(r => r.GeneratedByUserName)
            .HasMaxLength(150);

        builder.Property(r => r.FilterCriteriaJson)
            .HasMaxLength(4000);

        builder.HasIndex(r => new { r.TenantId, r.CreatedAtUtc });
    }
}

public sealed class EsgEmissionFactorConfiguration : IEntityTypeConfiguration<EsgEmissionFactor>
{
    public void Configure(EntityTypeBuilder<EsgEmissionFactor> builder)
    {
        builder.ToTable("EsgEmissionFactors");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.FuelCode)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(e => e.FuelName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.KgCo2PerUnit)
            .HasPrecision(18, 4);

        builder.Property(e => e.UnitName)
            .IsRequired()
            .HasMaxLength(30);

        builder.Property(e => e.RegulatoryStandard)
            .HasMaxLength(100);

        builder.HasIndex(e => new { e.TenantId, e.FuelCode })
            .IsUnique();
    }
}
