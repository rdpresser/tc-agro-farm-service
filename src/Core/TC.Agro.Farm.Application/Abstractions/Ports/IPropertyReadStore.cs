using TC.Agro.Farm.Application.UseCases.Properties.GetById;
using TC.Agro.Farm.Application.UseCases.Properties.List;

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
        Task<GetPropertyByIdResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a paginated list of properties with optional filtering.
        /// </summary>
        Task<(IReadOnlyList<ListPropertiesResponse> Properties, int TotalCount)> GetPropertyListAsync(
            ListPropertiesQuery query,
            CancellationToken cancellationToken = default);
    }
}
