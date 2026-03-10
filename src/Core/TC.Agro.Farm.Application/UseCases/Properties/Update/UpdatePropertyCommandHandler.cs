namespace TC.Agro.Farm.Application.UseCases.Properties.Update
{
    internal sealed class UpdatePropertyCommandHandler
        : BaseHandler<UpdatePropertyCommand, UpdatePropertyResponse>
    {
        private readonly IPropertyAggregateRepository _repository;
        private readonly ICropTypeSuggestionRepository _cropTypeRepository;
        private readonly IUserContext _userContext;
        private readonly ITransactionalOutbox _outbox;
        private readonly ILogger<UpdatePropertyCommandHandler> _logger;

        public UpdatePropertyCommandHandler(
            IPropertyAggregateRepository repository,
            ICropTypeSuggestionRepository cropTypeRepository,
            IUserContext userContext,
            ITransactionalOutbox outbox,
            ILogger<UpdatePropertyCommandHandler> logger)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _cropTypeRepository = cropTypeRepository ?? throw new ArgumentNullException(nameof(cropTypeRepository));
            _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
            _outbox = outbox ?? throw new ArgumentNullException(nameof(outbox));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public override async Task<Result<UpdatePropertyResponse>> ExecuteAsync(
            UpdatePropertyCommand command,
            CancellationToken ct = default)
        {
            _logger.LogInformation(
                "Updating property {PropertyId} by user {UserId}",
                command.Id,
                _userContext.Id);

            // 1. Load existing aggregate
            var aggregate = await _repository.GetByIdAsync(command.Id, ct).ConfigureAwait(false);
            if (aggregate is null)
            {
                _logger.LogWarning("Property {PropertyId} not found", command.Id);
                AddError(x => x.Id, "Property not found.", FarmDomainErrors.PropertyNotFound.ErrorCode);
                return BuildNotFoundResult();
            }

            // 2. Authorization check - only owner or admin can update
            if (aggregate.OwnerId != _userContext.Id && !_userContext.IsAdmin)
            {
                _logger.LogWarning(
                    "User {UserId} attempted to update property {PropertyId} owned by {OwnerId}",
                    _userContext.Id,
                    command.Id,
                    aggregate.OwnerId);
                AddError(x => x.Id, "You are not authorized to update this property.", "Property.NotAuthorized");
                return BuildNotAuthorizedResult();
            }

            // 3. Check for name uniqueness (excluding current property)
            var nameExists = await _repository
                .NameExistsForOwnerExcludingAsync(command.Name, aggregate.OwnerId, command.Id, ct)
                .ConfigureAwait(false);

            if (nameExists)
            {
                AddError(x => x.Name, $"A property with name '{command.Name}' already exists for this owner.", "Name.Duplicate");
                return BuildValidationErrorResult();
            }

            var previousLatitude = aggregate.Location.Latitude;
            var previousLongitude = aggregate.Location.Longitude;

            // 4. Apply update
            var updateResult = aggregate.Update(
                command.Name,
                command.Address,
                command.City,
                command.State,
                command.Country,
                command.AreaHectares,
                command.Latitude,
                command.Longitude);

            if (!updateResult.IsSuccess)
            {
                AddErrors(updateResult.ValidationErrors);
                return BuildValidationErrorResult();
            }

            if (HasLocationChanged(previousLatitude, previousLongitude, aggregate.Location.Latitude, aggregate.Location.Longitude))
            {
                await _cropTypeRepository
                    .MarkAiSuggestionsAsStaleByPropertyAsync(aggregate.Id, ct)
                    .ConfigureAwait(false);
            }

            // 5. Publish integration events
            await PublishIntegrationEventsAsync(aggregate, ct).ConfigureAwait(false);

            // 6. Commit
            await _outbox.SaveChangesAsync(ct).ConfigureAwait(false);

            _logger.LogInformation("Property {PropertyId} updated successfully", aggregate.Id);

            return UpdatePropertyMapper.FromAggregate(aggregate);
        }

        private async Task PublishIntegrationEventsAsync(PropertyAggregate aggregate, CancellationToken ct)
        {
            var integrationEvents = aggregate.UncommittedEvents
                .MapToIntegrationEvents(
                    aggregate: aggregate,
                    userContext: _userContext,
                    requestedOwnerId: aggregate.OwnerId,
                    handlerName: nameof(UpdatePropertyCommandHandler),
                    mappings: new Dictionary<Type, Func<BaseDomainEvent, PropertyUpdatedIntegrationEvent>>
                    {
                        { typeof(PropertyUpdatedDomainEvent), e => UpdatePropertyMapper.ToIntegrationEvent((PropertyUpdatedDomainEvent)e, aggregate.OwnerId) }
                    })
                .ToList();

            if (integrationEvents.Count > 0)
            {
                foreach (var evt in integrationEvents)
                {
                    await _outbox.EnqueueAsync(evt, ct).ConfigureAwait(false);
                }
            }

            _logger.LogInformation(
                "Enqueued {Count} integration events for property {PropertyId}",
                integrationEvents.Count,
                aggregate.Id);
        }

        private static bool HasLocationChanged(double? previousLatitude, double? previousLongitude, double? currentLatitude, double? currentLongitude)
        {
            if (previousLatitude.HasValue != currentLatitude.HasValue || previousLongitude.HasValue != currentLongitude.HasValue)
            {
                return true;
            }

            if (!previousLatitude.HasValue || !previousLongitude.HasValue || !currentLatitude.HasValue || !currentLongitude.HasValue)
            {
                return false;
            }

            const double tolerance = 0.000001;

            return Math.Abs(previousLatitude.Value - currentLatitude.Value) > tolerance ||
                   Math.Abs(previousLongitude.Value - currentLongitude.Value) > tolerance;
        }
    }
}
