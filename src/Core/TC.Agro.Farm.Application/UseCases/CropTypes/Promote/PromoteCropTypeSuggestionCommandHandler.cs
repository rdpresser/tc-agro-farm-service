using TC.Agro.Farm.Application.UseCases.CropTypes.Create;
using TC.Agro.Farm.Domain.Aggregates;

namespace TC.Agro.Farm.Application.UseCases.CropTypes.Promote
{
    internal sealed class PromoteCropTypeSuggestionCommandHandler
        : BaseHandler<PromoteCropTypeSuggestionCommand, PromoteCropTypeSuggestionResponse>
    {
        private readonly ICropTypeSuggestionRepository _suggestionRepository;
        private readonly ICropTypeCatalogRepository _catalogRepository;
        private readonly IUserContext _userContext;
        private readonly ITransactionalOutbox _outbox;
        private readonly ILogger<PromoteCropTypeSuggestionCommandHandler> _logger;

        public PromoteCropTypeSuggestionCommandHandler(
            ICropTypeSuggestionRepository suggestionRepository,
            ICropTypeCatalogRepository catalogRepository,
            IUserContext userContext,
            ITransactionalOutbox outbox,
            ILogger<PromoteCropTypeSuggestionCommandHandler> logger)
        {
            _suggestionRepository = suggestionRepository ?? throw new ArgumentNullException(nameof(suggestionRepository));
            _catalogRepository = catalogRepository ?? throw new ArgumentNullException(nameof(catalogRepository));
            _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
            _outbox = outbox ?? throw new ArgumentNullException(nameof(outbox));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public override async Task<Result<PromoteCropTypeSuggestionResponse>> ExecuteAsync(
            PromoteCropTypeSuggestionCommand command,
            CancellationToken ct = default)
        {
            var suggestion = await _suggestionRepository
                .GetByIdAsync(command.SuggestionId, ct)
                .ConfigureAwait(false);

            if (suggestion is null)
            {
                AddError(x => x.SuggestionId, "Crop type suggestion not found.", "CropTypeSuggestion.NotFound");
                return BuildNotFoundResult();
            }

            if (suggestion.OwnerId != _userContext.Id && !_userContext.IsAdmin)
            {
                AddError(x => x.SuggestionId, "You are not authorized to promote this crop type suggestion.", "CropTypeSuggestion.NotAuthorized");
                return BuildNotAuthorizedResult();
            }

            var existingCatalog = await _catalogRepository
                .GetByNameAsync(suggestion.CropName.Value, suggestion.OwnerId, ct)
                .ConfigureAwait(false);

            if (existingCatalog is not null && existingCatalog.OwnerId == suggestion.OwnerId)
            {
                if (!suggestion.IsStale)
                {
                    var staleResult = suggestion.MarkAsStale();
                    if (!staleResult.IsSuccess)
                    {
                        AddErrors(staleResult.ValidationErrors);
                        return BuildValidationErrorResult();
                    }

                    await _outbox.SaveChangesAsync(ct).ConfigureAwait(false);
                }

                return Result.Success(BuildResponse(existingCatalog, suggestion, alreadyPromoted: true));
            }

            var (startMonth, endMonth) = CropTypeCatalogCommandMapping.ParsePlantingWindow(suggestion.PlantingWindow);

            var aggregateResult = CropTypeCatalogAggregate.Create(
                cropTypeName: suggestion.CropName.Value,
                isSystemDefined: false,
                description: suggestion.Notes,
                recommendedIrrigationType: suggestion.SuggestedIrrigationType,
                typicalHarvestCycleMonths: suggestion.HarvestCycleMonths,
                scientificName: null,
                typicalPlantingStartMonth: startMonth,
                typicalPlantingEndMonth: endMonth,
                minTemperature: null,
                maxTemperature: suggestion.MaxTemperature,
                minHumidity: suggestion.MinHumidity,
                minSoilMoisture: suggestion.MinSoilMoisture,
                maxSoilMoisture: null,
                suggestedImage: suggestion.SuggestedImage,
                ownerId: suggestion.OwnerId);

            if (!aggregateResult.IsSuccess)
            {
                AddErrors(aggregateResult.ValidationErrors);
                return BuildValidationErrorResult();
            }

            var staleMarkResult = suggestion.MarkAsStale();
            if (!staleMarkResult.IsSuccess)
            {
                AddErrors(staleMarkResult.ValidationErrors);
                return BuildValidationErrorResult();
            }

            var catalog = aggregateResult.Value;
            _catalogRepository.Add(catalog);
            await _outbox.SaveChangesAsync(ct).ConfigureAwait(false);

            _logger.LogInformation(
                "Crop type suggestion {SuggestionId} promoted to catalog {CropTypeCatalogId} by user {UserId}",
                suggestion.Id,
                catalog.Id,
                _userContext.Id);

            return Result.Success(BuildResponse(catalog, suggestion, alreadyPromoted: false));
        }

        private static PromoteCropTypeSuggestionResponse BuildResponse(
            CropTypeCatalogAggregate catalog,
            CropTypeSuggestionAggregate suggestion,
            bool alreadyPromoted)
            => new(
                Id: catalog.Id,
                PropertyId: suggestion.PropertyId,
                OwnerId: catalog.OwnerId ?? suggestion.OwnerId,
                CropType: catalog.CropTypeName.Value,
                SuggestedImage: catalog.SuggestedImage,
                Source: "Catalog",
                IsOverride: false,
                IsStale: suggestion.IsStale,
                PlantingWindow: CropTypeCatalogCommandMapping.BuildPlantingWindow(
                    catalog.TypicalPlantingStartMonth,
                    catalog.TypicalPlantingEndMonth),
                HarvestCycleMonths: catalog.TypicalHarvestCycleMonths,
                SuggestedIrrigationType: catalog.RecommendedIrrigationType,
                MinSoilMoisture: catalog.MinSoilMoisture,
                MaxTemperature: catalog.MaxTemperature,
                MinHumidity: catalog.MinHumidity,
                Notes: catalog.Description,
                CreatedAt: catalog.CreatedAt,
                CropTypeCatalogId: catalog.Id,
                PromotedSuggestionId: suggestion.Id,
                AlreadyPromoted: alreadyPromoted);
    }
}