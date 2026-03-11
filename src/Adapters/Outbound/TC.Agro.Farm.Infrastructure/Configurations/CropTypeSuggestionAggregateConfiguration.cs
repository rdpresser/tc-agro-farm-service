namespace TC.Agro.Farm.Infrastructure.Configurations
{
    internal sealed class CropTypeSuggestionAggregateConfiguration : BaseEntityConfiguration<CropTypeSuggestionAggregate>
    {
        public override void Configure(EntityTypeBuilder<CropTypeSuggestionAggregate> builder)
        {
            base.Configure(builder);
            builder.ToTable("crop_type_suggestions");

            builder.Property(c => c.PropertyId)
                .HasColumnName("property_id")
                .IsRequired();

            builder.Property(c => c.OwnerId)
                .HasColumnName("owner_id")
                .IsRequired();

            builder.Property(c => c.Source)
                .HasColumnName("source")
                .HasMaxLength(20)
                .IsRequired();

            builder.Property(c => c.IsOverride)
                .HasColumnName("is_override")
                .IsRequired()
                .HasDefaultValue(false);

            builder.Property(c => c.IsStale)
                .HasColumnName("is_stale")
                .IsRequired()
                .HasDefaultValue(false);

            builder.Property(c => c.ConfidenceScore)
                .HasColumnName("confidence_score")
                .HasColumnType("double precision")
                .IsRequired(false);

            builder.Property(c => c.PlantingWindow)
                .HasColumnName("planting_window")
                .HasMaxLength(200)
                .IsRequired(false);

            builder.Property(c => c.HarvestCycleMonths)
                .HasColumnName("harvest_cycle_months")
                .IsRequired(false);

            builder.Property(c => c.SuggestedIrrigationType)
                .HasColumnName("suggested_irrigation_type")
                .HasMaxLength(100)
                .IsRequired(false);

            builder.Property(c => c.MinSoilMoisture)
                .HasColumnName("min_soil_moisture")
                .HasColumnType("double precision")
                .IsRequired(false);

            builder.Property(c => c.MaxTemperature)
                .HasColumnName("max_temperature")
                .HasColumnType("double precision")
                .IsRequired(false);

            builder.Property(c => c.MinHumidity)
                .HasColumnName("min_humidity")
                .HasColumnType("double precision")
                .IsRequired(false);

            builder.Property(c => c.Notes)
                .HasColumnName("notes")
                .HasMaxLength(500)
                .IsRequired(false);

            builder.Property(c => c.Model)
                .HasColumnName("model")
                .HasMaxLength(100)
                .IsRequired(false);

            builder.Property(c => c.GeneratedAt)
                .HasColumnName("generated_at")
                .HasColumnType("timestamptz")
                .IsRequired(false);

            builder.Property(c => c.SuggestedImage)
                .HasColumnName("suggested_image")
                .HasMaxLength(10)
                .IsRequired(false);

            builder.OwnsOne(c => c.CropName, cropName =>
            {
                cropName.Property(x => x.Value)
                    .HasColumnName("crop_type")
                    .HasMaxLength(100)
                    .IsRequired();

                cropName.WithOwner();
            });

            builder.HasIndex(c => c.PropertyId);
            builder.HasIndex(c => c.OwnerId);
            builder.HasIndex(c => new { c.PropertyId, c.OwnerId });
            builder.HasIndex(c => new { c.PropertyId, c.Source, c.IsActive, c.IsStale });

            builder.HasOne(c => c.Property)
                .WithMany()
                .HasForeignKey(c => c.PropertyId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(c => c.Owner)
                .WithMany()
                .HasForeignKey(c => c.OwnerId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Navigation(c => c.CropName).IsRequired();
            builder.Navigation(c => c.Property).IsRequired();
            builder.Navigation(c => c.Owner).IsRequired();
        }
    }
}
