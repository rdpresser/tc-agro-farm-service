namespace TC.Agro.Farm.Infrastructure.Repositories
{
    public sealed class SensorAggregateRepository : BaseRepository<SensorAggregate>, ISensorAggregateRepository
    {
        private readonly IUserContext _userContext;

        public SensorAggregateRepository(ApplicationDbContext dbContext, IUserContext userContext)
            : base(dbContext)
        {
            _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
        }

        private IQueryable<SensorAggregate> FilteredDbSet => _userContext.IsAdmin
            ? DbSet
            : DbSet.Where(x => x.OwnerId == _userContext.Id);

        /// <inheritdoc />
        public override async Task<SensorAggregate?> GetByIdAsync(Guid aggregateId, CancellationToken cancellationToken = default)
        {
            return await FilteredDbSet
                .Include(s => s.Plot)
                    .ThenInclude(p => p.Property)
                .FirstOrDefaultAsync(s => s.Id == aggregateId, cancellationToken)
                .ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<bool> LabelExistsForPlotAsync(string label, Guid plotId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(label))
                return false;

            return await FilteredDbSet
                .AsNoTracking()
                .AnyAsync(s => s.PlotId == plotId &&
                              s.Label != null &&
                              EF.Functions.ILike(s.Label.Value, label), cancellationToken)
                .ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<bool> LabelExistsForPlotExcludingAsync(string label, Guid plotId, Guid excludeId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(label))
                return false;

            return await FilteredDbSet
                .AsNoTracking()
                .AnyAsync(s => s.PlotId == plotId &&
                    s.Id != excludeId &&
                    s.Label != null &&
                    EF.Functions.ILike(s.Label.Value, label), cancellationToken)
                .ConfigureAwait(false);
        }
    }
}
