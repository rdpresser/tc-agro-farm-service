namespace TC.Agro.Farm.Infrastructure.Configurations
{
    internal sealed class CropTypeCatalogAggregateConfiguration : BaseEntityConfiguration<CropTypeCatalogAggregate>
    {
        public override void Configure(EntityTypeBuilder<CropTypeCatalogAggregate> builder)
        {
            base.Configure(builder);
            builder.ToTable("crop_type_catalog");

            builder.Property(c => c.IsSystemDefined)
                .HasColumnName("is_system_defined")
                .IsRequired()
                .HasDefaultValue(true);

            builder.Property(c => c.Description)
                .HasColumnName("description")
                .HasMaxLength(500)
                .IsRequired(false);

            builder.Property(c => c.ScientificName)
                .HasColumnName("scientific_name")
                .HasMaxLength(150)
                .IsRequired(false);

            builder.Property(c => c.TypicalPlantingStartMonth)
                .HasColumnName("typical_planting_start_month")
                .IsRequired(false);

            builder.Property(c => c.TypicalPlantingEndMonth)
                .HasColumnName("typical_planting_end_month")
                .IsRequired(false);

            builder.Property(c => c.RecommendedIrrigationType)
                .HasColumnName("recommended_irrigation_type")
                .HasMaxLength(100)
                .IsRequired(false);

            builder.Property(c => c.TypicalHarvestCycleMonths)
                .HasColumnName("typical_harvest_cycle_months")
                .IsRequired(false);

            builder.Property(c => c.MinTemperature)
                .HasColumnName("min_temperature")
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

            builder.Property(c => c.MinSoilMoisture)
                .HasColumnName("min_soil_moisture")
                .HasColumnType("double precision")
                .IsRequired(false);

            builder.Property(c => c.MaxSoilMoisture)
                .HasColumnName("max_soil_moisture")
                .HasColumnType("double precision")
                .IsRequired(false);

            builder.OwnsOne(c => c.CropTypeName, cropTypeName =>
            {
                cropTypeName.Property(x => x.Value)
                    .HasColumnName("name")
                    .HasMaxLength(100)
                    .IsRequired();

                cropTypeName.WithOwner();
            });

            builder.Navigation(c => c.CropTypeName).IsRequired();
        }
    }
}
