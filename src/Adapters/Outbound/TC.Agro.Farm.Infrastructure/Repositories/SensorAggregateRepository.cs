using TC.Agro.Farm.Application.Abstractions.Ports;
using TC.Agro.Farm.Domain.Aggregates;

namespace TC.Agro.Farm.Infrastructure.Repositories
{
    public sealed class SensorAggregateRepository : BaseRepository<SensorAggregate>, ISensorAggregateRepository
    {
        public SensorAggregateRepository(ApplicationDbContext dbContext)
            : base(dbContext)
        {
        }

        /// <inheritdoc />
        public async Task<bool> LabelExistsForPlotAsync(string label, Guid plotId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(label))
                return false;

            return await DbSet
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

            return await DbSet
                .AsNoTracking()
                .AnyAsync(s => s.PlotId == plotId &&
                              s.Id != excludeId &&
                              s.Label != null &&
                              EF.Functions.ILike(s.Label.Value, label), cancellationToken)
                .ConfigureAwait(false);
        }
    }
}
