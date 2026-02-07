using TC.Agro.Farm.Application.UseCases.Plots.GetPlotById;
using TC.Agro.Farm.Application.UseCases.Plots.GetPlotList;

namespace TC.Agro.Farm.Application.Abstractions.Ports
{
    /// <summary>
    /// Read store interface for Plot queries (CQRS read side).
    /// </summary>
    public interface IPlotReadStore
    {
        /// <summary>
        /// Gets a plot by its unique identifier.
        /// </summary>
        Task<PlotByIdResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a paginated list of plots with optional filtering.
        /// </summary>
        Task<(IReadOnlyList<PlotListResponse> Plots, int TotalCount)> GetPlotListAsync(
            GetPlotListQuery query,
            CancellationToken cancellationToken = default);
    }
}
