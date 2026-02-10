namespace TC.Agro.Farm.Infrastructure.Repositories
{
    /// <summary>
    /// Read-optimized snapshot store for Owner data.
    /// Persists owner snapshots received from integration events through the message broker.
    /// This store is maintained as a projection from external Owner aggregate events.
    /// </summary>
    public sealed class OwnerSnapshotStore : IOwnerSnapshotStore
    {
        private readonly ApplicationDbContext _dbContext;

        public OwnerSnapshotStore(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }

        /// <inheritdoc />
        public async Task AddAsync(OwnerSnapshot snapshot, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(snapshot);

            await _dbContext.OwnerSnapshots.AddAsync(snapshot, cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task UpdateAsync(OwnerSnapshot snapshot, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(snapshot);

            var existingSnapshot = await _dbContext.OwnerSnapshots
                .FirstOrDefaultAsync(o => o.Id == snapshot.Id, cancellationToken)
                .ConfigureAwait(false);

            if (existingSnapshot == null)
                return;

            _dbContext.OwnerSnapshots.Update(snapshot);
        }

        /// <inheritdoc />
        public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var snapshot = await _dbContext.OwnerSnapshots
                .FirstOrDefaultAsync(o => o.Id == id, cancellationToken)
                .ConfigureAwait(false);

            if (snapshot == null)
                return;

            snapshot.Delete();
            _dbContext.OwnerSnapshots.Update(snapshot);
        }

        /// <inheritdoc />
        public async Task<OwnerSnapshot?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _dbContext.OwnerSnapshots
                .FirstOrDefaultAsync(o => o.Id == id, cancellationToken)
                .ConfigureAwait(false);
        }
    }
}
