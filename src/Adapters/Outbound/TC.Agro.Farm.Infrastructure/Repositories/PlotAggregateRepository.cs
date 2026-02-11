namespace TC.Agro.Farm.Infrastructure.Repositories
{
    public sealed class PlotAggregateRepository : BaseRepository<PlotAggregate>, IPlotAggregateRepository
    {
        private readonly IUserContext _userContext;

        public PlotAggregateRepository(ApplicationDbContext dbContext, IUserContext userContext)
            : base(dbContext)
        {
            _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
        }

        private IQueryable<PlotAggregate> FilteredDbSet => DbSet
            .Where(x => x.Property.OwnerId == _userContext.Id);

        /// <inheritdoc />
        public async Task<bool> NameExistsForPropertyAsync(string name, Guid propertyId, CancellationToken cancellationToken = default)
        {
            return await FilteredDbSet
                .AsNoTracking()
                .AnyAsync(p => p.PropertyId == propertyId &&
                    EF.Functions.ILike(p.Name.Value, name), cancellationToken)
                .ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<bool> NameExistsForPropertyExcludingAsync(string name, Guid propertyId, Guid excludeId, CancellationToken cancellationToken = default)
        {
            return await FilteredDbSet
                .AsNoTracking()
                .AnyAsync(p => p.PropertyId == propertyId &&
                    p.Id != excludeId &&
                    EF.Functions.ILike(p.Name.Value, name), cancellationToken)
                .ConfigureAwait(false);
        }
    }
}
