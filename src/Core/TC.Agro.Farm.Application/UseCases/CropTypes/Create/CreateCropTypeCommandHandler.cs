namespace TC.Agro.Farm.Application.UseCases.CropTypes.Create
{
    internal sealed class CreateCropTypeCommandHandler
        : BaseHandler<CreateCropTypeCommand, CreateCropTypeResponse>
    {
        private readonly ICropTypeSuggestionRepository _repository;
        private readonly IPropertyAggregateRepository _propertyRepository;
        private readonly IUserContext _userContext;
        private readonly ITransactionalOutbox _outbox;
        private readonly ILogger<CreateCropTypeCommandHandler> _logger;

        public CreateCropTypeCommandHandler(
            ICropTypeSuggestionRepository repository,
            IPropertyAggregateRepository propertyRepository,
            IUserContext userContext,
            ITransactionalOutbox outbox,
            ILogger<CreateCropTypeCommandHandler> logger)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _propertyRepository = propertyRepository ?? throw new ArgumentNullException(nameof(propertyRepository));
            _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
            _outbox = outbox ?? throw new ArgumentNullException(nameof(outbox));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public override async Task<Result<CreateCropTypeResponse>> ExecuteAsync(
            CreateCropTypeCommand command,
            CancellationToken ct = default)
        {
            var property = await _propertyRepository
                .GetByIdAsync(command.PropertyId, ct)
                .ConfigureAwait(false);

            if (property is null)
            {
                AddError(x => x.PropertyId, "Property not found.", FarmDomainErrors.PropertyNotFound.ErrorCode);
                return BuildNotFoundResult();
            }

            if (property.OwnerId != _userContext.Id && !_userContext.IsAdmin)
            {
                AddError(x => x.PropertyId, "You are not authorized to create crop types for this property.", "CropTypeSuggestion.NotAuthorized");
                return BuildNotAuthorizedResult();
            }

            var aggregateResult = CropTypeSuggestionAggregate.CreateManual(
                propertyId: property.Id,
                ownerId: property.OwnerId,
                cropType: command.CropType,
                plantingWindow: command.PlantingWindow,
                harvestCycleMonths: command.HarvestCycleMonths,
                suggestedIrrigationType: command.SuggestedIrrigationType,
                minSoilMoisture: command.MinSoilMoisture,
                maxTemperature: command.MaxTemperature,
                minHumidity: command.MinHumidity,
                notes: command.Notes);

            if (!aggregateResult.IsSuccess)
            {
                AddErrors(aggregateResult.ValidationErrors);
                return BuildValidationErrorResult();
            }

            var aggregate = aggregateResult.Value;
            _repository.Add(aggregate);
            await _outbox.SaveChangesAsync(ct).ConfigureAwait(false);

            _logger.LogInformation(
                "Manual crop type suggestion {CropTypeSuggestionId} created for property {PropertyId}",
                aggregate.Id,
                aggregate.PropertyId);

            return new CreateCropTypeResponse(
                aggregate.Id,
                aggregate.PropertyId,
                aggregate.OwnerId,
                aggregate.CropName.Value,
                aggregate.Source,
                aggregate.IsOverride,
                aggregate.IsStale,
                aggregate.ConfidenceScore,
                aggregate.PlantingWindow,
                aggregate.HarvestCycleMonths,
                aggregate.SuggestedIrrigationType,
                aggregate.MinSoilMoisture,
                aggregate.MaxTemperature,
                aggregate.MinHumidity,
                aggregate.Notes,
                aggregate.Model,
                aggregate.GeneratedAt,
                aggregate.CreatedAt);
        }
    }
}
