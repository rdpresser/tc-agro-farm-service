using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TC.Agro.Farm.Domain.Aggregates;
using TC.Agro.SharedKernel.Infrastructure.Database.EfCore;

namespace TC.Agro.Farm.Infrastructure.Configurations
{
    internal sealed class PlotAggregateConfiguration : BaseEntityConfiguration<PlotAggregate>
    {
        public override void Configure(EntityTypeBuilder<PlotAggregate> builder)
        {
            base.Configure(builder);
            builder.ToTable("plots");

            // PropertyId - required foreign key
            builder.Property(p => p.PropertyId)
                .IsRequired();

            builder.HasIndex(p => p.PropertyId);

            // Name value object - single column
            builder.OwnsOne(p => p.Name, name =>
            {
                name.Property(n => n.Value)
                    .HasColumnName("name")
                    .IsRequired()
                    .HasMaxLength(200);

                name.WithOwner();
            });

            // CropType value object - single column
            builder.OwnsOne(p => p.CropType, cropType =>
            {
                cropType.Property(c => c.Value)
                    .HasColumnName("crop_type")
                    .IsRequired()
                    .HasMaxLength(100);

                cropType.WithOwner();
            });

            // Area value object - single column
            builder.OwnsOne(p => p.AreaHectares, area =>
            {
                area.Property(a => a.Hectares)
                    .HasColumnName("area_hectares")
                    .IsRequired();

                area.WithOwner();
            });

            // Navigation properties are required
            builder.Navigation(p => p.Name).IsRequired();
            builder.Navigation(p => p.CropType).IsRequired();
            builder.Navigation(p => p.AreaHectares).IsRequired();

            // Composite index for name uniqueness per property
            builder.HasIndex(p => new { p.PropertyId, p.Name.Value })
                .IsUnique()
                .HasDatabaseName("ix_plots_property_id_name");
        }
    }
}
