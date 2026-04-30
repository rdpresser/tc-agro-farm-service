namespace TC.Agro.Farm.Infrastructure.Repositories
{
    public sealed class PropertyAggregateRepository : BaseRepository<PropertyAggregate>, IPropertyAggregateRepository
    {
        private readonly IUserContext _userContext;

        public PropertyAggregateRepository(ApplicationDbContext dbContext, IUserContext userContext)
            : base(dbContext)
        {
            _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
        }

        private IQueryable<PropertyAggregate> FilteredDbSet => _userContext.IsAdmin
            ? DbSet
            : DbSet.Where(x => x.OwnerId == _userContext.Id);

        /// <inheritdoc />
        public async Task<PropertyAggregate?> GetAnyByOwnerAsync(Guid ownerId, CancellationToken cancellationToken = default)
        {
            if (ownerId == Guid.Empty)
            {
                return null;
            }

            return await FilteredDbSet
                .AsNoTracking()
                .Where(property => property.OwnerId == ownerId)
                .OrderBy(property => property.CreatedAt)
                .ThenBy(property => property.Id)
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<bool> NameExistsForOwnerAsync(string name, Guid ownerId, CancellationToken cancellationToken = default)
        {
            return await FilteredDbSet
                .AsNoTracking()
                .AnyAsync(p => p.OwnerId == ownerId &&
                    EF.Functions.ILike(p.Name.Value, name), cancellationToken)
                .ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<bool> NameExistsForOwnerExcludingAsync(string name, Guid ownerId, Guid excludeId, CancellationToken cancellationToken = default)
        {
            return await FilteredDbSet
                .AsNoTracking()
                .AnyAsync(p => p.OwnerId == ownerId &&
                    p.Id != excludeId &&
                    EF.Functions.ILike(p.Name.Value, name), cancellationToken)
                .ConfigureAwait(false);
        }
    }
}
