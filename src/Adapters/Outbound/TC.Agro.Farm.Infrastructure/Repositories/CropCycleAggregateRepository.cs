using TC.Agro.Farm.Domain.ValueObjects;

namespace TC.Agro.Farm.Infrastructure.Repositories
{
    public sealed class CropCycleAggregateRepository : BaseRepository<CropCycleAggregate>, ICropCycleAggregateRepository
    {
        private readonly IUserContext _userContext;

        public CropCycleAggregateRepository(ApplicationDbContext dbContext, IUserContext userContext)
            : base(dbContext)
        {
            _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
        }

        private IQueryable<CropCycleAggregate> FilteredDbSet => _userContext.IsAdmin
            ? DbSet
            : DbSet.Where(x => x.OwnerId == _userContext.Id);

        public async Task<bool> HasActiveCyclesByPropertyAsync(Guid propertyId, CancellationToken cancellationToken = default)
        {
            var activeStatuses = CropCycleStatus.GetActiveStatuses();

            return await FilteredDbSet
                .AsNoTracking()
                .AnyAsync(
                    cycle => cycle.PropertyId == propertyId && activeStatuses.Contains(cycle.Status.Value),
                    cancellationToken)
                .ConfigureAwait(false);
        }

        public async Task<bool> HasActiveCyclesByPlotAsync(
            Guid plotId,
            Guid? excludingCycleId = null,
            CancellationToken cancellationToken = default)
        {
            var activeStatuses = CropCycleStatus.GetActiveStatuses();
            var query = FilteredDbSet
                .AsNoTracking()
                .Where(cycle => cycle.PlotId == plotId && activeStatuses.Contains(cycle.Status.Value));

            if (excludingCycleId.HasValue && excludingCycleId.Value != Guid.Empty)
            {
                query = query.Where(cycle => cycle.Id != excludingCycleId.Value);
            }

            return await query.AnyAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}
