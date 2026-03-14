namespace TC.Agro.Farm.Infrastructure.Configurations
{
    internal sealed class CropCycleAggregateConfiguration : BaseEntityConfiguration<CropCycleAggregate>
    {
        public override void Configure(EntityTypeBuilder<CropCycleAggregate> builder)
        {
            base.Configure(builder);
            builder.ToTable("crop_cycles");

            builder.Property(c => c.PlotId)
                .HasColumnName("plot_id")
                .IsRequired();

            builder.Property(c => c.PropertyId)
                .HasColumnName("property_id")
                .IsRequired();

            builder.Property(c => c.OwnerId)
                .HasColumnName("owner_id")
                .IsRequired();

            builder.Property(c => c.CropTypeCatalogId)
                .HasColumnName("crop_type_catalog_id")
                .IsRequired();

            builder.Property(c => c.SelectedCropTypeSuggestionId)
                .HasColumnName("selected_crop_type_suggestion_id")
                .IsRequired(false);

            builder.Property(c => c.Notes)
                .HasColumnName("notes")
                .HasMaxLength(1000)
                .IsRequired(false);

            builder.Property(c => c.StartedAt)
                .HasColumnName("started_at")
                .HasColumnType("timestamptz")
                .IsRequired();

            builder.Property(c => c.ExpectedHarvestDate)
                .HasColumnName("expected_harvest_date")
                .HasColumnType("timestamptz")
                .IsRequired(false);

            builder.Property(c => c.EndedAt)
                .HasColumnName("ended_at")
                .HasColumnType("timestamptz")
                .IsRequired(false);

            builder.HasIndex(c => c.PlotId);
            builder.HasIndex(c => c.PropertyId);
            builder.HasIndex(c => c.OwnerId);
            builder.HasIndex(c => c.CropTypeCatalogId);
            builder.HasIndex(c => c.SelectedCropTypeSuggestionId);

            builder.HasOne(c => c.Plot)
                .WithMany()
                .HasForeignKey(c => c.PlotId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(c => c.Property)
                .WithMany()
                .HasForeignKey(c => c.PropertyId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(c => c.Owner)
                .WithMany()
                .HasForeignKey(c => c.OwnerId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(c => c.CropTypeCatalog)
                .WithMany()
                .HasForeignKey(c => c.CropTypeCatalogId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(c => c.SelectedCropTypeSuggestion)
                .WithMany()
                .HasForeignKey(c => c.SelectedCropTypeSuggestionId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasMany(c => c.Events)
                .WithOne(e => e.CropCycle)
                .HasForeignKey(e => e.CropCycleId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.OwnsOne(c => c.Status, status =>
            {
                status.Property(s => s.Value)
                    .HasColumnName("status")
                    .HasMaxLength(50)
                    .IsRequired();

                status.WithOwner();

                status.HasIndex(s => s.Value)
                    .HasDatabaseName("ix_crop_cycles_status");
            });

            builder.Navigation(c => c.Status).IsRequired();
            builder.Navigation(c => c.CropTypeCatalog).IsRequired();
        }
    }
}
