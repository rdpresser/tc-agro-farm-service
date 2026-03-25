using TC.Agro.Farm.Application.UseCases.Plots.Submit;

namespace TC.Agro.Farm.Application.UseCases.Plots.Update
{
    internal sealed class UpdatePlotCommandHandler
        : BaseHandler<UpdatePlotCommand, UpdatePlotResponse>
    {
        private readonly PlotSubmissionCoordinator _submissionCoordinator;

        public UpdatePlotCommandHandler(
            IPlotAggregateRepository repository,
            IPropertyAggregateRepository propertyRepository,
            ICropCycleAggregateRepository cropCycleRepository,
            ICropTypeCatalogRepository cropTypeCatalogRepository,
            ICropTypeSuggestionRepository cropTypeSuggestionRepository,
            IUserContext userContext,
            ITransactionalOutbox outbox,
            ILogger<UpdatePlotCommandHandler> logger)
        {
            _submissionCoordinator = new PlotSubmissionCoordinator(
                repository,
                propertyRepository,
                cropCycleRepository,
                cropTypeCatalogRepository,
                cropTypeSuggestionRepository,
                userContext,
                outbox,
                logger);
        }

        public override async Task<Result<UpdatePlotResponse>> ExecuteAsync(
            UpdatePlotCommand command,
            CancellationToken ct = default)
        {
            var request = new PlotSubmissionRequest(
                PlotId: command.PlotId,
                PropertyId: Guid.Empty,
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
                OwnerId: null,
                CropTypeCatalogId: command.CropTypeCatalogId,
                SelectedCropTypeSuggestionId: command.SelectedCropTypeSuggestionId);

            var result = await _submissionCoordinator.UpdateAsync(request, ct).ConfigureAwait(false);
            if (result.IsSuccess)
            {
                return Result.Success(BuildResponse(result.Value));
            }

            if (result.Status == ResultStatus.NotFound)
            {
                AddError(nameof(UpdatePlotCommand.PlotId), result.Errors.FirstOrDefault() ?? "Plot not found.");
                return BuildNotFoundResult();
            }

            if (result.Status == ResultStatus.Unauthorized || result.Status == ResultStatus.Forbidden)
            {
                AddError(nameof(UpdatePlotCommand), result.Errors.FirstOrDefault() ?? "Unauthorized.");
                return BuildNotAuthorizedResult();
            }

            AddErrors(result.ValidationErrors);
            return BuildValidationErrorResult();
        }

        private static UpdatePlotResponse BuildResponse(PlotSubmissionResult result)
            => new(
                PlotId: result.Plot.Id,
                PropertyId: result.Plot.PropertyId,
                Name: result.Plot.Name.Value,
                CropType: result.ResolvedCropType,
                AreaHectares: result.Plot.AreaHectares.Hectares,
                Latitude: result.Plot.Latitude,
                Longitude: result.Plot.Longitude,
                PlantingDate: result.CurrentCycle.StartedAt,
                ExpectedHarvestDate: result.CurrentCycle.ExpectedHarvestDate ?? result.Plot.ExpectedHarvestDate,
                IrrigationType: result.CurrentCycle.IrrigationType.Value,
                AdditionalNotes: result.CurrentCycle.Notes,
                UpdatedAt: result.Plot.UpdatedAt,
                CropTypeCatalogId: result.CurrentCycle.CropTypeCatalogId,
                SelectedCropTypeSuggestionId: result.CurrentCycle.SelectedCropTypeSuggestionId);
    }
}
