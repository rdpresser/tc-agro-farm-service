using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TC.Agro.Farm.Domain.Aggregates;
using TC.Agro.Farm.Domain.ValueObjects;
using TC.Agro.SharedKernel.Infrastructure.Database.EfCore;

namespace TC.Agro.Farm.Infrastructure.Configurations
{
    internal sealed class SensorAggregateConfiguration : BaseEntityConfiguration<SensorAggregate>
    {
        public override void Configure(EntityTypeBuilder<SensorAggregate> builder)
        {
            base.Configure(builder);
            builder.ToTable("sensors");

            // PlotId - required foreign key
            builder.Property(s => s.PlotId)
                .IsRequired();

            builder.HasIndex(s => s.PlotId);

            // SensorType value object - single column
            builder.OwnsOne(s => s.Type, type =>
            {
                type.Property(t => t.Value)
                    .HasColumnName("type")
                    .IsRequired()
                    .HasMaxLength(50);

                type.WithOwner();

                // Index for type filtering
                type.HasIndex(t => t.Value)
                    .HasDatabaseName("ix_sensors_type");
            });

            // SensorStatus value object - single column
            builder.OwnsOne(s => s.Status, status =>
            {
                status.Property(st => st.Value)
                    .HasColumnName("status")
                    .IsRequired()
                    .HasMaxLength(20);

                status.WithOwner();

                status.HasIndex(st => st.Value)
                    .HasDatabaseName("ix_sensors_status");
            });

            // InstalledAt - required timestamp
            builder.Property(s => s.InstalledAt)
                .IsRequired()
                .HasColumnType("timestamptz");

            // Label value object - optional
            builder.OwnsOne(s => s.Label, label =>
            {
                label.Property(l => l.Value)
                    .HasColumnName("label")
                    .IsRequired(false)
                    .HasMaxLength(200);

                label.WithOwner();
            });

            // Navigation properties
            builder.Navigation(s => s.Type).IsRequired();
            builder.Navigation(s => s.Status).IsRequired();
        }
    }
}
