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
            var ownerId = _userContext.Id;
            if (ownerId == Guid.Empty)
            {
                AddError(
                    x => x.CropType,
                    "You are not authorized to create owner-scoped crop catalog entries.",
                    "CropTypeCatalog.NotAuthorized");
                return BuildNotAuthorizedResult();
            }

            var existingEntry = await _repository
                .GetByNameAsync(command.CropType, ownerId, ct)
                .ConfigureAwait(false);

            if (existingEntry is not null && existingEntry.OwnerId == ownerId)
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
                ownerId: ownerId,
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

            var ownerProperty = await _propertyRepository
                .GetAnyByOwnerAsync(ownerId, ct)
                .ConfigureAwait(false);

            var propertyIdContext = ownerProperty?.Id ?? Guid.Empty;

            _logger.LogInformation(
                "Crop type catalog entry {CropTypeCatalogId} created for owner {OwnerId} with property context {PropertyId}",
                aggregate.Id,
                ownerId,
                propertyIdContext);

            return new CreateCropTypeResponse(
                aggregate.Id,
                propertyIdContext,
                ownerId,
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
