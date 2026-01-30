using Microsoft.EntityFrameworkCore;
using TC.Agro.Farm.Application.Abstractions.Ports;
using TC.Agro.Farm.Domain.Aggregates;

namespace TC.Agro.Farm.Infrastructure.Repositories
{
    public sealed class PlotAggregateRepository : BaseRepository<PlotAggregate>, IPlotAggregateRepository
    {
        public PlotAggregateRepository(ApplicationDbContext dbContext)
            : base(dbContext)
        {
        }

        /// <inheritdoc />
        public async Task<bool> NameExistsForPropertyAsync(string name, Guid propertyId, CancellationToken cancellationToken = default)
        {
            return await DbSet
                .AsNoTracking()
                .AnyAsync(p => p.PropertyId == propertyId && 
                              p.IsActive && 
                              EF.Functions.ILike(p.Name.Value, name), cancellationToken)
                .ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<bool> NameExistsForPropertyExcludingAsync(string name, Guid propertyId, Guid excludeId, CancellationToken cancellationToken = default)
        {
            return await DbSet
                .AsNoTracking()
                .AnyAsync(p => p.PropertyId == propertyId && 
                              p.Id != excludeId && 
                              p.IsActive && 
                              EF.Functions.ILike(p.Name.Value, name), cancellationToken)
                .ConfigureAwait(false);
        }
    }
}
