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
        Task<CropTypeCatalogAggregate?> GetByNameAsync(string cropTypeName, CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks whether a crop type catalog name already exists.
        /// </summary>
        Task<bool> NameExistsAsync(string cropTypeName, CancellationToken cancellationToken = default);
    }
}
