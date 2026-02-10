namespace TC.Agro.Farm.Infrastructure.Configurations
{
    internal sealed class OwnerSnapshotConfiguration : IEntityTypeConfiguration<OwnerSnapshot>
    {
        public void Configure(EntityTypeBuilder<OwnerSnapshot> builder)
        {
            builder.ToTable("owner_snapshots");

            builder.HasKey(o => o.Id);
            builder.Property(x => x.Id)
                .IsRequired()
                .ValueGeneratedOnAdd();

            builder.Property(o => o.Name)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(o => o.Email)
                .IsRequired()
                .HasMaxLength(200);

            builder.HasIndex(o => o.Email)
                .IsUnique();

            // Soft delete / active flag
            builder.Property(x => x.IsActive)
                .IsRequired()
                .HasDefaultValue(true);

            builder.Property(o => o.CreatedAt)
                .IsRequired()
                .HasColumnType("timestamptz");

            builder.Property(o => o.UpdatedAt)
                .HasColumnType("timestamptz");

            // Navigation property to Properties owned by this owner
            builder.HasMany(o => o.Properties)
                .WithOne(p => p.Owner)
                .HasForeignKey(p => p.OwnerId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
