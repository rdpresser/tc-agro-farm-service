namespace TC.Agro.Farm.Application.Abstractions.Ports
{
    /// <summary>
    /// Repository interface for crop cycle aggregate persistence operations.
    /// </summary>
    public interface ICropCycleAggregateRepository : IBaseRepository<CropCycleAggregate>
    {
        /// <summary>
        /// Checks whether a property currently has active crop cycles.
        /// </summary>
        Task<bool> HasActiveCyclesByPropertyAsync(Guid propertyId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks whether a plot currently has active crop cycles.
        /// </summary>
        Task<bool> HasActiveCyclesByPlotAsync(
            Guid plotId,
            Guid? excludingCycleId = null,
            CancellationToken cancellationToken = default);
    }
}
