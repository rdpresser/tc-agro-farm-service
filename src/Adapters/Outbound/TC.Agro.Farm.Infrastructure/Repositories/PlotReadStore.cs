using TC.Agro.Farm.Application.Abstractions.Ports;
using TC.Agro.Farm.Application.UseCases.Plots.GetPlotById;
using TC.Agro.Farm.Application.UseCases.Plots.GetPlotList;
using TC.Agro.Farm.Domain.Aggregates;

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
                .Where(p => p.Id == id && p.IsActive)
                .Select(p => new
                {
                    p.Id,
                    p.PropertyId,
                    Name = p.Name.Value,
                    CropType = p.CropType.Value,
                    AreaHectares = p.AreaHectares.Hectares,
                    p.IsActive,
                    p.CreatedAt,
                    p.UpdatedAt
                })
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);

            if (plot is null)
                return null;

            // Get property name
            var propertyName = await _dbContext.Properties
                .AsNoTracking()
                .Where(prop => prop.Id == plot.PropertyId)
                .Select(prop => prop.Name.Value)
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false) ?? "Unknown";

            // Get sensor count
            var sensorCount = await _dbContext.Sensors
                .AsNoTracking()
                .CountAsync(s => s.PlotId == id && s.IsActive, cancellationToken)
                .ConfigureAwait(false);

            return new PlotByIdResponse(
                plot.Id,
                plot.PropertyId,
                propertyName,
                plot.Name,
                plot.CropType,
                plot.AreaHectares,
                plot.IsActive,
                sensorCount,
                plot.CreatedAt,
                plot.UpdatedAt);
        }

        /// <inheritdoc />
        public async Task<(IReadOnlyList<PlotListResponse> Plots, int TotalCount)> GetPlotListAsync(
            GetPlotListQuery query,
            CancellationToken cancellationToken = default)
        {
            var plotsQuery = _dbContext.Plots
                .AsNoTracking()
                .Where(p => p.IsActive);

            // Apply property filter
            if (query.PropertyId.HasValue)
            {
                plotsQuery = plotsQuery.Where(p => p.PropertyId == query.PropertyId.Value);
            }

            // Apply crop type filter
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
            plotsQuery = query.SortBy.ToLowerInvariant() switch
            {
                "name" => query.SortDirection.ToLowerInvariant() == "desc"
                    ? plotsQuery.OrderByDescending(p => p.Name.Value)
                    : plotsQuery.OrderBy(p => p.Name.Value),
                "croptype" => query.SortDirection.ToLowerInvariant() == "desc"
                    ? plotsQuery.OrderByDescending(p => p.CropType.Value)
                    : plotsQuery.OrderBy(p => p.CropType.Value),
                "areahectares" => query.SortDirection.ToLowerInvariant() == "desc"
                    ? plotsQuery.OrderByDescending(p => p.AreaHectares.Hectares)
                    : plotsQuery.OrderBy(p => p.AreaHectares.Hectares),
                "createdat" => query.SortDirection.ToLowerInvariant() == "desc"
                    ? plotsQuery.OrderByDescending(p => p.CreatedAt)
                    : plotsQuery.OrderBy(p => p.CreatedAt),
                _ => plotsQuery.OrderBy(p => p.Name.Value)
            };

            // Apply pagination
            var skip = (query.PageNumber - 1) * query.PageSize;
            var plots = await plotsQuery
                .Skip(skip)
                .Take(query.PageSize)
                .Select(p => new
                {
                    p.Id,
                    p.PropertyId,
                    Name = p.Name.Value,
                    CropType = p.CropType.Value,
                    AreaHectares = p.AreaHectares.Hectares,
                    p.IsActive,
                    p.CreatedAt
                })
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            // Get property names for all plots in a single query
            var propertyIds = plots.Select(p => p.PropertyId).Distinct().ToList();
            var propertyNames = await _dbContext.Properties
                .AsNoTracking()
                .Where(prop => propertyIds.Contains(prop.Id))
                .Select(prop => new { prop.Id, Name = prop.Name.Value })
                .ToDictionaryAsync(x => x.Id, x => x.Name, cancellationToken)
                .ConfigureAwait(false);

            // Get sensor counts for all plots in a single query
            var plotIds = plots.Select(p => p.Id).ToList();
            var sensorCounts = await _dbContext.Sensors
                .AsNoTracking()
                .Where(s => plotIds.Contains(s.PlotId) && s.IsActive)
                .GroupBy(s => s.PlotId)
                .Select(g => new { PlotId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.PlotId, x => x.Count, cancellationToken)
                .ConfigureAwait(false);

            var results = plots.Select(p => new PlotListResponse(
                p.Id,
                p.PropertyId,
                propertyNames.GetValueOrDefault(p.PropertyId, "Unknown"),
                p.Name,
                p.CropType,
                p.AreaHectares,
                p.IsActive,
                sensorCounts.GetValueOrDefault(p.Id, 0),
                p.CreatedAt)).ToList();

            return ([.. results], totalCount);
        }
    }
}
