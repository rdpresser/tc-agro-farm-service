namespace TC.Agro.Farm.Application.UseCases.Plots.CreatePlot
{
    internal sealed class CreatePlotCommandHandler
        : BaseCommandHandler<CreatePlotCommand, CreatePlotResponse, PlotAggregate, IPlotAggregateRepository>
    {
        private readonly IPropertyAggregateRepository _propertyRepository;
        private readonly ILogger<CreatePlotCommandHandler> _logger;

        public CreatePlotCommandHandler(
            IPlotAggregateRepository repository,
            IPropertyAggregateRepository propertyRepository,
            IUserContext userContext,
            ITransactionalOutbox outbox,
            ILogger<CreatePlotCommandHandler> logger)
            : base(repository, userContext, outbox, logger)
        {
            _propertyRepository = propertyRepository ?? throw new ArgumentNullException(nameof(propertyRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        protected override Task<Result<PlotAggregate>> MapAsync(CreatePlotCommand command, CancellationToken ct)
        {
            var aggregateResult = CreatePlotMapper.ToAggregate(command);
            return Task.FromResult(aggregateResult);
        }

        protected override async Task<Result> ValidateAsync(PlotAggregate aggregate, CancellationToken ct)
        {
            // 1. Check if property exists
            var property = await _propertyRepository
                .GetByIdAsync(aggregate.PropertyId, ct)
                .ConfigureAwait(false);

            if (property is null)
            {
                return Result.Invalid(FarmDomainErrors.PropertyNotFound);
            }

            // 2. Authorization: only property owner or admin can create plots
            if (property.OwnerId != UserContext.Id && UserContext.Role != AppConstants.AdminRole)
            {
                _logger.LogWarning(
                    "User {UserId} attempted to create plot in property {PropertyId} owned by {OwnerId}",
                    UserContext.Id,
                    aggregate.PropertyId,
                    property.OwnerId);
                return Result.Forbidden();
            }

            // 3. Check if plot name already exists for this property
            var nameExists = await Repository
                .NameExistsForPropertyAsync(aggregate.Name.Value, aggregate.PropertyId, ct)
                .ConfigureAwait(false);

            if (nameExists)
            {
                return Result.Invalid(new ValidationError(
                    "Name",
                    $"A plot with name '{aggregate.Name.Value}' already exists for this property."));
            }

            return Result.Success();
        }

        protected override async Task PublishIntegrationEventsAsync(PlotAggregate aggregate, CancellationToken ct)
        {
            var integrationEvents = aggregate.UncommittedEvents
                .MapToIntegrationEvents(
                    aggregate: aggregate,
                    userContext: UserContext,
                    handlerName: nameof(CreatePlotCommandHandler),
                    mappings: new Dictionary<Type, Func<BaseDomainEvent, PlotCreatedIntegrationEvent>>
                    {
                        { typeof(PlotCreatedDomainEvent), e => CreatePlotMapper.ToIntegrationEvent((PlotCreatedDomainEvent)e) }
                    });

            foreach (var evt in integrationEvents)
            {
                await Outbox.EnqueueAsync(evt, ct).ConfigureAwait(false);
            }

            _logger.LogInformation(
                "Enqueued {Count} integration events for plot {PlotId}",
                integrationEvents.Count(),
                aggregate.Id);
        }

        protected override Task<CreatePlotResponse> BuildResponseAsync(PlotAggregate aggregate, CancellationToken ct)
            => Task.FromResult(CreatePlotMapper.FromAggregate(aggregate));
    }
}
