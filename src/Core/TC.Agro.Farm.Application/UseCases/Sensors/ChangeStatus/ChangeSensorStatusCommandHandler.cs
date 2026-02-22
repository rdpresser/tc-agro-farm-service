namespace TC.Agro.Farm.Application.UseCases.Sensors.ChangeStatus
{
    /// <summary>
    /// Command handler for changing sensor operational status.
    /// 
    /// Flow:
    /// 1. Validates command via FluentValidation
    /// 2. Loads sensor aggregate from repository
    /// 3. Captures current status before transition
    /// 4. Applies status change via appropriate domain method (SetActive, SetMaintenance, etc.)
    /// 5. Converts domain events to integration events
    /// 6. Enqueues to Outbox (Wolverine+EF Core transaction boundary)
    /// 7. Returns response with before/after status
    /// 
    /// Integration with other services:
    /// - Sensor Ingest Service: consumes SensorOperationalStatusChangedIntegrationEvent
    /// - Analytics Worker: may trigger alert rules based on status change
    /// - Dashboard: real-time updates via SignalR subscription to this event
    /// </summary>
    internal sealed class ChangeSensorStatusCommandHandler
        : BaseCommandHandler<ChangeSensorStatusCommand, ChangeSensorStatusResponse, SensorAggregate, ISensorAggregateRepository>
    {
        private readonly ILogger<ChangeSensorStatusCommandHandler> _logger;
        private string? _reason;

        public ChangeSensorStatusCommandHandler(
            ISensorAggregateRepository repository,
            IUserContext userContext,
            ITransactionalOutbox outbox,
            ILogger<ChangeSensorStatusCommandHandler> logger)
            : base(repository, userContext, outbox, logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Loads sensor aggregate by ID, captures current status, and applies the requested status change.
        /// Fails if sensor not found or is deactivated.
        /// </summary>
        protected override async Task<Result<SensorAggregate>> MapAsync(ChangeSensorStatusCommand command, CancellationToken ct)
        {
            _reason = command.Reason;
            var sensor = await Repository.GetByIdAsync(command.SensorId, ct);

            if (sensor is null)
            {
                _logger.LogWarning(
                    "Attempted to change status of non-existent sensor {SensorId}",
                    command.SensorId);
                return Result.Invalid(FarmDomainErrors.SensorNotFound);
            }

            if (!sensor.IsActive)
            {
                _logger.LogWarning(
                    "Attempted to change status of deactivated sensor {SensorId}",
                    command.SensorId);
                return Result.Invalid(FarmDomainErrors.SensorAlreadyDeactivated);
            }

            // Apply the requested status change via domain method
            Result statusChangeResult;
            if (command.NewStatus.Equals("Active", StringComparison.OrdinalIgnoreCase))
                statusChangeResult = sensor.SetActive();
            else if (command.NewStatus.Equals("Inactive", StringComparison.OrdinalIgnoreCase))
                statusChangeResult = sensor.SetInactive();
            else if (command.NewStatus.Equals("Maintenance", StringComparison.OrdinalIgnoreCase))
                statusChangeResult = sensor.SetMaintenance();
            else if (command.NewStatus.Equals("Faulty", StringComparison.OrdinalIgnoreCase))
                statusChangeResult = sensor.SetFaulty();
            else
            {
                _logger.LogWarning(
                    "Invalid sensor status requested: {NewStatus}",
                    command.NewStatus);
                return Result.Invalid(FarmDomainErrors.InvalidSensorStatus);
            }

            if (!statusChangeResult.IsSuccess)
            {
                _logger.LogWarning(
                    "Failed to change sensor {SensorId} status to {NewStatus}: {ErrorCount} error(s)",
                    command.SensorId,
                    command.NewStatus,
                    statusChangeResult.Errors.Count());
                return statusChangeResult;
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
        /// Publishes integration events after successful status change.
        /// </summary>
        protected override async Task PublishIntegrationEventsAsync(SensorAggregate aggregate, CancellationToken ct)
        {
            var integrationEvents = aggregate.UncommittedEvents
                .MapToIntegrationEvents(
                    aggregate: aggregate,
                    userContext: UserContext,
                    handlerName: nameof(ChangeSensorStatusCommandHandler),
                    mappings: new Dictionary<Type, Func<BaseDomainEvent, SensorOperationalStatusChangedIntegrationEvent>>
                    {
                        { typeof(SensorAggregate.SensorStatusChangedDomainEvent), e => 
                            ChangeSensorStatusMapper.ToIntegrationEvent(
                                (SensorAggregate.SensorStatusChangedDomainEvent)e, 
                                aggregate, 
                                UserContext.Id,
                                _reason) }
                    });

            foreach (var evt in integrationEvents)
            {
                await Outbox.EnqueueAsync(evt, ct).ConfigureAwait(false);
            }

            _logger.LogInformation(
                "Enqueued {Count} integration events for sensor {SensorId} status change",
                integrationEvents.Count(),
                aggregate.Id);
        }

        /// <summary>
        /// Builds response with before/after status.
        /// </summary>
        protected override Task<ChangeSensorStatusResponse> BuildResponseAsync(SensorAggregate aggregate, CancellationToken ct)
            => Task.FromResult(ChangeSensorStatusMapper.FromAggregate(aggregate));
    }
}
