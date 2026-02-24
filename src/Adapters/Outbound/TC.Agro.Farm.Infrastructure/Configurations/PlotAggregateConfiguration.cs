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

            builder.HasOne(p => p.Property)
                .WithMany(p => p.Plots)
                .HasForeignKey(p => p.PropertyId);

            builder.HasMany(p => p.Sensors)
                .WithOne(s => s.Plot)
                .HasForeignKey(s => s.PlotId);

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

            // Navigation properties are required (except optional AdditionalNotes)
            builder.Navigation(p => p.Name).IsRequired();
            builder.Navigation(p => p.CropType).IsRequired();
            builder.Navigation(p => p.AreaHectares).IsRequired();
            builder.Navigation(p => p.IrrigationType).IsRequired();
        }
    }
}
