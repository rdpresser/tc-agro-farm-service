using TC.Agro.Farm.Application.UseCases.Plots.GetById;
using TC.Agro.Farm.Application.UseCases.Plots.ListByProperty;

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
        Task<GetPlotByIdResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a paginated list of plots with optional filtering.
        /// </summary>
        Task<(IReadOnlyList<ListPlotsFromPropertyResponse> Plots, int TotalCount)> ListPlotsFromPropertyAsync(
            ListPlotsFromPropertyQuery query,
            CancellationToken cancellationToken = default);
    }
}
