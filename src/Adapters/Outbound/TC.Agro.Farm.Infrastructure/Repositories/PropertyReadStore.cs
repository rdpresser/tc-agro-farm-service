using TC.Agro.Farm.Application.Abstractions.Ports;
using TC.Agro.Farm.Application.UseCases.Properties.GetPropertyById;
using TC.Agro.Farm.Application.UseCases.Properties.GetPropertyList;
using TC.Agro.Farm.Domain.Aggregates;

namespace TC.Agro.Farm.Infrastructure.Repositories
{
    public sealed class PropertyReadStore : IPropertyReadStore
    {
        private readonly ApplicationDbContext _dbContext;

        public PropertyReadStore(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }

        /// <inheritdoc />
        public async Task<PropertyByIdResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var property = await _dbContext.Properties
                .AsNoTracking()
                .Where(p => p.Id == id && p.IsActive)
                .Select(p => new
                {
                    p.Id,
                    Name = p.Name.Value,
                    Address = p.Location.Address,
                    City = p.Location.City,
                    State = p.Location.State,
                    Country = p.Location.Country,
                    Latitude = p.Location.Latitude,
                    Longitude = p.Location.Longitude,
                    AreaHectares = p.AreaHectares.Hectares,
                    p.OwnerId,
                    p.IsActive,
                    p.CreatedAt,
                    p.UpdatedAt
                })
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);

            if (property is null)
                return null;

            // Get plot count
            var plotCount = await _dbContext.Plots
                .AsNoTracking()
                .CountAsync(plot => plot.PropertyId == id && plot.IsActive, cancellationToken)
                .ConfigureAwait(false);

            return new PropertyByIdResponse(
                property.Id,
                property.Name,
                property.Address,
                property.City,
                property.State,
                property.Country,
                property.Latitude,
                property.Longitude,
                property.AreaHectares,
                property.OwnerId,
                property.IsActive,
                plotCount,
                property.CreatedAt,
                property.UpdatedAt);
        }

        /// <inheritdoc />
        public async Task<(IReadOnlyList<PropertyListResponse> Properties, int TotalCount)> GetPropertyListAsync(
            GetPropertyListQuery query,
            CancellationToken cancellationToken = default)
        {
            var propertiesQuery = _dbContext.Properties
                .AsNoTracking()
                .Where(p => p.IsActive);

            // Apply owner filter
            if (query.OwnerId.HasValue)
            {
                propertiesQuery = propertiesQuery.Where(p => p.OwnerId == query.OwnerId.Value);
            }

            // Apply text filter
            if (!string.IsNullOrWhiteSpace(query.Filter))
            {
                var pattern = $"%{query.Filter}%";
                propertiesQuery = propertiesQuery.Where(p =>
                    EF.Functions.ILike(p.Name.Value, pattern) ||
                    EF.Functions.ILike(p.Location.City, pattern) ||
                    EF.Functions.ILike(p.Location.State, pattern) ||
                    EF.Functions.ILike(p.Location.Country, pattern));
            }

            // Get total count before pagination
            var totalCount = await propertiesQuery.CountAsync(cancellationToken).ConfigureAwait(false);

            // Apply sorting
            propertiesQuery = query.SortBy.ToLowerInvariant() switch
            {
                "name" => query.SortDirection.ToLowerInvariant() == "desc"
                    ? propertiesQuery.OrderByDescending(p => p.Name.Value)
                    : propertiesQuery.OrderBy(p => p.Name.Value),
                "city" => query.SortDirection.ToLowerInvariant() == "desc"
                    ? propertiesQuery.OrderByDescending(p => p.Location.City)
                    : propertiesQuery.OrderBy(p => p.Location.City),
                "state" => query.SortDirection.ToLowerInvariant() == "desc"
                    ? propertiesQuery.OrderByDescending(p => p.Location.State)
                    : propertiesQuery.OrderBy(p => p.Location.State),
                "areahectares" => query.SortDirection.ToLowerInvariant() == "desc"
                    ? propertiesQuery.OrderByDescending(p => p.AreaHectares.Hectares)
                    : propertiesQuery.OrderBy(p => p.AreaHectares.Hectares),
                "createdat" => query.SortDirection.ToLowerInvariant() == "desc"
                    ? propertiesQuery.OrderByDescending(p => p.CreatedAt)
                    : propertiesQuery.OrderBy(p => p.CreatedAt),
                _ => propertiesQuery.OrderBy(p => p.Name.Value)
            };

            // Apply pagination
            var skip = (query.PageNumber - 1) * query.PageSize;
            var properties = await propertiesQuery
                .Skip(skip)
                .Take(query.PageSize)
                .Select(p => new
                {
                    p.Id,
                    Name = p.Name.Value,
                    City = p.Location.City,
                    State = p.Location.State,
                    Country = p.Location.Country,
                    AreaHectares = p.AreaHectares.Hectares,
                    p.OwnerId,
                    p.IsActive,
                    p.CreatedAt
                })
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            // Get plot counts for all properties in a single query
            var propertyIds = properties.Select(p => p.Id).ToList();
            var plotCounts = await _dbContext.Plots
                .AsNoTracking()
                .Where(plot => propertyIds.Contains(plot.PropertyId) && plot.IsActive)
                .GroupBy(plot => plot.PropertyId)
                .Select(g => new { PropertyId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.PropertyId, x => x.Count, cancellationToken)
                .ConfigureAwait(false);

            var results = properties.Select(p => new PropertyListResponse(
                p.Id,
                p.Name,
                p.City,
                p.State,
                p.Country,
                p.AreaHectares,
                p.OwnerId,
                p.IsActive,
                plotCounts.GetValueOrDefault(p.Id, 0),
                p.CreatedAt)).ToList();

            return ([.. results], totalCount);
        }
    }
}
