namespace TC.Agro.Farm.Application.UseCases.Plots.Create
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
            var ownerIdResult = ResolveEffectiveOwnerId(command.OwnerId);
            if (!ownerIdResult.IsSuccess)
            {
                return Task.FromResult(Result<PlotAggregate>.Invalid(ownerIdResult.ValidationErrors));
            }

            var aggregateResult = CreatePlotMapper.ToAggregate(command, ownerIdResult.Value);
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

            if (property.OwnerId != aggregate.OwnerId)
            {
                return Result.Invalid(new ValidationError(
                    nameof(CreatePlotCommand.OwnerId),
                    "Selected OwnerId does not match the property owner."));
            }

            // 2. Authorization: only property owner or admin can create plots
            if (property.OwnerId != UserContext.Id && !UserContext.IsAdmin)
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

        private Result<Guid> ResolveEffectiveOwnerId(Guid? requestedOwnerId)
        {
            var isAdmin = UserContext.IsAdmin;

            if (isAdmin)
            {
                if (!requestedOwnerId.HasValue || requestedOwnerId.Value == Guid.Empty)
                {
                    return Result<Guid>.Invalid(new ValidationError(
                        nameof(CreatePlotCommand.OwnerId),
                        "OwnerId is required when creating plot on behalf as Admin."));
                }

                return Result.Success(requestedOwnerId.Value);
            }

            return Result.Success(UserContext.Id);
        }

        protected override async Task PublishIntegrationEventsAsync(PlotAggregate aggregate, CancellationToken ct)
        {
            var integrationEvents = aggregate.UncommittedEvents
                .MapToIntegrationEvents(
                    aggregate: aggregate,
                    userContext: UserContext,
                    requestedOwnerId: aggregate.OwnerId,
                    handlerName: nameof(CreatePlotCommandHandler),
                    mappings: new Dictionary<Type, Func<BaseDomainEvent, PlotCreatedIntegrationEvent>>
                    {
                        { typeof(PlotCreatedDomainEvent), e => CreatePlotMapper.ToIntegrationEvent((PlotCreatedDomainEvent)e) }
                    })
                .ToList();

            if (integrationEvents.Count > 0)
            {
                await Outbox.EnqueueAsync(integrationEvents, ct).ConfigureAwait(false);
            }

            _logger.LogInformation(
                "Enqueued {Count} integration events for plot {PlotId}",
                integrationEvents.Count,
                aggregate.Id);
        }

        protected override Task<CreatePlotResponse> BuildResponseAsync(PlotAggregate aggregate, CancellationToken ct)
            => Task.FromResult(CreatePlotMapper.FromAggregate(aggregate));
    }
}
