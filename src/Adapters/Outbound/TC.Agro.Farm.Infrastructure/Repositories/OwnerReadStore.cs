using TC.Agro.Farm.Application.UseCases.Owners.List;

namespace TC.Agro.Farm.Infrastructure.Repositories
{
    public sealed class OwnerReadStore : IOwnerReadStore
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IUserContext _userContext;

        public OwnerReadStore(ApplicationDbContext dbContext, IUserContext userContext)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
        }

        private IQueryable<OwnerSnapshot> FilteredDbSet
        {
            get
            {
                if (_userContext.IsAdmin)
                {
                    return _dbContext.OwnerSnapshots.Where(x => x.IsActive);
                }

                if (string.Equals(_userContext.Role, AppConstants.ProducerRole, StringComparison.OrdinalIgnoreCase))
                {
                    return _dbContext.OwnerSnapshots.Where(x => x.Id == _userContext.Id && x.IsActive);
                }

                return _dbContext.OwnerSnapshots.Where(_ => false);
            }
        }

        /// <inheritdoc />
        public async Task<(IReadOnlyList<ListOwnersResponse> Owners, int TotalCount)> ListOwnersAsync(
            ListOwnersQuery query,
            CancellationToken cancellationToken = default)
        {
            var ownersQuery = FilteredDbSet
                .AsNoTracking()
                .ApplyTextFilter(query.Filter);

            var totalCount = await ownersQuery
                .CountAsync(cancellationToken)
                .ConfigureAwait(false);

            var owners = await ownersQuery
                .ApplySorting(query.SortBy, query.SortDirection)
                .ApplyPagination(query.PageNumber, query.PageSize)
                .Select(o => new ListOwnersResponse(
                    o.Id,
                    o.Name,
                    o.Email,
                    o.IsActive,
                    o.CreatedAt,
                    o.UpdatedAt))
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            return ([.. owners], totalCount);
        }
    }
}
