using TC.Agro.Farm.Application.UseCases.Sensors.GetSensorById;
using TC.Agro.Farm.Application.UseCases.Sensors.GetSensorList;

namespace TC.Agro.Farm.Infrastructure.Repositories
{
    public sealed class SensorReadStore : ISensorReadStore
    {
        private readonly ApplicationDbContext _dbContext;

        public SensorReadStore(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }

        /// <inheritdoc />
        public async Task<SensorByIdResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var sensor = await _dbContext.Sensors
                .AsNoTracking()
                .Where(s => s.Id == id)
                .Select(s => new SensorByIdResponse(
                    s.Id,
                    s.PlotId,
                    s.Plot.Name.Value,
                    s.Plot.PropertyId,
                    s.Plot.Property.Name.Value,
                    s.Type.Value,
                    s.Status.Value,
                    s.Label != null ? s.Label.Value : null,
                    s.InstalledAt,
                    s.UpdatedAt))
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);

            return sensor;
        }

        /// <inheritdoc />
        public async Task<(IReadOnlyList<SensorListResponse> Sensors, int TotalCount)> GetSensorListAsync(
            GetSensorListQuery query,
            CancellationToken cancellationToken = default)
        {
            var sensorsQuery = _dbContext.Sensors
                .AsNoTracking();

            // Apply plot filter
            if (query.PlotId.HasValue)
            {
                sensorsQuery = sensorsQuery.Where(s => s.PlotId == query.PlotId.Value);
            }

            // Apply property filter using navigation property join
            if (query.PropertyId.HasValue)
            {
                sensorsQuery = sensorsQuery.Where(s => s.Plot.PropertyId == query.PropertyId.Value);
            }

            // Apply type filter (using index on type)
            if (!string.IsNullOrWhiteSpace(query.Type))
            {
                sensorsQuery = sensorsQuery.Where(s => EF.Functions.ILike(s.Type.Value, query.Type));
            }

            // Apply status filter (using index on status)
            if (!string.IsNullOrWhiteSpace(query.Status))
            {
                sensorsQuery = sensorsQuery.Where(s => EF.Functions.ILike(s.Status.Value, query.Status));
            }

            // Apply text filter on label
            if (!string.IsNullOrWhiteSpace(query.Filter))
            {
                var pattern = $"%{query.Filter}%";
                sensorsQuery = sensorsQuery.Where(s =>
                    s.Label != null && EF.Functions.ILike(s.Label.Value, pattern));
            }

            // Get total count before pagination
            var totalCount = await sensorsQuery.CountAsync(cancellationToken).ConfigureAwait(false);

            // Apply sorting
            if (!string.IsNullOrWhiteSpace(query.SortBy))
            {
                var isAscending = string.Equals(query.SortDirection, "asc", StringComparison.OrdinalIgnoreCase);

                sensorsQuery = query.SortBy.ToLowerInvariant() switch
                {
                    "type" => isAscending
                        ? sensorsQuery.OrderBy(s => s.Type.Value)
                        : sensorsQuery.OrderByDescending(s => s.Type.Value),
                    "status" => isAscending
                        ? sensorsQuery.OrderBy(s => s.Status.Value)
                        : sensorsQuery.OrderByDescending(s => s.Status.Value),
                    "label" => isAscending
                        ? sensorsQuery.OrderBy(s => s.Label != null ? s.Label.Value : "")
                        : sensorsQuery.OrderByDescending(s => s.Label != null ? s.Label.Value : ""),
                    "installedat" => isAscending
                        ? sensorsQuery.OrderBy(s => s.InstalledAt)
                        : sensorsQuery.OrderByDescending(s => s.InstalledAt),
                    _ => sensorsQuery.OrderByDescending(s => s.InstalledAt)
                };
            }
            else
            {
                sensorsQuery = sensorsQuery.OrderByDescending(s => s.InstalledAt);
            }

            // Apply pagination and project directly to response DTO
            var sensors = await sensorsQuery
                .Skip((query.PageNumber - 1) * query.PageSize)
                .Take(query.PageSize)
                .Select(s => new SensorListResponse(
                    s.Id,
                    s.PlotId,
                    s.Plot.Name.Value,
                    s.Plot.PropertyId,
                    s.Plot.Property.Name.Value,
                    s.Type.Value,
                    s.Status.Value,
                    s.Label != null ? s.Label.Value : null,
                    s.InstalledAt))
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            return ([.. sensors], totalCount);
        }
    }
}
