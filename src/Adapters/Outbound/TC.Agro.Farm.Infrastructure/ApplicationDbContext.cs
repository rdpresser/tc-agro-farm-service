using TC.Agro.Farm.Domain.Aggregates;
using TC.Agro.Farm.Domain.Snapshots;
using TC.Agro.SharedKernel.Infrastructure.Database;

namespace TC.Agro.Farm.Infrastructure
{
    [ExcludeFromCodeCoverage]
    public sealed class ApplicationDbContext : DbContext, IApplicationDbContext
    {
        public DbSet<PropertyAggregate> Properties { get; set; } = default!;
        public DbSet<PlotAggregate> Plots { get; set; } = default!;
        public DbSet<SensorAggregate> Sensors { get; set; } = default!;
        public DbSet<OwnerSnapshot> OwnerSnapshots { get; set; } = default!;

        /// <inheritdoc />
        public DbContext DbContext => this;

        public ApplicationDbContext(DbContextOptions options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.HasDefaultSchema(DefaultSchemas.Default);

            // Ignore domain events - they are not persisted as separate entities
            modelBuilder.Ignore<BaseDomainEvent>();

            modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

            // Global query filter for soft delete - only return active users
            modelBuilder.Entity<PropertyAggregate>().HasQueryFilter(u => u.IsActive);
            modelBuilder.Entity<PlotAggregate>().HasQueryFilter(u => u.IsActive);
            modelBuilder.Entity<SensorAggregate>().HasQueryFilter(u => u.IsActive);
            modelBuilder.Entity<OwnerSnapshot>().HasQueryFilter(u => u.IsActive);
        }

        /// <inheritdoc />
        async Task<int> IUnitOfWork.SaveChangesAsync(CancellationToken ct)
        {
            Log.Debug("ApplicationDbContext.SaveChangesAsync called. ChangeTracker has {Count} entries",
                ChangeTracker.Entries().Count());

            var entriesBeforeSave = ChangeTracker.Entries()
                .Where(e => e.State == EntityState.Added ||
                           e.State == EntityState.Modified ||
                           e.State == EntityState.Deleted)
                .ToList();

            Log.Debug("Entries to save: Added={Added}, Modified={Modified}, Deleted={Deleted}",
                entriesBeforeSave.Count(e => e.State == EntityState.Added),
                entriesBeforeSave.Count(e => e.State == EntityState.Modified),
                entriesBeforeSave.Count(e => e.State == EntityState.Deleted));

            if (!entriesBeforeSave.Any())
            {
                Log.Warning("SaveChangesAsync called but ChangeTracker has no pending changes!");
                return 0;
            }

            var result = await base.SaveChangesAsync(ct);

            Log.Information("Successfully saved {Count} changes to database", result);

            return result;
        }
    }
}
