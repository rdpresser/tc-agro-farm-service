namespace TC.Agro.Farm.Application.UseCases.Plots.Submit
{
    internal sealed class SubmitPlotCommandHandler
        : BaseHandler<SubmitPlotCommand, SubmitPlotResponse>
    {
        private readonly PlotSubmissionCoordinator _submissionCoordinator;

        public SubmitPlotCommandHandler(
            IPlotAggregateRepository plotRepository,
            IPropertyAggregateRepository propertyRepository,
            ICropCycleAggregateRepository cropCycleRepository,
            ICropTypeCatalogRepository cropTypeCatalogRepository,
            ICropTypeSuggestionRepository cropTypeSuggestionRepository,
            IUserContext userContext,
            ITransactionalOutbox outbox,
            ILogger<SubmitPlotCommandHandler> logger)
        {
            _submissionCoordinator = new PlotSubmissionCoordinator(
                plotRepository,
                propertyRepository,
                cropCycleRepository,
                cropTypeCatalogRepository,
                cropTypeSuggestionRepository,
                userContext,
                outbox,
                logger);
        }

        public override async Task<Result<SubmitPlotResponse>> ExecuteAsync(
            SubmitPlotCommand command,
            CancellationToken ct = default)
        {
            return command.IsUpdate
                ? await UpdateAsync(command, ct).ConfigureAwait(false)
                : await CreateAsync(command, ct).ConfigureAwait(false);
        }

        private async Task<Result<SubmitPlotResponse>> CreateAsync(SubmitPlotCommand command, CancellationToken ct)
        {
            var request = new PlotSubmissionRequest(
                PlotId: command.PlotId,
                PropertyId: command.PropertyId,
                Name: command.Name,
                CropType: command.CropType,
                AreaHectares: command.AreaHectares,
                Latitude: command.Latitude,
                Longitude: command.Longitude,
                BoundaryGeoJson: command.BoundaryGeoJson,
                PlantingDate: command.PlantingDate,
                ExpectedHarvestDate: command.ExpectedHarvestDate,
                IrrigationType: command.IrrigationType,
                AdditionalNotes: command.AdditionalNotes,
                OwnerId: command.OwnerId,
                CropTypeCatalogId: command.CropTypeCatalogId,
                SelectedCropTypeSuggestionId: command.SelectedCropTypeSuggestionId);

            var result = await _submissionCoordinator.CreateAsync(request, ct).ConfigureAwait(false);
            if (result.IsSuccess)
            {
                return Result.Success(BuildResponse(result.Value));
            }

            if (result.Status == ResultStatus.NotFound)
            {
                AddError(nameof(SubmitPlotCommand), result.Errors.FirstOrDefault() ?? "Resource not found.");
                return BuildNotFoundResult();
            }

            if (result.Status == ResultStatus.Unauthorized || result.Status == ResultStatus.Forbidden)
            {
                AddError(nameof(SubmitPlotCommand), result.Errors.FirstOrDefault() ?? "Unauthorized.");
                return BuildNotAuthorizedResult();
            }

            AddErrors(result.ValidationErrors);
            return BuildValidationErrorResult();
        }

        private async Task<Result<SubmitPlotResponse>> UpdateAsync(SubmitPlotCommand command, CancellationToken ct)
        {
            var request = new PlotSubmissionRequest(
                PlotId: command.PlotId,
                PropertyId: command.PropertyId,
                Name: command.Name,
                CropType: command.CropType,
                AreaHectares: command.AreaHectares,
                Latitude: command.Latitude,
                Longitude: command.Longitude,
                BoundaryGeoJson: command.BoundaryGeoJson,
                PlantingDate: command.PlantingDate,
                ExpectedHarvestDate: command.ExpectedHarvestDate,
                IrrigationType: command.IrrigationType,
                AdditionalNotes: command.AdditionalNotes,
                OwnerId: command.OwnerId,
                CropTypeCatalogId: command.CropTypeCatalogId,
                SelectedCropTypeSuggestionId: command.SelectedCropTypeSuggestionId);

            var result = await _submissionCoordinator.UpdateAsync(request, ct).ConfigureAwait(false);
            if (result.IsSuccess)
            {
                return Result.Success(BuildResponse(result.Value));
            }

            if (result.Status == ResultStatus.NotFound)
            {
                AddError(nameof(SubmitPlotCommand), result.Errors.FirstOrDefault() ?? "Resource not found.");
                return BuildNotFoundResult();
            }

            if (result.Status == ResultStatus.Unauthorized || result.Status == ResultStatus.Forbidden)
            {
                AddError(nameof(SubmitPlotCommand), result.Errors.FirstOrDefault() ?? "Unauthorized.");
                return BuildNotAuthorizedResult();
            }

            AddErrors(result.ValidationErrors);
            return BuildValidationErrorResult();
        }

        private static SubmitPlotResponse BuildResponse(PlotSubmissionResult result)
            => new(
                Id: result.Plot.Id,
                PropertyId: result.Plot.PropertyId,
                OwnerId: result.Plot.OwnerId,
                Name: result.Plot.Name.Value,
                CropType: result.ResolvedCropType,
                AreaHectares: result.Plot.AreaHectares.Hectares,
                Latitude: result.Plot.Latitude,
                Longitude: result.Plot.Longitude,
                BoundaryGeoJson: result.Plot.BoundaryGeoJson,
                PlantingDate: result.CurrentCycle.StartedAt,
                ExpectedHarvestDate: result.CurrentCycle.ExpectedHarvestDate ?? result.Plot.ExpectedHarvestDate,
                IrrigationType: result.CurrentCycle.IrrigationType.Value,
                AdditionalNotes: result.CurrentCycle.Notes,
                IsActive: result.Plot.IsActive,
                CreatedAt: result.Plot.CreatedAt,
                UpdatedAt: result.Plot.UpdatedAt,
                CropTypeCatalogId: result.CurrentCycle.CropTypeCatalogId,
                SelectedCropTypeSuggestionId: result.CurrentCycle.SelectedCropTypeSuggestionId,
                IsCreated: result.IsCreated);

    }
}