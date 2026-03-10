namespace TC.Agro.Farm.Infrastructure.Repositories
{
    public sealed class CropTypeCatalogRepository : BaseRepository<CropTypeCatalogAggregate>, ICropTypeCatalogRepository
    {
        public CropTypeCatalogRepository(ApplicationDbContext dbContext)
            : base(dbContext)
        {
        }

        /// <inheritdoc />
        public async Task<CropTypeCatalogAggregate?> GetByNameAsync(string cropTypeName, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(cropTypeName))
            {
                return null;
            }

            var normalized = cropTypeName.Trim();

            return await DbSet
                .AsNoTracking()
                .FirstOrDefaultAsync(x => EF.Functions.ILike(x.CropTypeName.Value, normalized), cancellationToken)
                .ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<bool> NameExistsAsync(string cropTypeName, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(cropTypeName))
            {
                return false;
            }

            var normalized = cropTypeName.Trim();

            return await DbSet
                .AsNoTracking()
                .AnyAsync(x => EF.Functions.ILike(x.CropTypeName.Value, normalized), cancellationToken)
                .ConfigureAwait(false);
        }
    }
}
