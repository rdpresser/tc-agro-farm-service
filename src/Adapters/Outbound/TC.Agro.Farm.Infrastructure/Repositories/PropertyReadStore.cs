using TC.Agro.Farm.Application.UseCases.Properties.GetById;
using TC.Agro.Farm.Application.UseCases.Properties.List;
using TC.Agro.Farm.Infrastructure.Extensions;

namespace TC.Agro.Farm.Infrastructure.Repositories
{
    public sealed class PropertyReadStore : IPropertyReadStore
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IUserContext _userContext;

        public PropertyReadStore(ApplicationDbContext dbContext, IUserContext userContext)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
        }

        private IQueryable<PropertyAggregate> FilteredDbSet => _dbContext.Properties
            .Where(x => x.OwnerId == _userContext.Id);

        /// <inheritdoc />
        public async Task<GetPropertyByIdResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var property = await FilteredDbSet
                .AsNoTracking()
                .Where(p => p.Id == id)
                .Select(p => new GetPropertyByIdResponse(
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
        public async Task<(IReadOnlyList<ListPropertiesResponse> Properties, int TotalCount)> GetPropertyListAsync(
            ListPropertiesQuery query,
            CancellationToken cancellationToken = default)
        {
            var propertiesQuery = FilteredDbSet
                .AsNoTracking();

            // Apply text filter
            propertiesQuery = propertiesQuery.ApplyTextFilter(query.Filter);

            // Get total count before pagination
            var totalCount = await propertiesQuery
                .CountAsync(cancellationToken)
                .ConfigureAwait(false);

            // Apply sorting, pagination, and projection
            var properties = await propertiesQuery
                .ApplySorting(query.SortBy, query.SortDirection)
                .ApplyPagination(query.PageNumber, query.PageSize)
                .Select(p => new ListPropertiesResponse(
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
