namespace TC.Agro.Farm.Application.Abstractions.Ports
{
    /// <summary>
    /// Repository interface for SensorAggregate persistence operations.
    /// </summary>
    public interface ISensorAggregateRepository : IBaseRepository<SensorAggregate>
    {
        /// <summary>
        /// Checks if a sensor with the given label exists for the specified plot.
        /// </summary>
        Task<bool> LabelExistsForPlotAsync(string label, Guid plotId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if a sensor with the given label exists for the specified plot, excluding a specific sensor.
        /// </summary>
        Task<bool> LabelExistsForPlotExcludingAsync(string label, Guid plotId, Guid excludeId, CancellationToken cancellationToken = default);
    }
}
