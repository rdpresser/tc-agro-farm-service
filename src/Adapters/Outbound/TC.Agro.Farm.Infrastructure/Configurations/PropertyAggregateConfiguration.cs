using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TC.Agro.Farm.Domain.Aggregates;
using TC.Agro.Farm.Domain.ValueObjects;
using TC.Agro.SharedKernel.Infrastructure.Database.EfCore;

namespace TC.Agro.Farm.Infrastructure.Configurations
{
    internal sealed class PropertyAggregateConfiguration : BaseEntityConfiguration<PropertyAggregate>
    {
        public override void Configure(EntityTypeBuilder<PropertyAggregate> builder)
        {
            base.Configure(builder);
            builder.ToTable("properties");

            // Name value object - single column
            builder.OwnsOne(p => p.Name, name =>
            {
                name.Property(n => n.Value)
                    .HasColumnName("name")
                    .IsRequired()
                    .HasMaxLength(200);

                name.WithOwner();
            });

            // Location value object - 6 columns for complex location data
            builder.OwnsOne(p => p.Location, location =>
            {
                location.Property(l => l.Address)
                    .HasColumnName("location_address")
                    .IsRequired()
                    .HasMaxLength(500);

                location.Property(l => l.City)
                    .HasColumnName("location_city")
                    .IsRequired()
                    .HasMaxLength(100);

                location.Property(l => l.State)
                    .HasColumnName("location_state")
                    .IsRequired()
                    .HasMaxLength(100);

                location.Property(l => l.Country)
                    .HasColumnName("location_country")
                    .IsRequired()
                    .HasMaxLength(100);

                location.Property(l => l.Latitude)
                    .HasColumnName("location_latitude")
                    .IsRequired(false);

                location.Property(l => l.Longitude)
                    .HasColumnName("location_longitude")
                    .IsRequired(false);

                location.WithOwner();
            });

            // Area value object - single column
            builder.OwnsOne(p => p.AreaHectares, area =>
            {
                area.Property(a => a.Hectares)
                    .HasColumnName("area_hectares")
                    .IsRequired();

                area.WithOwner();
            });

            // OwnerId - required foreign key to Identity service (not navigable)
            builder.Property(p => p.OwnerId)
                .IsRequired();

            builder.HasIndex(p => p.OwnerId);

            // Navigation properties are required
            builder.Navigation(p => p.Name).IsRequired();
            builder.Navigation(p => p.Location).IsRequired();
            builder.Navigation(p => p.AreaHectares).IsRequired();
        }
    }
}
