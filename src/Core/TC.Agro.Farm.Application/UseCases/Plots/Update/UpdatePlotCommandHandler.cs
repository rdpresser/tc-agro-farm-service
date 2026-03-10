namespace TC.Agro.Farm.Application.UseCases.Plots.Update
{
    internal sealed class UpdatePlotCommandHandler
        : BaseHandler<UpdatePlotCommand, UpdatePlotResponse>
    {
        private readonly IPlotAggregateRepository _repository;
        private readonly ICropTypeCatalogRepository _cropTypeCatalogRepository;
        private readonly ICropTypeSuggestionRepository _cropTypeSuggestionRepository;
        private readonly IUserContext _userContext;
        private readonly ITransactionalOutbox _outbox;
        private readonly ILogger<UpdatePlotCommandHandler> _logger;

        public UpdatePlotCommandHandler(
            IPlotAggregateRepository repository,
            ICropTypeCatalogRepository cropTypeCatalogRepository,
            ICropTypeSuggestionRepository cropTypeSuggestionRepository,
            IUserContext userContext,
            ITransactionalOutbox outbox,
            ILogger<UpdatePlotCommandHandler> logger)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _cropTypeCatalogRepository = cropTypeCatalogRepository ?? throw new ArgumentNullException(nameof(cropTypeCatalogRepository));
            _cropTypeSuggestionRepository = cropTypeSuggestionRepository ?? throw new ArgumentNullException(nameof(cropTypeSuggestionRepository));
            _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
            _outbox = outbox ?? throw new ArgumentNullException(nameof(outbox));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public override async Task<Result<UpdatePlotResponse>> ExecuteAsync(
            UpdatePlotCommand command,
            CancellationToken ct = default)
        {
            _logger.LogInformation(
                "Updating plot {PlotId} by user {UserId}",
                command.PlotId,
                _userContext.Id);

            var aggregate = await _repository.GetByIdAsync(command.PlotId, ct).ConfigureAwait(false);
            if (aggregate is null)
            {
                _logger.LogWarning("Plot {PlotId} not found", command.PlotId);
                AddError(x => x.PlotId, "Plot not found.", FarmDomainErrors.PlotNotFound.ErrorCode);
                return BuildNotFoundResult();
            }

            if (aggregate.OwnerId != _userContext.Id && !_userContext.IsAdmin)
            {
                _logger.LogWarning(
                    "User {UserId} attempted to update plot {PlotId} owned by {OwnerId}",
                    _userContext.Id,
                    command.PlotId,
                    aggregate.OwnerId);
                AddError(x => x.PlotId, "You are not authorized to update this plot.", "Plot.NotAuthorized");
                return BuildNotAuthorizedResult();
            }

            var nameExists = await _repository
                .NameExistsForPropertyExcludingAsync(command.Name, aggregate.PropertyId, aggregate.Id, ct)
                .ConfigureAwait(false);

            if (nameExists)
            {
                AddError(x => x.Name,
                    $"A plot with name '{command.Name}' already exists for this property.",
                    "Name.Duplicate");
                return BuildValidationErrorResult();
            }

            var cropReferenceResult = await ResolveCropReferencesAsync(
                command,
                aggregate.OwnerId,
                aggregate.PropertyId,
                ct).ConfigureAwait(false);

            if (!cropReferenceResult.IsSuccess)
            {
                AddErrors(cropReferenceResult.ValidationErrors);
                return BuildValidationErrorResult();
            }

            var updateResult = aggregate.Update(
                command.Name,
                cropReferenceResult.Value.ResolvedCropType,
                command.AreaHectares,
                command.PlantingDate,
                command.ExpectedHarvestDate,
                command.IrrigationType,
                command.AdditionalNotes,
                command.Latitude,
                command.Longitude,
                command.BoundaryGeoJson,
                cropReferenceResult.Value.CropTypeCatalogId,
                cropReferenceResult.Value.SelectedCropTypeSuggestionId);

            if (!updateResult.IsSuccess)
            {
                AddErrors(updateResult.ValidationErrors);
                return BuildValidationErrorResult();
            }

            await _outbox.SaveChangesAsync(ct).ConfigureAwait(false);

            _logger.LogInformation("Plot {PlotId} updated successfully", aggregate.Id);

            return UpdatePlotMapper.FromAggregate(aggregate, cropReferenceResult.Value.ResolvedCropType);
        }

        private async Task<Result<CropReferenceResolution>> ResolveCropReferencesAsync(
            UpdatePlotCommand command,
            Guid ownerId,
            Guid propertyId,
            CancellationToken ct)
        {
            var normalizedCropType = string.IsNullOrWhiteSpace(command.CropType)
                ? null
                : command.CropType.Trim();

            CropTypeCatalogAggregate? catalogAggregate = null;

            if (command.CropTypeCatalogId.HasValue)
            {
                if (command.CropTypeCatalogId.Value == Guid.Empty)
                {
                    return Result<CropReferenceResolution>.Invalid(
                        new ValidationError(nameof(command.CropTypeCatalogId), "CropTypeCatalogId cannot be empty when informed."));
                }

                catalogAggregate = await _cropTypeCatalogRepository
                    .GetByIdAsync(command.CropTypeCatalogId.Value, ct)
                    .ConfigureAwait(false);
            }
            else
            {
                if (string.IsNullOrWhiteSpace(normalizedCropType))
                {
                    return Result<CropReferenceResolution>.Invalid(
                        new ValidationError(nameof(command.CropType), "CropTypeCatalogId is required, or an existing CropType name must be informed."));
                }

                catalogAggregate = await _cropTypeCatalogRepository
                    .GetByNameAsync(normalizedCropType, ct)
                    .ConfigureAwait(false);
            }

            if (catalogAggregate is null)
            {
                return Result<CropReferenceResolution>.Invalid(FarmDomainErrors.CropTypeCatalogNotFound);
            }

            if (!string.IsNullOrWhiteSpace(normalizedCropType) &&
                !string.Equals(normalizedCropType, catalogAggregate.CropTypeName.Value, StringComparison.OrdinalIgnoreCase))
            {
                return Result<CropReferenceResolution>.Invalid(
                    new ValidationError(
                        nameof(command.CropType),
                        "CropType must match the informed CropTypeCatalogId when both are provided."));
            }

            var resolvedCropType = catalogAggregate.CropTypeName.Value;

            if (command.SelectedCropTypeSuggestionId.HasValue)
            {
                if (command.SelectedCropTypeSuggestionId.Value == Guid.Empty)
                {
                    return Result<CropReferenceResolution>.Invalid(
                        new ValidationError(
                            nameof(command.SelectedCropTypeSuggestionId),
                            "SelectedCropTypeSuggestionId cannot be empty when informed."));
                }

                var selectedSuggestion = await _cropTypeSuggestionRepository
                    .GetByIdAsync(command.SelectedCropTypeSuggestionId.Value, ct)
                    .ConfigureAwait(false);

                if (selectedSuggestion is null)
                {
                    return Result<CropReferenceResolution>.Invalid(FarmDomainErrors.CropTypeSuggestionNotFound);
                }

                if (selectedSuggestion.PropertyId != propertyId)
                {
                    return Result<CropReferenceResolution>.Invalid(
                        new ValidationError(
                            nameof(command.SelectedCropTypeSuggestionId),
                            "Selected crop type suggestion does not belong to the informed property."));
                }

                if (selectedSuggestion.OwnerId != ownerId)
                {
                    return Result<CropReferenceResolution>.Invalid(
                        new ValidationError(
                            nameof(command.SelectedCropTypeSuggestionId),
                            "Selected crop type suggestion does not belong to the informed owner."));
                }

                if (!string.Equals(selectedSuggestion.CropName.Value, resolvedCropType, StringComparison.OrdinalIgnoreCase))
                {
                    return Result<CropReferenceResolution>.Invalid(
                        new ValidationError(
                            nameof(command.SelectedCropTypeSuggestionId),
                            "Selected crop type suggestion does not match the resolved crop type catalog."));
                }
            }

            return Result<CropReferenceResolution>.Success(
                new CropReferenceResolution(
                    resolvedCropType,
                    catalogAggregate.Id,
                    command.SelectedCropTypeSuggestionId));
        }

        private sealed record CropReferenceResolution(
            string ResolvedCropType,
            Guid CropTypeCatalogId,
            Guid? SelectedCropTypeSuggestionId);
    }
}
