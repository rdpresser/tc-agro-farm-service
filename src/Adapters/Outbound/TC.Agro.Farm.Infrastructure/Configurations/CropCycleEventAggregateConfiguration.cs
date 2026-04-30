namespace TC.Agro.Farm.Infrastructure.Configurations
{
    internal sealed class CropCycleEventAggregateConfiguration : IEntityTypeConfiguration<CropCycleEventAggregate>
    {
        public void Configure(EntityTypeBuilder<CropCycleEventAggregate> builder)
        {
            builder.ToTable("crop_cycle_events");

            builder.HasKey(e => e.Id);

            builder.Property(e => e.Id)
                .HasColumnName("id")
                .ValueGeneratedNever();

            builder.Property(e => e.CropCycleId)
                .HasColumnName("crop_cycle_id")
                .IsRequired();

            builder.Property(e => e.PlotId)
                .HasColumnName("plot_id")
                .IsRequired();

            builder.Property(e => e.PropertyId)
                .HasColumnName("property_id")
                .IsRequired();

            builder.Property(e => e.OwnerId)
                .HasColumnName("owner_id")
                .IsRequired();

            builder.Property(e => e.EventType)
                .HasColumnName("event_type")
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(e => e.Status)
                .HasColumnName("status")
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(e => e.Notes)
                .HasColumnName("notes")
                .HasMaxLength(1000)
                .IsRequired(false);

            builder.Property(e => e.OccurredAt)
                .HasColumnName("occurred_at")
                .HasColumnType("timestamptz")
                .IsRequired();

            builder.Property(e => e.CreatedAt)
                .HasColumnName("created_at")
                .HasColumnType("timestamptz")
                .IsRequired();

            builder.HasIndex(e => e.CropCycleId);
            builder.HasIndex(e => e.PlotId);
            builder.HasIndex(e => e.PropertyId);
            builder.HasIndex(e => e.OwnerId);
            builder.HasIndex(e => e.EventType);
        }
    }
}
