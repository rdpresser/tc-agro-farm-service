namespace TC.Agro.Farm.Application.UseCases.Sensors.Create
{
    internal sealed class CreateSensorCommandHandler
        : BaseCommandHandler<CreateSensorCommand, CreateSensorResponse, SensorAggregate, ISensorAggregateRepository>
    {
        private readonly IPlotAggregateRepository _plotRepository;
        private readonly IPlotReadStore _plotReadStore;
        private readonly ILogger<CreateSensorCommandHandler> _logger;

        public CreateSensorCommandHandler(
            ISensorAggregateRepository repository,
            IPlotAggregateRepository plotRepository,
            IPlotReadStore plotReadStore,
            IUserContext userContext,
            ITransactionalOutbox outbox,
            ILogger<CreateSensorCommandHandler> logger)
            : base(repository, userContext, outbox, logger)
        {
            _plotRepository = plotRepository ?? throw new ArgumentNullException(nameof(plotRepository));
            _plotReadStore = plotReadStore ?? throw new ArgumentNullException(nameof(plotReadStore));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        protected override async Task<Result<SensorAggregate>> MapAsync(CreateSensorCommand command, CancellationToken ct)
        {
            var ownerId = UserContext.Id;
            var searchPlot = await _plotReadStore.GetByIdAsync(command.PlotId, ct);

            if (searchPlot == null)
            {
                return Result.Invalid(FarmDomainErrors.PlotNotFound);
            }

            var propertyId = searchPlot.PropertyId;
            var propertyName = searchPlot.PropertyName;
            var plotName = searchPlot.Name;


            var aggregateResult = CreateSensorMapper.ToAggregate(command, ownerId, propertyId, plotId: command.PlotId, propertyName, plotName);
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
                    handlerName: nameof(CreateSensorCommandHandler),
                    mappings: new Dictionary<Type, Func<BaseDomainEvent, SensorRegisteredIntegrationEvent>>
                    {
                        { typeof(SensorRegisteredDomainEvent), e => CreateSensorMapper.ToIntegrationEvent((SensorRegisteredDomainEvent)e, UserContext.Id) }
                    });

            foreach (var evt in integrationEvents)
            {
                await Outbox.EnqueueAsync(evt, ct).ConfigureAwait(false);
            }

            _logger.LogInformation(
                "Enqueued {Count} integration events for sensor {SensorId}",
                integrationEvents.Count(),
                aggregate.Id);
        }

        protected override Task<CreateSensorResponse> BuildResponseAsync(SensorAggregate aggregate, CancellationToken ct)
            => Task.FromResult(CreateSensorMapper.FromAggregate(aggregate));
    }
}
