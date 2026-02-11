using TC.Agro.Farm.Application.UseCases.Sensors.GetSensorById;
using TC.Agro.Farm.Application.UseCases.Sensors.ListFromPlot;

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
        Task<(IReadOnlyList<ListSensorsFromPlotResponse> Sensors, int TotalCount)> ListSensorsFromPlotAsync(
            ListSensorsFromPlotQuery query,
            CancellationToken cancellationToken = default);
    }
}
