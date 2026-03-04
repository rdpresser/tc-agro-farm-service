namespace TC.Agro.Farm.Application.UseCases.Sensors.Deactivate
{
    /// <summary>
    /// Command handler for deactivating (soft-deleting) a sensor.
    ///
    /// Flow:
    /// 1. Validates command via FluentValidation
    /// 2. Loads sensor aggregate from repository
    /// 3. Calls Deactivate() domain method which sets IsActive = false
    /// 4. Converts domain events to integration events
    /// 5. Enqueues to Outbox (Wolverine+EF Core transaction boundary)
    /// 6. Returns response with sensor ID and deactivation timestamp
    ///
    /// Integration with other services:
    /// - Sensor Ingest Service: consumes SensorDeactivatedIntegrationEvent to stop data collection
    /// - Analytics Worker: may trigger cleanup or archival processes
    /// - Dashboard: real-time updates via SignalR subscription to this event
    /// </summary>
    internal sealed class DeactivateSensorCommandHandler
        : BaseCommandHandler<DeactivateSensorCommand, DeactivateSensorResponse, SensorAggregate, ISensorAggregateRepository>
    {
        private readonly ILogger<DeactivateSensorCommandHandler> _logger;
        private string? _reason;

        /// <summary>
        /// Command handler for deactivating (soft-deleting) a sensor.
        /// </summary>
        public DeactivateSensorCommandHandler(
            ISensorAggregateRepository repository,
            IUserContext userContext,
            ITransactionalOutbox outbox,
            ILogger<DeactivateSensorCommandHandler> logger)
            : base(repository, userContext, outbox, logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Loads sensor aggregate by ID and deactivates it.
        /// Fails if sensor not found or already deactivated.
        /// </summary>
        protected override async Task<Result<SensorAggregate>> MapAsync(DeactivateSensorCommand command, CancellationToken ct)
        {
            _reason = command.Reason;
            var sensor = await Repository.GetByIdAsync(command.SensorId, ct);

            if (sensor is null)
            {
                _logger.LogWarning(
                    "Attempted to deactivate non-existent sensor {SensorId}",
                    command.SensorId);
                return Result.Invalid(FarmDomainErrors.SensorNotFound);
            }

            if (!sensor.IsActive)
            {
                _logger.LogWarning(
                    "Attempted to deactivate already deactivated sensor {SensorId}",
                    command.SensorId);
                return Result.Invalid(FarmDomainErrors.SensorAlreadyDeactivated);
            }

            var deactivateResult = sensor.Deactivate();

            if (!deactivateResult.IsSuccess)
            {
                _logger.LogWarning(
                    "Failed to deactivate sensor {SensorId}: {ErrorCount} error(s)",
                    command.SensorId,
                    deactivateResult.Errors.Count());
                return deactivateResult;
            }

            return Result.Success(sensor);
        }

        /// <summary>
        /// Override to skip Repository.Add() since the aggregate was already loaded
        /// and tracked by EF Core in MapAsync. The change tracker will detect
        /// the modifications automatically on SaveChangesAsync.
        /// </summary>
        protected override Task PersistAsync(SensorAggregate aggregate, CancellationToken ct)
            => Task.CompletedTask;

        /// <summary>
        /// Performs additional business-rule validation after the aggregate is loaded and updated.
        /// For this handler, all required validation is performed in <see cref="MapAsync" />,
        /// so this method currently performs no additional checks.
        /// </summary>
        protected override Task<Result> ValidateAsync(SensorAggregate aggregate, CancellationToken ct)
        {
            return Task.FromResult(Result.Success());
        }

        /// <summary>
        /// Publishes integration events after successful deactivation.
        /// </summary>
        protected override async Task PublishIntegrationEventsAsync(SensorAggregate aggregate, CancellationToken ct)
        {
            var integrationEvents = aggregate.UncommittedEvents
                .MapToIntegrationEvents(
                    aggregate: aggregate,
                    userContext: UserContext,
                    requestedOwnerId: aggregate.OwnerId,
                    handlerName: nameof(DeactivateSensorCommandHandler),
                    mappings: new Dictionary<Type, Func<BaseDomainEvent, SensorDeactivatedIntegrationEvent>>
                    {
                        { typeof(SensorAggregate.SensorDeactivatedDomainEvent), e =>
                            DeactivateSensorMapper.ToIntegrationEvent(
                                (SensorAggregate.SensorDeactivatedDomainEvent)e,
                                aggregate,
                                UserContext.Id,
                                _reason) }
                    })
                .ToList();

            if (integrationEvents.Count > 0)
            {
                await Outbox.EnqueueAsync(integrationEvents, ct).ConfigureAwait(false);
            }

            _logger.LogInformation(
                "Enqueued {Count} integration events for sensor {SensorId} deactivation",
                integrationEvents.Count,
                aggregate.Id);
        }

        /// <summary>
        /// Builds response with sensor ID and deactivation timestamp.
        /// </summary>
        protected override Task<DeactivateSensorResponse> BuildResponseAsync(SensorAggregate aggregate, CancellationToken ct)
        {
            var deactivatedAt = aggregate.UpdatedAt ?? DateTimeOffset.UtcNow;

            return Task.FromResult(DeactivateSensorMapper.FromAggregate(aggregate, deactivatedAt));
        }
    }
}
