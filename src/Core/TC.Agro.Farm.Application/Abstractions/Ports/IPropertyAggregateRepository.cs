namespace TC.Agro.Farm.Application.Abstractions.Ports
{
    /// <summary>
    /// Repository interface for PropertyAggregate persistence operations.
    /// </summary>
    public interface IPropertyAggregateRepository : IBaseRepository<PropertyAggregate>
    {
        /// <summary>
        /// Gets a representative property for the informed owner.
        /// Used when owner-scoped contracts still need property context in response payloads.
        /// </summary>
        Task<PropertyAggregate?> GetAnyByOwnerAsync(Guid ownerId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if a property with the given name exists for the specified owner.
        /// </summary>
        Task<bool> NameExistsForOwnerAsync(string name, Guid ownerId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if a property with the given name exists for the specified owner, excluding a specific property.
        /// </summary>
        Task<bool> NameExistsForOwnerExcludingAsync(string name, Guid ownerId, Guid excludeId, CancellationToken cancellationToken = default);
    }
}
