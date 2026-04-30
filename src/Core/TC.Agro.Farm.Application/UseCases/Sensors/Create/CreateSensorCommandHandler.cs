namespace TC.Agro.Farm.Application.UseCases.Sensors.Create
{
    internal sealed class CreateSensorCommandHandler
        : BaseCommandHandler<CreateSensorCommand, CreateSensorResponse, SensorAggregate, ISensorAggregateRepository>
    {
        private readonly IPlotAggregateRepository _plotRepository;
        private readonly IPropertyAggregateRepository _propertyRepository;
        private readonly ILogger<CreateSensorCommandHandler> _logger;

        public CreateSensorCommandHandler(
            ISensorAggregateRepository repository,
            IPlotAggregateRepository plotRepository,
            IPropertyAggregateRepository propertyRepository,
            IUserContext userContext,
            ITransactionalOutbox outbox,
            ILogger<CreateSensorCommandHandler> logger)
            : base(repository, userContext, outbox, logger)
        {
            _plotRepository = plotRepository ?? throw new ArgumentNullException(nameof(plotRepository));
            _propertyRepository = propertyRepository ?? throw new ArgumentNullException(nameof(propertyRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        protected override async Task<Result<SensorAggregate>> MapAsync(CreateSensorCommand command, CancellationToken ct)
        {
            var ownerIdResult = ResolveEffectiveOwnerId(command.OwnerId);
            if (!ownerIdResult.IsSuccess)
            {
                return Result<SensorAggregate>.Invalid(ownerIdResult.ValidationErrors);
            }

            var ownerId = ownerIdResult.Value;
            var plot = await _plotRepository.GetByIdAsync(command.PlotId, ct).ConfigureAwait(false);

            if (plot == null)
            {
                return Result.Invalid(FarmDomainErrors.PlotNotFound);
            }

            var propertyId = plot.PropertyId;
            var property = await _propertyRepository.GetByIdAsync(propertyId, ct).ConfigureAwait(false);

            if (property is null)
            {
                return Result.Invalid(FarmDomainErrors.PropertyNotFound);
            }

            if (property.OwnerId != ownerId)
            {
                return Result.Invalid(new ValidationError(
                    nameof(CreateSensorCommand.OwnerId),
                    "Selected OwnerId does not match the property owner for the target plot."));
            }

            var aggregateResult = CreateSensorMapper.ToAggregate(
                command: command,
                ownerId: ownerId,
                propertyId: propertyId,
                plotId: command.PlotId,
                propertyName: property.Name.Value,
                plotName: plot.Name.Value,
                plotLatitude: plot.Latitude ?? property.Location.Latitude,
                plotLongitude: plot.Longitude ?? property.Location.Longitude,
                plotBoundaryGeoJson: plot.BoundaryGeoJson);

            return aggregateResult;
        }

        protected override async Task<Result> ValidateAsync(SensorAggregate aggregate, CancellationToken ct)
        {
            // 1. Check if plot exists
            var plot = await _plotRepository
                .GetByIdAsync(aggregate.PlotId, ct)
                .ConfigureAwait(false);

            if (plot is null)
            {
                return Result.Invalid(FarmDomainErrors.PlotNotFound);
            }

            // 2. Check label uniqueness if provided
            if (aggregate.Label is not null)
            {
                var labelExists = await Repository
                    .LabelExistsForPlotAsync(aggregate.Label.Value, aggregate.PlotId, ct)
                    .ConfigureAwait(false);

                if (labelExists)
                {
                    return Result.Invalid(new ValidationError(
                        "Label",
                        $"A sensor with label '{aggregate.Label.Value}' already exists for this plot."));
                }
            }

            return Result.Success();
        }

        protected override async Task PublishIntegrationEventsAsync(SensorAggregate aggregate, CancellationToken ct)
        {
            var integrationEvents = aggregate.UncommittedEvents
                .MapToIntegrationEvents(
                    aggregate: aggregate,
                    userContext: UserContext,
                    requestedOwnerId: aggregate.OwnerId,
                    handlerName: nameof(CreateSensorCommandHandler),
                    mappings: new Dictionary<Type, Func<BaseDomainEvent, SensorRegisteredIntegrationEvent>>
                    {
                        { typeof(SensorRegisteredDomainEvent), e => CreateSensorMapper.ToIntegrationEvent((SensorRegisteredDomainEvent)e) }
                    })
                .ToList();

            if (integrationEvents.Count > 0)
            {
                foreach (var evt in integrationEvents)
                {
                    await Outbox.EnqueueAsync(evt, ct).ConfigureAwait(false);
                }
            }

            _logger.LogInformation(
                "Enqueued {Count} integration events for sensor {SensorId}",
                integrationEvents.Count,
                aggregate.Id);
        }

        protected override Task<CreateSensorResponse> BuildResponseAsync(SensorAggregate aggregate, CancellationToken ct)
            => Task.FromResult(CreateSensorMapper.FromAggregate(aggregate));

        private Result<Guid> ResolveEffectiveOwnerId(Guid? requestedOwnerId)
        {
            var isAdmin = UserContext.IsAdmin;

            if (isAdmin)
            {
                if (!requestedOwnerId.HasValue || requestedOwnerId.Value == Guid.Empty)
                {
                    return Result<Guid>.Invalid(new ValidationError(
                        nameof(CreateSensorCommand.OwnerId),
                        "OwnerId is required when creating sensor on behalf as Admin."));
                }

                return Result.Success(requestedOwnerId.Value);
            }

            return Result.Success(UserContext.Id);
        }
    }
}
