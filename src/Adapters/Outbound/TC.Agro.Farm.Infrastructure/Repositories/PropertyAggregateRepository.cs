using Microsoft.EntityFrameworkCore;
using TC.Agro.Farm.Application.Abstractions.Ports;
using TC.Agro.Farm.Domain.Aggregates;

namespace TC.Agro.Farm.Infrastructure.Repositories
{
    public sealed class PropertyAggregateRepository : BaseRepository<PropertyAggregate>, IPropertyAggregateRepository
    {
        public PropertyAggregateRepository(ApplicationDbContext dbContext)
            : base(dbContext)
        {
        }

        /// <inheritdoc />
        public async Task<bool> NameExistsForOwnerAsync(string name, Guid ownerId, CancellationToken cancellationToken = default)
        {
            return await DbSet
                .AsNoTracking()
                .AnyAsync(p => p.OwnerId == ownerId && 
                              p.IsActive && 
                              EF.Functions.ILike(p.Name.Value, name), cancellationToken)
                .ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<bool> NameExistsForOwnerExcludingAsync(string name, Guid ownerId, Guid excludeId, CancellationToken cancellationToken = default)
        {
            return await DbSet
                .AsNoTracking()
                .AnyAsync(p => p.OwnerId == ownerId && 
                              p.Id != excludeId && 
                              p.IsActive && 
                              EF.Functions.ILike(p.Name.Value, name), cancellationToken)
                .ConfigureAwait(false);
        }
    }
}
