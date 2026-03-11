namespace TC.Agro.Farm.Application.Abstractions.Ports
{
    /// <summary>
    /// Repository interface for CropTypeCatalogAggregate persistence operations.
    /// </summary>
    public interface ICropTypeCatalogRepository : IBaseRepository<CropTypeCatalogAggregate>
    {
        /// <summary>
        /// Gets a crop type catalog entry by name.
        /// </summary>
        Task<CropTypeCatalogAggregate?> GetByNameAsync(
            string cropTypeName,
            Guid? ownerId = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a crop type catalog entry by identifier within the effective tenant scope.
        /// </summary>
        Task<CropTypeCatalogAggregate?> GetByIdScopedAsync(
            Guid id,
            Guid? ownerId = null,
            bool includeInactive = false,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks whether a crop type catalog name already exists.
        /// </summary>
        Task<bool> NameExistsAsync(
            string cropTypeName,
            Guid? ownerId = null,
            CancellationToken cancellationToken = default);
    }
}
