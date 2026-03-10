namespace TC.Agro.Farm.Application.UseCases.CropTypes.Update
{
    internal sealed class UpdateCropTypeCommandHandler
        : BaseHandler<UpdateCropTypeCommand, UpdateCropTypeResponse>
    {
        private readonly ICropTypeSuggestionRepository _repository;
        private readonly IUserContext _userContext;
        private readonly ITransactionalOutbox _outbox;
        private readonly ILogger<UpdateCropTypeCommandHandler> _logger;

        public UpdateCropTypeCommandHandler(
            ICropTypeSuggestionRepository repository,
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
            var aggregate = await _repository
                .GetByIdAsync(command.CropTypeId, ct)
                .ConfigureAwait(false);

            if (aggregate is null)
            {
                AddError(x => x.CropTypeId, "Crop type suggestion not found.", FarmDomainErrors.CropTypeSuggestionNotFound.ErrorCode);
                return BuildNotFoundResult();
            }

            if (aggregate.OwnerId != _userContext.Id && !_userContext.IsAdmin)
            {
                AddError(x => x.CropTypeId, "You are not authorized to update this crop type suggestion.", "CropTypeSuggestion.NotAuthorized");
                return BuildNotAuthorizedResult();
            }

            var updateResult = aggregate.UpdateManual(
                cropType: command.CropType,
                plantingWindow: command.PlantingWindow,
                harvestCycleMonths: command.HarvestCycleMonths,
                suggestedIrrigationType: command.SuggestedIrrigationType,
                minSoilMoisture: command.MinSoilMoisture,
                maxTemperature: command.MaxTemperature,
                minHumidity: command.MinHumidity,
                notes: command.Notes);

            if (!updateResult.IsSuccess)
            {
                AddErrors(updateResult.ValidationErrors);
                return BuildValidationErrorResult();
            }

            await _outbox.SaveChangesAsync(ct).ConfigureAwait(false);

            _logger.LogInformation(
                "Crop type suggestion {CropTypeSuggestionId} updated by user {UserId}",
                aggregate.Id,
                _userContext.Id);

            return new UpdateCropTypeResponse(
                aggregate.Id,
                aggregate.PropertyId,
                aggregate.OwnerId,
                aggregate.CropName.Value,
                aggregate.Source,
                aggregate.IsOverride,
                aggregate.IsStale,
                aggregate.PlantingWindow,
                aggregate.HarvestCycleMonths,
                aggregate.SuggestedIrrigationType,
                aggregate.MinSoilMoisture,
                aggregate.MaxTemperature,
                aggregate.MinHumidity,
                aggregate.Notes,
                aggregate.UpdatedAt);
        }
    }
}
