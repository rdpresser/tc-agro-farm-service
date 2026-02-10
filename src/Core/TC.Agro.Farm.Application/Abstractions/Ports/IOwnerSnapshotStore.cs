using TC.Agro.Farm.Domain.Snapshots;

namespace TC.Agro.Farm.Application.Abstractions.Ports
{
    /// <summary>
    /// Port for read-optimized Owner snapshot store operations.
    /// Handles persistence of owner snapshots projected from external integration events.
    /// </summary>
    public interface IOwnerSnapshotStore
    {
        /// <summary>
        /// Adds a new owner snapshot to the store.
        /// </summary>
        /// <param name="snapshot">The owner snapshot to add</param>
        /// <param name="cancellationToken">Cancellation token</param>
        Task AddAsync(OwnerSnapshot snapshot, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates an existing owner snapshot in the store.
        /// </summary>
        /// <param name="snapshot">The owner snapshot to update</param>
        /// <param name="cancellationToken">Cancellation token</param>
        Task UpdateAsync(OwnerSnapshot snapshot, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes an owner snapshot from the store by marking it as inactive.
        /// </summary>
        /// <param name="id">The owner snapshot identifier</param>
        /// <param name="cancellationToken">Cancellation token</param>
        Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves an owner snapshot by its identifier.
        /// </summary>
        /// <param name="id">The owner snapshot identifier</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The owner snapshot, or null if not found</returns>
        Task<OwnerSnapshot?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    }
}
