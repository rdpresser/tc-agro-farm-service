namespace TC.Agro.Farm.Application.UseCases.CropTypes.Create
{
    internal sealed class CreateCropTypeCommandHandler
        : BaseHandler<CreateCropTypeCommand, CreateCropTypeResponse>
    {
        private readonly ICropTypeCatalogRepository _repository;
        private readonly IPropertyAggregateRepository _propertyRepository;
        private readonly IUserContext _userContext;
        private readonly ITransactionalOutbox _outbox;
        private readonly ILogger<CreateCropTypeCommandHandler> _logger;

        public CreateCropTypeCommandHandler(
            ICropTypeCatalogRepository repository,
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
                AddError(x => x.PropertyId, "You are not authorized to create crop types for this property.", "CropTypeCatalog.NotAuthorized");
                return BuildNotAuthorizedResult();
            }

            var existingEntry = await _repository
                .GetByNameAsync(command.CropType, property.OwnerId, ct)
                .ConfigureAwait(false);

            if (existingEntry is not null && existingEntry.OwnerId == property.OwnerId)
            {
                AddError(x => x.CropType, "A crop type catalog entry with this name already exists for this owner.", "CropTypeCatalog.NameAlreadyExists");
                return BuildValidationErrorResult();
            }

            var (startMonth, endMonth) = CropTypeCatalogCommandMapping.ParsePlantingWindow(command.PlantingWindow);

            var aggregateResult = CropTypeCatalogAggregate.Create(
                cropTypeName: command.CropType,
                isSystemDefined: false,
                description: command.Notes,
                recommendedIrrigationType: command.SuggestedIrrigationType,
                typicalHarvestCycleMonths: command.HarvestCycleMonths,
                typicalPlantingStartMonth: startMonth,
                typicalPlantingEndMonth: endMonth,
                maxTemperature: command.MaxTemperature,
                minHumidity: command.MinHumidity,
                minSoilMoisture: command.MinSoilMoisture,
                suggestedImage: command.SuggestedImage,
                ownerId: property.OwnerId,
                scientificName: null,
                minTemperature: null,
                maxSoilMoisture: null);

            if (!aggregateResult.IsSuccess)
            {
                AddErrors(aggregateResult.ValidationErrors);
                return BuildValidationErrorResult();
            }

            var aggregate = aggregateResult.Value;
            _repository.Add(aggregate);
            await _outbox.SaveChangesAsync(ct).ConfigureAwait(false);

            _logger.LogInformation(
                "Crop type catalog entry {CropTypeCatalogId} created for owner {OwnerId} and property {PropertyId}",
                aggregate.Id,
                property.OwnerId,
                property.Id);

            return new CreateCropTypeResponse(
                aggregate.Id,
                property.Id,
                property.OwnerId,
                aggregate.CropTypeName.Value,
                aggregate.SuggestedImage,
                "Catalog",
                false,
                false,
                null,
                CropTypeCatalogCommandMapping.BuildPlantingWindow(
                    aggregate.TypicalPlantingStartMonth,
                    aggregate.TypicalPlantingEndMonth),
                aggregate.TypicalHarvestCycleMonths,
                aggregate.RecommendedIrrigationType,
                aggregate.MinSoilMoisture,
                aggregate.MaxTemperature,
                aggregate.MinHumidity,
                aggregate.Description,
                null,
                null,
                aggregate.CreatedAt,
                aggregate.Id);
        }
    }
}
