using Microsoft.EntityFrameworkCore;
using TC.Agro.Farm.Application.Abstractions.Ports;
using TC.Agro.Farm.Application.UseCases.Sensors.GetSensorById;
using TC.Agro.Farm.Application.UseCases.Sensors.GetSensorList;
using TC.Agro.Farm.Domain.Aggregates;

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
            var sensor = await _dbContext.Set<SensorAggregate>()
                .AsNoTracking()
                .Where(s => s.Id == id && s.IsActive)
                .Select(s => new
                {
                    s.Id,
                    s.PlotId,
                    Type = s.Type.Value,
                    Status = s.Status.Value,
                    Label = s.Label != null ? s.Label.Value : null,
                    s.InstalledAt,
                    s.UpdatedAt
                })
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);

            if (sensor is null)
                return null;

            // Get plot info
            var plotInfo = await _dbContext.Set<PlotAggregate>()
                .AsNoTracking()
                .Where(p => p.Id == sensor.PlotId)
                .Select(p => new { PlotName = p.Name.Value, p.PropertyId })
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);

            if (plotInfo is null)
                return null;

            // Get property name
            var propertyName = await _dbContext.Set<PropertyAggregate>()
                .AsNoTracking()
                .Where(prop => prop.Id == plotInfo.PropertyId)
                .Select(prop => prop.Name.Value)
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false) ?? "Unknown";

            // Use UpdatedAt as LastMaintenanceAt for now (could be a separate field in the future)
            DateTimeOffset? lastMaintenanceAt = sensor.UpdatedAt;

            return new SensorByIdResponse(
                sensor.Id,
                sensor.PlotId,
                plotInfo.PlotName,
                plotInfo.PropertyId,
                propertyName,
                sensor.Type,
                sensor.Status,
                sensor.Label,
                sensor.InstalledAt,
                lastMaintenanceAt);
        }

        /// <inheritdoc />
        public async Task<IReadOnlyList<SensorListResponse>> GetSensorListAsync(
            GetSensorListQuery query,
            CancellationToken cancellationToken = default)
        {
            var sensorsQuery = _dbContext.Set<SensorAggregate>()
                .AsNoTracking()
                .Where(s => s.IsActive);

            // Apply plot filter
            if (query.PlotId.HasValue)
            {
                sensorsQuery = sensorsQuery.Where(s => s.PlotId == query.PlotId.Value);
            }

            // Apply property filter (need to join with plots)
            if (query.PropertyId.HasValue)
            {
                var propertyPlotIds = await _dbContext.Set<PlotAggregate>()
                    .AsNoTracking()
                    .Where(p => p.PropertyId == query.PropertyId.Value && p.IsActive)
                    .Select(p => p.Id)
                    .ToListAsync(cancellationToken)
                    .ConfigureAwait(false);

                sensorsQuery = sensorsQuery.Where(s => propertyPlotIds.Contains(s.PlotId));
            }

            // Apply type filter
            if (!string.IsNullOrWhiteSpace(query.Type))
            {
                sensorsQuery = sensorsQuery.Where(s => EF.Functions.ILike(s.Type.Value, query.Type));
            }

            // Apply status filter
            if (!string.IsNullOrWhiteSpace(query.Status))
            {
                sensorsQuery = sensorsQuery.Where(s => EF.Functions.ILike(s.Status.Value, query.Status));
            }

            // Apply text filter (on label)
            if (!string.IsNullOrWhiteSpace(query.Filter))
            {
                var pattern = $"%{query.Filter}%";
                sensorsQuery = sensorsQuery.Where(s =>
                    s.Label != null && EF.Functions.ILike(s.Label.Value, pattern));
            }

            // Apply sorting
            sensorsQuery = query.SortBy.ToLowerInvariant() switch
            {
                "type" => query.SortDirection.ToLowerInvariant() == "desc"
                    ? sensorsQuery.OrderByDescending(s => s.Type.Value)
                    : sensorsQuery.OrderBy(s => s.Type.Value),
                "status" => query.SortDirection.ToLowerInvariant() == "desc"
                    ? sensorsQuery.OrderByDescending(s => s.Status.Value)
                    : sensorsQuery.OrderBy(s => s.Status.Value),
                "label" => query.SortDirection.ToLowerInvariant() == "desc"
                    ? sensorsQuery.OrderByDescending(s => s.Label != null ? s.Label.Value : "")
                    : sensorsQuery.OrderBy(s => s.Label != null ? s.Label.Value : ""),
                "installedat" => query.SortDirection.ToLowerInvariant() == "desc"
                    ? sensorsQuery.OrderByDescending(s => s.InstalledAt)
                    : sensorsQuery.OrderBy(s => s.InstalledAt),
                _ => sensorsQuery.OrderByDescending(s => s.InstalledAt)
            };

            // Apply pagination
            var skip = (query.PageNumber - 1) * query.PageSize;
            var sensors = await sensorsQuery
                .Skip(skip)
                .Take(query.PageSize)
                .Select(s => new
                {
                    s.Id,
                    s.PlotId,
                    Type = s.Type.Value,
                    Status = s.Status.Value,
                    Label = s.Label != null ? s.Label.Value : null,
                    s.InstalledAt
                })
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            // Get plot info for all sensors in a single query
            var plotIds = sensors.Select(s => s.PlotId).Distinct().ToList();
            var plotInfos = await _dbContext.Set<PlotAggregate>()
                .AsNoTracking()
                .Where(p => plotIds.Contains(p.Id))
                .Select(p => new { p.Id, PlotName = p.Name.Value, p.PropertyId })
                .ToDictionaryAsync(x => x.Id, x => new { x.PlotName, x.PropertyId }, cancellationToken)
                .ConfigureAwait(false);

            // Get property names for all plots in a single query
            var propertyIds = plotInfos.Values.Select(p => p.PropertyId).Distinct().ToList();
            var propertyNames = await _dbContext.Set<PropertyAggregate>()
                .AsNoTracking()
                .Where(prop => propertyIds.Contains(prop.Id))
                .Select(prop => new { prop.Id, Name = prop.Name.Value })
                .ToDictionaryAsync(x => x.Id, x => x.Name, cancellationToken)
                .ConfigureAwait(false);

            return sensors.Select(s =>
            {
                var plotInfo = plotInfos.GetValueOrDefault(s.PlotId);
                var plotName = plotInfo?.PlotName ?? "Unknown";
                var propertyId = plotInfo?.PropertyId ?? Guid.Empty;
                var propertyName = propertyNames.GetValueOrDefault(propertyId, "Unknown");

                return new SensorListResponse(
                    s.Id,
                    s.PlotId,
                    plotName,
                    propertyId,
                    propertyName,
                    s.Type,
                    s.Status,
                    s.Label,
                    s.InstalledAt);
            }).ToList();
        }

        /// <inheritdoc />
        public async Task<int> GetSensorCountAsync(
            GetSensorListQuery query,
            CancellationToken cancellationToken = default)
        {
            var sensorsQuery = _dbContext.Set<SensorAggregate>()
                .AsNoTracking()
                .Where(s => s.IsActive);

            // Apply plot filter
            if (query.PlotId.HasValue)
            {
                sensorsQuery = sensorsQuery.Where(s => s.PlotId == query.PlotId.Value);
            }

            // Apply property filter (need to join with plots)
            if (query.PropertyId.HasValue)
            {
                var filterPlotIds = await _dbContext.Set<PlotAggregate>()
                    .AsNoTracking()
                    .Where(p => p.PropertyId == query.PropertyId.Value && p.IsActive)
                    .Select(p => p.Id)
                    .ToListAsync(cancellationToken)
                    .ConfigureAwait(false);

                sensorsQuery = sensorsQuery.Where(s => filterPlotIds.Contains(s.PlotId));
            }

            // Apply type filter
            if (!string.IsNullOrWhiteSpace(query.Type))
            {
                sensorsQuery = sensorsQuery.Where(s => EF.Functions.ILike(s.Type.Value, query.Type));
            }

            // Apply status filter
            if (!string.IsNullOrWhiteSpace(query.Status))
            {
                sensorsQuery = sensorsQuery.Where(s => EF.Functions.ILike(s.Status.Value, query.Status));
            }

            // Apply text filter (on label)
            if (!string.IsNullOrWhiteSpace(query.Filter))
            {
                var pattern = $"%{query.Filter}%";
                sensorsQuery = sensorsQuery.Where(s =>
                    s.Label != null && EF.Functions.ILike(s.Label.Value, pattern));
            }

            return await sensorsQuery.CountAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}
