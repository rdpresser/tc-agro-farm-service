using TC.Agro.Farm.Application.UseCases.Owners.List;

namespace TC.Agro.Farm.Application.Abstractions.Ports
{
    /// <summary>
    /// Read store interface for Owner snapshot queries (CQRS read side).
    /// </summary>
    public interface IOwnerReadStore
    {
        /// <summary>
        /// Gets a paginated list of active producer owners with optional filtering.
        /// </summary>
        Task<(IReadOnlyList<ListOwnersResponse> Owners, int TotalCount)> ListOwnersAsync(
            ListOwnersQuery query,
            CancellationToken cancellationToken = default);
    }
}
