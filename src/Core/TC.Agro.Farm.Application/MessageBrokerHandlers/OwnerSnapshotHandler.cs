using TC.Agro.Contracts.Events.Identity;
using TC.Agro.Farm.Domain.Snapshots;
using Wolverine;

namespace TC.Agro.Farm.Application.MessageBrokerHandlers
{
    /// <summary>
    /// Handles user integration events from the Identity microservice and maintains
    /// the OwnerSnapshot store accordingly.
    /// This class projects external user events into a read-optimized snapshot
    /// for the Farm microservice.
    /// </summary>
    public class OwnerSnapshotHandler : IWolverineHandler
    {
        private readonly IOwnerSnapshotStore _store;
        private readonly IUnitOfWork _unitOfWork;

        public OwnerSnapshotHandler(IOwnerSnapshotStore store, IUnitOfWork unitOfWork)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        // -------------------------
        // User Created
        // -------------------------
        /// <summary>
        /// Handles the UserCreatedIntegrationEvent by creating a new OwnerSnapshot.
        /// Saves the snapshot to the store.
        /// </summary>
        public async Task HandleAsync(EventContext<UserCreatedIntegrationEvent> @event, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(@event);

            // Map integration event to snapshot
            var snapshot = OwnerSnapshot.Create(
                @event.EventData.AggregateId,
                @event.EventData.Name,
                @event.EventData.Email,
                @event.EventData.OccurredOn
            );

            // Add snapshot to store
            await _store.AddAsync(snapshot, cancellationToken).ConfigureAwait(false);

            // Persist changes
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        // -------------------------
        // User Updated
        // -------------------------
        /// <summary>
        /// Handles the UserUpdatedIntegrationEvent by updating the existing OwnerSnapshot.
        /// Saves the updated snapshot to the store.
        /// </summary>
        public async Task HandleAsync(EventContext<UserUpdatedIntegrationEvent> @event, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(@event);

            // Load existing snapshot
            var snapshot = await _store.GetByIdAsync(@event.EventData.Id, cancellationToken).ConfigureAwait(false);
            if (snapshot == null)
                return;

            // Update snapshot
            snapshot.Update(@event.EventData.Name, @event.EventData.Email, isActive: true);

            // Update in store
            await _store.UpdateAsync(snapshot, cancellationToken).ConfigureAwait(false);

            // Persist changes
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        // -------------------------
        // User Deactivated
        // -------------------------
        /// <summary>
        /// Handles the UserDeactivatedIntegrationEvent by marking the OwnerSnapshot as inactive.
        /// Saves the updated snapshot to the store.
        /// </summary>
        public async Task HandleAsync(EventContext<UserDeactivatedIntegrationEvent> @event, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(@event);

            // Load existing snapshot
            var snapshot = await _store.GetByIdAsync(@event.EventData.Id, cancellationToken).ConfigureAwait(false);
            if (snapshot == null)
                return;

            // Mark as inactive
            snapshot.Delete();

            // Update in store
            await _store.UpdateAsync(snapshot, cancellationToken).ConfigureAwait(false);

            // Persist changes
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}
