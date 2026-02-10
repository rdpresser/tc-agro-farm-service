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
                .Where(p => p.Id == id)
                .Select(p => new PropertyByIdResponse(
                    p.Id,
                    p.Name.Value,
                    p.Location.Address,
                    p.Location.City,
                    p.Location.State,
                    p.Location.Country,
                    p.Location.Latitude,
                    p.Location.Longitude,
                    p.AreaHectares.Hectares,
                    p.OwnerId,
                    p.IsActive,
                    p.Plots.Count,
                    p.CreatedAt,
                    p.UpdatedAt))
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);

            return property;
        }

        /// <inheritdoc />
        public async Task<(IReadOnlyList<PropertyListResponse> Properties, int TotalCount)> GetPropertyListAsync(
            GetPropertyListQuery query,
            CancellationToken cancellationToken = default)
        {
            var propertiesQuery = _dbContext.Properties
                .AsNoTracking();

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
            if (!string.IsNullOrWhiteSpace(query.SortBy))
            {
                var isAscending = string.Equals(query.SortDirection, "asc", StringComparison.OrdinalIgnoreCase);

                propertiesQuery = query.SortBy.ToLowerInvariant() switch
                {
                    "name" => isAscending
                        ? propertiesQuery.OrderBy(p => p.Name.Value)
                        : propertiesQuery.OrderByDescending(p => p.Name.Value),
                    "city" => isAscending
                        ? propertiesQuery.OrderBy(p => p.Location.City)
                        : propertiesQuery.OrderByDescending(p => p.Location.City),
                    "state" => isAscending
                        ? propertiesQuery.OrderBy(p => p.Location.State)
                        : propertiesQuery.OrderByDescending(p => p.Location.State),
                    "areahectares" => isAscending
                        ? propertiesQuery.OrderBy(p => p.AreaHectares.Hectares)
                        : propertiesQuery.OrderByDescending(p => p.AreaHectares.Hectares),
                    "createdat" => isAscending
                        ? propertiesQuery.OrderBy(p => p.CreatedAt)
                        : propertiesQuery.OrderByDescending(p => p.CreatedAt),
                    _ => propertiesQuery.OrderByDescending(p => p.CreatedAt)
                };
            }
            else
            {
                propertiesQuery = propertiesQuery.OrderByDescending(p => p.CreatedAt);
            }

            // Apply pagination and project directly to response DTO
            var properties = await propertiesQuery
                .Skip((query.PageNumber - 1) * query.PageSize)
                .Take(query.PageSize)
                .Select(p => new PropertyListResponse(
                    p.Id,
                    p.Name.Value,
                    p.Location.City,
                    p.Location.State,
                    p.Location.Country,
                    p.AreaHectares.Hectares,
                    p.OwnerId,
                    p.IsActive,
                    p.Plots.Count,
                    p.CreatedAt))
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            return ([.. properties], totalCount);
        }
    }
}
