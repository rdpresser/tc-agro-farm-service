namespace TC.Agro.Farm.Application.UseCases.CropTypes.Update
{
    internal sealed class UpdateCropTypeCommandHandler
        : BaseHandler<UpdateCropTypeCommand, UpdateCropTypeResponse>
    {
        private readonly ICropTypeCatalogRepository _repository;
        private readonly IUserContext _userContext;
        private readonly ITransactionalOutbox _outbox;
        private readonly ILogger<UpdateCropTypeCommandHandler> _logger;

        public UpdateCropTypeCommandHandler(
            ICropTypeCatalogRepository repository,
            IUserContext userContext,
            ITransactionalOutbox outbox,
            ILogger<UpdateCropTypeCommandHandler> logger)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
            _outbox = outbox ?? throw new ArgumentNullException(nameof(outbox));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public override async Task<Result<UpdateCropTypeResponse>> ExecuteAsync(
            UpdateCropTypeCommand command,
            CancellationToken ct = default)
        {
            var aggregate = _userContext.IsAdmin
                ? await _repository.GetByIdAsync(command.CropTypeId, ct).ConfigureAwait(false)
                : await _repository.GetByIdScopedAsync(command.CropTypeId, _userContext.Id, cancellationToken: ct).ConfigureAwait(false);

            if (aggregate is null)
            {
                AddError(x => x.CropTypeId, "Crop type catalog entry not found.", FarmDomainErrors.CropTypeCatalogNotFound.ErrorCode);
                return BuildNotFoundResult();
            }

            if (aggregate.OwnerId != _userContext.Id && !_userContext.IsAdmin)
            {
                AddError(x => x.CropTypeId, "You are not authorized to update this crop type catalog entry.", "CropTypeCatalog.NotAuthorized");
                return BuildNotAuthorizedResult();
            }

            if (!string.Equals(command.CropType.Trim(), aggregate.CropTypeName.Value, StringComparison.OrdinalIgnoreCase))
            {
                AddError(
                    x => x.CropType,
                    "CropType cannot be renamed via update. Create a new crop type entry for a different name.",
                    "CropTypeCatalog.RenameNotSupported");
                return BuildValidationErrorResult();
            }

            var (startMonth, endMonth) = CropTypeCatalogCommandMapping.ParsePlantingWindow(command.PlantingWindow);

            var updateResult = aggregate.UpdateMetadata(
                description: command.Notes,
                recommendedIrrigationType: command.SuggestedIrrigationType,
                typicalHarvestCycleMonths: command.HarvestCycleMonths,
                scientificName: null,
                typicalPlantingStartMonth: startMonth,
                typicalPlantingEndMonth: endMonth,
                minTemperature: null,
                maxTemperature: command.MaxTemperature,
                minHumidity: command.MinHumidity,
                minSoilMoisture: command.MinSoilMoisture,
                maxSoilMoisture: null,
                suggestedImage: command.SuggestedImage);

            if (!updateResult.IsSuccess)
            {
                AddErrors(updateResult.ValidationErrors);
                return BuildValidationErrorResult();
            }

            await _outbox.SaveChangesAsync(ct).ConfigureAwait(false);

            _logger.LogInformation(
                "Crop type catalog entry {CropTypeCatalogId} updated by user {UserId}",
                aggregate.Id,
                _userContext.Id);

            return new UpdateCropTypeResponse(
                aggregate.Id,
                Guid.Empty,
                aggregate.OwnerId ?? Guid.Empty,
                aggregate.CropTypeName.Value,
                aggregate.SuggestedImage,
                "Catalog",
                false,
                false,
                CropTypeCatalogCommandMapping.BuildPlantingWindow(
                    aggregate.TypicalPlantingStartMonth,
                    aggregate.TypicalPlantingEndMonth),
                aggregate.TypicalHarvestCycleMonths,
                aggregate.RecommendedIrrigationType,
                aggregate.MinSoilMoisture,
                aggregate.MaxTemperature,
                aggregate.MinHumidity,
                aggregate.Description,
                aggregate.UpdatedAt,
                aggregate.Id);
        }
    }
}
