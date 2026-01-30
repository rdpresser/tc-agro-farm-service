using TC.Agro.Farm.Application.UseCases.Properties.GetPropertyById;
using TC.Agro.Farm.Application.UseCases.Properties.GetPropertyList;

namespace TC.Agro.Farm.Application.Abstractions.Ports
{
    /// <summary>
    /// Read store interface for Property queries (CQRS read side).
    /// </summary>
    public interface IPropertyReadStore
    {
        /// <summary>
        /// Gets a property by its unique identifier.
        /// </summary>
        Task<PropertyByIdResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a paginated list of properties with optional filtering.
        /// </summary>
        Task<IReadOnlyList<PropertyListResponse>> GetPropertyListAsync(
            GetPropertyListQuery query,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the total count of properties matching the filter criteria.
        /// </summary>
        Task<int> GetPropertyCountAsync(
            GetPropertyListQuery query,
            CancellationToken cancellationToken = default);
    }
}
