namespace TC.Agro.Farm.Application.UseCases.Properties.Create
{
    internal sealed class CreatePropertyCommandHandler
        : BaseCommandHandler<CreatePropertyCommand, CreatePropertyResponse, PropertyAggregate, IPropertyAggregateRepository>
    {
        private readonly ILogger<CreatePropertyCommandHandler> _logger;

        public CreatePropertyCommandHandler(
            IPropertyAggregateRepository repository,
            IUserContext userContext,
            ITransactionalOutbox outbox,
            ILogger<CreatePropertyCommandHandler> logger)
            : base(repository, userContext, outbox, logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        protected override Task<Result<PropertyAggregate>> MapAsync(CreatePropertyCommand command, CancellationToken ct)
        {
            var aggregateResult = CreatePropertyMapper.ToAggregate(command, UserContext.Id);
            return Task.FromResult(aggregateResult);
        }

        protected override async Task<Result> ValidateAsync(PropertyAggregate aggregate, CancellationToken ct)
        {
            // Check if property name already exists for this owner
            var exists = await Repository
                .NameExistsForOwnerAsync(aggregate.Name.Value, aggregate.OwnerId, ct)
                .ConfigureAwait(false);

            if (exists)
            {
                return Result.Invalid(new ValidationError(
                    "Name",
                    $"A property with name '{aggregate.Name.Value}' already exists for this owner."));
            }

            return Result.Success();
        }

        protected override async Task PublishIntegrationEventsAsync(PropertyAggregate aggregate, CancellationToken ct)
        {
            var integrationEvents = aggregate.UncommittedEvents
                .MapToIntegrationEvents(
                    aggregate: aggregate,
                    userContext: UserContext,
                    handlerName: nameof(CreatePropertyCommandHandler),
                    mappings: new Dictionary<Type, Func<BaseDomainEvent, PropertyCreatedIntegrationEvent>>
                    {
                        { typeof(PropertyCreatedDomainEvent), e => CreatePropertyMapper.ToIntegrationEvent((PropertyCreatedDomainEvent)e) }
                    });

            foreach (var evt in integrationEvents)
            {
                await Outbox.EnqueueAsync(evt, ct).ConfigureAwait(false);
            }

            _logger.LogInformation(
                "Enqueued {Count} integration events for property {PropertyId}",
                integrationEvents.Count(),
                aggregate.Id);
        }

        protected override Task<CreatePropertyResponse> BuildResponseAsync(PropertyAggregate aggregate, CancellationToken ct)
            => Task.FromResult(CreatePropertyMapper.FromAggregate(aggregate));
    }
}
