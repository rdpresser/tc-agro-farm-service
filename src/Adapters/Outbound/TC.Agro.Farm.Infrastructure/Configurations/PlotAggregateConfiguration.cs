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

            builder.Property(p => p.OwnerId)
                .HasColumnName("owner_id")
                .IsRequired();

            builder.Property(p => p.CropTypeCatalogId)
                .HasColumnName("crop_type_catalog_id")
                .IsRequired();

            builder.Property(p => p.SelectedCropTypeSuggestionId)
                .HasColumnName("selected_crop_type_suggestion_id")
                .IsRequired(false);

            builder.HasIndex(p => p.PropertyId);
            builder.HasIndex(p => p.OwnerId);
            builder.HasIndex(p => p.CropTypeCatalogId);
            builder.HasIndex(p => p.SelectedCropTypeSuggestionId);

            builder.HasOne(p => p.Property)
                .WithMany(p => p.Plots)
                .HasForeignKey(p => p.PropertyId);

            builder.HasMany(p => p.Sensors)
                .WithOne(s => s.Plot)
                .HasForeignKey(s => s.PlotId);

            builder.HasOne(p => p.CropTypeCatalog)
                .WithMany(c => c.Plots)
                .HasForeignKey(p => p.CropTypeCatalogId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(p => p.SelectedCropTypeSuggestion)
                .WithMany()
                .HasForeignKey(p => p.SelectedCropTypeSuggestionId)
                .OnDelete(DeleteBehavior.SetNull);

            // Name value object - single column
            builder.OwnsOne(p => p.Name, name =>
            {
                name.Property(n => n.Value)
                    .HasColumnName("name")
                    .IsRequired()
                    .HasMaxLength(200);

                name.WithOwner();
            });

            // Area value object - single column
            builder.OwnsOne(p => p.AreaHectares, area =>
            {
                area.Property(a => a.Hectares)
                    .HasColumnName("area_hectares")
                    .IsRequired();

                area.WithOwner();
            });

            builder.Property(p => p.Latitude)
                .HasColumnName("latitude")
                .HasColumnType("double precision")
                .IsRequired(false);

            builder.Property(p => p.Longitude)
                .HasColumnName("longitude")
                .HasColumnType("double precision")
                .IsRequired(false);

            builder.Property(p => p.BoundaryGeoJson)
                .HasColumnName("boundary_geo_json")
                .HasColumnType("text")
                .IsRequired(false);

            // Agronomy fields
            builder.Property(p => p.PlantingDate)
                .HasColumnName("planting_date")
                .HasColumnType("timestamptz")
                .IsRequired();

            builder.Property(p => p.ExpectedHarvestDate)
                .HasColumnName("expected_harvest_date")
                .HasColumnType("timestamptz")
                .IsRequired();

            builder.OwnsOne(p => p.IrrigationType, irrigation =>
            {
                irrigation.Property(i => i.Value)
                    .HasColumnName("irrigation_type")
                    .IsRequired()
                    .HasMaxLength(50);

                irrigation.WithOwner();
            });

            // AdditionalNotes value object - optional, single column
            builder.OwnsOne(p => p.AdditionalNotes, notes =>
            {
                notes.Property(n => n.Value)
                    .HasColumnName("additional_notes")
                    .HasMaxLength(1000);

                notes.WithOwner();
            });

            // Navigation properties are required
            builder.Navigation(p => p.Name).IsRequired();
            builder.Navigation(p => p.AreaHectares).IsRequired();
            builder.Navigation(p => p.IrrigationType).IsRequired();
            builder.Navigation(p => p.CropTypeCatalog).IsRequired();
        }
    }
}
