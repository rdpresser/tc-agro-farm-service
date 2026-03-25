using TC.Agro.Farm.Application.UseCases.Plots.Submit;

namespace TC.Agro.Farm.Application.UseCases.Plots.Create
{
    internal sealed class CreatePlotCommandHandler
        : BaseHandler<CreatePlotCommand, CreatePlotResponse>
    {
        private readonly PlotSubmissionCoordinator _submissionCoordinator;

        public CreatePlotCommandHandler(
            IPlotAggregateRepository repository,
            IPropertyAggregateRepository propertyRepository,
            ICropCycleAggregateRepository cropCycleRepository,
            ICropTypeCatalogRepository cropTypeCatalogRepository,
            ICropTypeSuggestionRepository cropTypeSuggestionRepository,
            IUserContext userContext,
            ITransactionalOutbox outbox,
            ILogger<CreatePlotCommandHandler> logger)
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

        public override async Task<Result<CreatePlotResponse>> ExecuteAsync(
            CreatePlotCommand command,
            CancellationToken ct = default)
        {
            var request = new PlotSubmissionRequest(
                PlotId: Guid.Empty,
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

            if (result.Status == ResultStatus.Unauthorized || result.Status == ResultStatus.Forbidden)
            {
                AddError(nameof(CreatePlotCommand), result.Errors.FirstOrDefault() ?? "Unauthorized.");
                return BuildNotAuthorizedResult();
            }

            AddErrors(result.ValidationErrors);
            return result.Status == ResultStatus.NotFound
                ? BuildNotFoundResult()
                : BuildValidationErrorResult();
        }

        private static CreatePlotResponse BuildResponse(PlotSubmissionResult result)
            => new(
                Id: result.Plot.Id,
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
                IsActive: result.Plot.IsActive,
                CreatedAt: result.Plot.CreatedAt,
                CropTypeCatalogId: result.CurrentCycle.CropTypeCatalogId,
                SelectedCropTypeSuggestionId: result.CurrentCycle.SelectedCropTypeSuggestionId);
    }
}
