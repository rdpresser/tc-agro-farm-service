namespace TC.Agro.Farm.Application.Abstractions.Ports
{
    /// <summary>
    /// Repository interface for PlotAggregate persistence operations.
    /// </summary>
    public interface IPlotAggregateRepository : IBaseRepository<PlotAggregate>
    {
        /// <summary>
        /// Checks if a plot with the given name exists for the specified property.
        /// </summary>
        Task<bool> NameExistsForPropertyAsync(string name, Guid propertyId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if a plot with the given name exists for the specified property, excluding a specific plot.
        /// </summary>
        Task<bool> NameExistsForPropertyExcludingAsync(string name, Guid propertyId, Guid excludeId, CancellationToken cancellationToken = default);
    }
}
