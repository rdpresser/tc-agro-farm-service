using TC.Agro.Farm.Application.UseCases.Plots.GetPlotById;
using TC.Agro.Farm.Application.UseCases.Plots.GetPlotList;

namespace TC.Agro.Farm.Infrastructure.Repositories
{
    public sealed class PlotReadStore : IPlotReadStore
    {
        private readonly ApplicationDbContext _dbContext;

        public PlotReadStore(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }

        /// <inheritdoc />
        public async Task<PlotByIdResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var plot = await _dbContext.Plots
                .AsNoTracking()
                .Where(p => p.Id == id)
                .Select(p => new PlotByIdResponse(
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
        public async Task<(IReadOnlyList<PlotListResponse> Plots, int TotalCount)> GetPlotListAsync(
            GetPlotListQuery query,
            CancellationToken cancellationToken = default)
        {
            var plotsQuery = _dbContext.Plots
                .AsNoTracking();

            // Apply property filter
            if (query.PropertyId.HasValue)
            {
                plotsQuery = plotsQuery.Where(p => p.PropertyId == query.PropertyId.Value);
            }

            // Apply crop type filter (using index on crop_type)
            if (!string.IsNullOrWhiteSpace(query.CropType))
            {
                plotsQuery = plotsQuery.Where(p => EF.Functions.ILike(p.CropType.Value, query.CropType));
            }

            // Apply text filter
            if (!string.IsNullOrWhiteSpace(query.Filter))
            {
                var pattern = $"%{query.Filter}%";
                plotsQuery = plotsQuery.Where(p =>
                    EF.Functions.ILike(p.Name.Value, pattern) ||
                    EF.Functions.ILike(p.CropType.Value, pattern));
            }

            // Get total count before pagination
            var totalCount = await plotsQuery.CountAsync(cancellationToken).ConfigureAwait(false);

            // Apply sorting
            if (!string.IsNullOrWhiteSpace(query.SortBy))
            {
                var isAscending = string.Equals(query.SortDirection, "asc", StringComparison.OrdinalIgnoreCase);

                plotsQuery = query.SortBy.ToLowerInvariant() switch
                {
                    "name" => isAscending
                        ? plotsQuery.OrderBy(p => p.Name.Value)
                        : plotsQuery.OrderByDescending(p => p.Name.Value),
                    "croptype" => isAscending
                        ? plotsQuery.OrderBy(p => p.CropType.Value)
                        : plotsQuery.OrderByDescending(p => p.CropType.Value),
                    "areahectares" => isAscending
                        ? plotsQuery.OrderBy(p => p.AreaHectares.Hectares)
                        : plotsQuery.OrderByDescending(p => p.AreaHectares.Hectares),
                    "createdat" => isAscending
                        ? plotsQuery.OrderBy(p => p.CreatedAt)
                        : plotsQuery.OrderByDescending(p => p.CreatedAt),
                    _ => plotsQuery.OrderByDescending(p => p.CreatedAt)
                };
            }
            else
            {
                plotsQuery = plotsQuery.OrderByDescending(p => p.CreatedAt);
            }

            // Apply pagination and project directly to response DTO with sensor count
            var plots = await plotsQuery
                .Skip((query.PageNumber - 1) * query.PageSize)
                .Take(query.PageSize)
                .Select(p => new PlotListResponse(
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
