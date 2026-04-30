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
            var ownerIdResult = ResolveEffectiveOwnerId(command.OwnerId);
            if (!ownerIdResult.IsSuccess)
            {
                return Task.FromResult(Result<PropertyAggregate>.Invalid(ownerIdResult.ValidationErrors));
            }

            var aggregateResult = CreatePropertyMapper.ToAggregate(command, ownerIdResult.Value);
            return Task.FromResult(aggregateResult);
        }

        private Result<Guid> ResolveEffectiveOwnerId(Guid? requestedOwnerId)
        {
            var isAdmin = UserContext.IsAdmin;

            if (isAdmin)
            {
                if (!requestedOwnerId.HasValue || requestedOwnerId.Value == Guid.Empty)
                {
                    return Result<Guid>.Invalid(new ValidationError(
                        nameof(CreatePropertyCommand.OwnerId),
                        "OwnerId is required when creating property on behalf as Admin."));
                }

                return Result.Success(requestedOwnerId.Value);
            }

            return Result.Success(UserContext.Id);
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
                    requestedOwnerId: aggregate.OwnerId,
                    handlerName: nameof(CreatePropertyCommandHandler),
                    mappings: new Dictionary<Type, Func<BaseDomainEvent, PropertyCreatedIntegrationEvent>>
                    {
                        { typeof(PropertyCreatedDomainEvent), e => CreatePropertyMapper.ToIntegrationEvent((PropertyCreatedDomainEvent)e) }
                    })
                .ToList();

            if (integrationEvents.Count > 0)
            {
                foreach (var evt in integrationEvents)
                {
                    await Outbox.EnqueueAsync(evt, ct).ConfigureAwait(false);
                }
            }

            if (aggregate.Location.Latitude.HasValue && aggregate.Location.Longitude.HasValue)
            {
                var triggerUserId = UserContext.Id == Guid.Empty ? aggregate.OwnerId : UserContext.Id;

                await Outbox.EnqueueAsync(
                    new CropTypes.Regenerate.GeneratePropertyCropTypeSuggestionsMessage(
                        PropertyId: aggregate.Id,
                        OwnerId: aggregate.OwnerId,
                        TriggeredByUserId: triggerUserId,
                        TriggerReason: "property-created",
                        RequestedAt: DateTimeOffset.UtcNow),
                    ct).ConfigureAwait(false);
            }
            else
            {
                _logger.LogInformation(
                    "Skipping crop suggestion generation queue for property {PropertyId} because coordinates are missing.",
                    aggregate.Id);
            }

            _logger.LogInformation(
                "Enqueued {Count} integration events for property {PropertyId}",
                integrationEvents.Count,
                aggregate.Id);
        }

        protected override Task<CreatePropertyResponse> BuildResponseAsync(PropertyAggregate aggregate, CancellationToken ct)
            => Task.FromResult(CreatePropertyMapper.FromAggregate(aggregate));
    }
}
