using TC.Agro.Farm.Application.UseCases.Sensors.GetSensorById;
using TC.Agro.Farm.Application.UseCases.Sensors.GetSensorList;

namespace TC.Agro.Farm.Application.Abstractions.Ports
{
    /// <summary>
    /// Read store interface for Sensor queries (CQRS read side).
    /// </summary>
    public interface ISensorReadStore
    {
        /// <summary>
        /// Gets a sensor by its unique identifier.
        /// </summary>
        Task<SensorByIdResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a paginated list of sensors with optional filtering.
        /// </summary>
        Task<IReadOnlyList<SensorListResponse>> GetSensorListAsync(
            GetSensorListQuery query,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the total count of sensors matching the filter criteria.
        /// </summary>
        Task<int> GetSensorCountAsync(
            GetSensorListQuery query,
            CancellationToken cancellationToken = default);
    }
}
