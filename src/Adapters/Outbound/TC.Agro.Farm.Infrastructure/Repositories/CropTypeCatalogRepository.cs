namespace TC.Agro.Farm.Infrastructure.Repositories
{
    public sealed class CropTypeCatalogRepository : BaseRepository<CropTypeCatalogAggregate>, ICropTypeCatalogRepository
    {
        public CropTypeCatalogRepository(ApplicationDbContext dbContext)
            : base(dbContext)
        {
        }

        /// <inheritdoc />
        public async Task<CropTypeCatalogAggregate?> GetByNameAsync(
            string cropTypeName,
            Guid? ownerId = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(cropTypeName))
            {
                return null;
            }

            var normalized = cropTypeName.Trim().ToLowerInvariant();

            return await BuildScopedQuery(ownerId)
                .AsNoTracking()
                .Where(x => x.CropTypeName.Value.ToLower() == normalized)
                .OrderByDescending(x => ownerId.HasValue && x.OwnerId == ownerId.Value)
                .ThenBy(x => x.IsSystemDefined)
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<CropTypeCatalogAggregate?> GetByIdScopedAsync(
            Guid id,
            Guid? ownerId = null,
            bool includeInactive = false,
            CancellationToken cancellationToken = default)
        {
            return await BuildScopedQuery(ownerId, includeInactive)
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
                .ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<bool> NameExistsAsync(
            string cropTypeName,
            Guid? ownerId = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(cropTypeName))
            {
                return false;
            }

            var normalized = cropTypeName.Trim().ToLowerInvariant();

            return await BuildScopedQuery(ownerId)
                .AsNoTracking()
                .AnyAsync(x => x.CropTypeName.Value.ToLower() == normalized, cancellationToken)
                .ConfigureAwait(false);
        }

        private IQueryable<CropTypeCatalogAggregate> BuildScopedQuery(Guid? ownerId, bool includeInactive = false)
        {
            var query = includeInactive
                ? DbSet.IgnoreQueryFilters()
                : DbSet;

            if (ownerId.HasValue && ownerId.Value != Guid.Empty)
            {
                return query.Where(x => x.OwnerId == null || x.OwnerId == ownerId.Value);
            }

            return query.Where(x => x.OwnerId == null);
        }
    }
}
