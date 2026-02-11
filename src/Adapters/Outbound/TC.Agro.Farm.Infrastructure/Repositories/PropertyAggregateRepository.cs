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

        private IQueryable<PropertyAggregate> FilteredDbSet => DbContext.Properties
            .Where(x => x.OwnerId == _userContext.Id);

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
