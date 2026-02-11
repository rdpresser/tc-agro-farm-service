using TC.Agro.Farm.Application.UseCases.Plots.GetById;
using TC.Agro.Farm.Application.UseCases.Plots.ListByProperty;
using TC.Agro.Farm.Infrastructure.Extensions;

namespace TC.Agro.Farm.Infrastructure.Repositories
{
    public sealed class PlotReadStore : IPlotReadStore
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IUserContext _userContext;

        public PlotReadStore(ApplicationDbContext dbContext, IUserContext userContext)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
        }

        private IQueryable<PlotAggregate> FilteredDbSet => _dbContext.Plots
            .Where(x => x.Property.OwnerId == _userContext.Id);

        /// <inheritdoc />
        public async Task<GetPlotByIdResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var plot = await FilteredDbSet
                .AsNoTracking()
                .Where(p => p.Id == id)
                .Select(p => new GetPlotByIdResponse(
                    p.Id,
                    p.PropertyId,
                    p.Property.Name.Value,
                    p.Name.Value,
                    p.CropType.Value,
                    p.AreaHectares.Hectares,
                    p.IsActive,
                    p.Sensors.Count,
                    p.CreatedAt,
                    p.UpdatedAt))
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);

            return plot;
        }

        /// <inheritdoc />
        public async Task<(IReadOnlyList<ListPlotsFromPropertyResponse> Plots, int TotalCount)> ListPlotsFromPropertyAsync(
            ListPlotsFromPropertyQuery query,
            CancellationToken cancellationToken = default)
        {
            var plotsQuery = FilteredDbSet
                .AsNoTracking()
                .Where(p => p.PropertyId == query.Id);

            // Apply optional crop type filter (using index on crop_type)
            if (!string.IsNullOrWhiteSpace(query.CropType))
            {
                plotsQuery = plotsQuery.Where(p => EF.Functions.ILike(p.CropType.Value, query.CropType));
            }

            // Apply text filter (searches name and crop type)
            plotsQuery = plotsQuery.ApplyTextFilter(query.Filter);

            // Get total count before pagination
            var totalCount = await plotsQuery
                .CountAsync(cancellationToken)
                .ConfigureAwait(false);

            // Apply sorting, pagination, and projection in one go
            var plots = await plotsQuery
                .ApplySorting(query.SortBy, query.SortDirection)
                .ApplyPagination(query.PageNumber, query.PageSize)
                .Select(p => new ListPlotsFromPropertyResponse(
                    p.Id,
                    p.PropertyId,
                    p.Property.Name.Value,
                    p.Name.Value,
                    p.CropType.Value,
                    p.AreaHectares.Hectares,
                    p.IsActive,
                    p.Sensors.Count,
                    p.CreatedAt))
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            return ([.. plots], totalCount);
        }
    }
}
