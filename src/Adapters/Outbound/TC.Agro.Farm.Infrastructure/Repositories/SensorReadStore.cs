using TC.Agro.Farm.Application.UseCases.Sensors.GetSensorById;
using TC.Agro.Farm.Application.UseCases.Sensors.ListFromPlot;
using TC.Agro.Farm.Infrastructure.Extensions;

namespace TC.Agro.Farm.Infrastructure.Repositories
{
    public sealed class SensorReadStore : ISensorReadStore
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IUserContext _userContext;

        public SensorReadStore(ApplicationDbContext dbContext, IUserContext userContext)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
        }

        private IQueryable<SensorAggregate> FilteredDbSet => _dbContext.Sensors
            .Where(x => x.Plot.Property.OwnerId == _userContext.Id);

        /// <inheritdoc />
        public async Task<SensorByIdResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var sensor = await FilteredDbSet
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
        public async Task<(IReadOnlyList<ListSensorsFromPlotResponse> Sensors, int TotalCount)> ListSensorsFromPlotAsync(
            ListSensorsFromPlotQuery query,
            CancellationToken cancellationToken = default)
        {
            var sensorsQuery = FilteredDbSet
                .AsNoTracking()
                .Where(s => s.PlotId == query.Id);

            // Apply optional type filter (using index on type)
            if (!string.IsNullOrWhiteSpace(query.Type))
            {
                sensorsQuery = sensorsQuery.Where(s => EF.Functions.ILike(s.Type.Value, query.Type));
            }

            // Apply optional status filter (using index on status)
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
            var totalCount = await sensorsQuery
                .CountAsync(cancellationToken)
                .ConfigureAwait(false);

            // Apply sorting, pagination, and projection
            var sensors = await sensorsQuery
                .ApplySorting(query.SortBy, query.SortDirection)
                .ApplyPagination(query.PageNumber, query.PageSize)
                .Select(s => new ListSensorsFromPlotResponse(
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
