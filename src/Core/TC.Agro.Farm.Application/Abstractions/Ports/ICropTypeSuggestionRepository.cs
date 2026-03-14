namespace TC.Agro.Farm.Application.Abstractions.Ports
{
    /// <summary>
    /// Repository interface for CropTypeSuggestionAggregate persistence operations.
    /// </summary>
    public interface ICropTypeSuggestionRepository : IBaseRepository<CropTypeSuggestionAggregate>
    {
        /// <summary>
        /// Marks active AI suggestions as stale for a given property.
        /// </summary>
        Task MarkAiSuggestionsAsStaleByPropertyAsync(Guid propertyId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deactivates active AI suggestions for a given property.
        /// Manual overrides are preserved.
        /// </summary>
        Task DeactivateAiSuggestionsByPropertyAsync(Guid propertyId, CancellationToken cancellationToken = default);
    }
}
