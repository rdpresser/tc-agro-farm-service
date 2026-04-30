using TC.Agro.Farm.Application.UseCases.CropTypes.Regenerate;
using Wolverine;

namespace TC.Agro.Farm.Application.MessageBrokerHandlers
{
    /// <summary>
    /// Handles asynchronous generation of location-aware crop type suggestions.
    /// This handler never throws for AI/provider failures to avoid retry storms.
    /// </summary>
    public sealed class CropTypeSuggestionsGenerationHandler : IWolverineHandler
    {
        private const int MaxSuggestions = 15;

        private readonly IPropertyAggregateRepository _propertyRepository;
        private readonly ICropTypeSuggestionRepository _cropTypeRepository;
        private readonly ICropTypeSuggestionAiProvider _aiProvider;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<CropTypeSuggestionsGenerationHandler> _logger;

        public CropTypeSuggestionsGenerationHandler(
            IPropertyAggregateRepository propertyRepository,
            ICropTypeSuggestionRepository cropTypeRepository,
            ICropTypeSuggestionAiProvider aiProvider,
            IUnitOfWork unitOfWork,
            ILogger<CropTypeSuggestionsGenerationHandler> logger)
        {
            _propertyRepository = propertyRepository ?? throw new ArgumentNullException(nameof(propertyRepository));
            _cropTypeRepository = cropTypeRepository ?? throw new ArgumentNullException(nameof(cropTypeRepository));
            _aiProvider = aiProvider ?? throw new ArgumentNullException(nameof(aiProvider));
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task HandleAsync(
            GeneratePropertyCropTypeSuggestionsMessage message,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(message);

            var property = await _propertyRepository
                .GetByIdAsync(message.PropertyId, cancellationToken)
                .ConfigureAwait(false);

            if (property is null)
            {
                _logger.LogWarning(
                    "Crop suggestion generation skipped. Property {PropertyId} was not found.",
                    message.PropertyId);
                return;
            }

            if (!property.Location.Latitude.HasValue || !property.Location.Longitude.HasValue)
            {
                _logger.LogInformation(
                    "Crop suggestion generation skipped for property {PropertyId}. Missing coordinates.",
                    message.PropertyId);
                return;
            }

            IReadOnlyList<CropTypeSuggestionDraft> drafts;
            try
            {
                drafts = await _aiProvider
                    .GenerateSuggestionsAsync(
                        new CropTypeSuggestionAiRequest(
                            PropertyId: property.Id,
                            OwnerId: property.OwnerId,
                            City: property.Location.City,
                            State: property.Location.State,
                            Country: property.Location.Country,
                            Latitude: property.Location.Latitude.Value,
                            Longitude: property.Location.Longitude.Value,
                            SuggestionCount: MaxSuggestions),
                        cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (TaskCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Crop suggestion generation failed for property {PropertyId}.",
                    property.Id);
                return;
            }

            var normalizedDrafts = NormalizeDrafts(drafts, MaxSuggestions);
            if (normalizedDrafts.Count == 0)
            {
                _logger.LogInformation(
                    "Crop suggestion generation finished for property {PropertyId} with no valid suggestions.",
                    property.Id);
                return;
            }

            // Replace previous AI suggestions with the latest generated set.
            await _cropTypeRepository
                .DeactivateAiSuggestionsByPropertyAsync(property.Id, cancellationToken)
                .ConfigureAwait(false);

            var generatedAt = DateTimeOffset.UtcNow;
            var aggregates = new List<CropTypeSuggestionAggregate>(normalizedDrafts.Count);

            foreach (var draft in normalizedDrafts)
            {
                var createResult = CropTypeSuggestionAggregate.CreateAi(
                    propertyId: property.Id,
                    ownerId: property.OwnerId,
                    cropType: draft.CropType,
                    confidenceScore: draft.ConfidenceScore,
                    plantingWindow: draft.PlantingWindow,
                    harvestCycleMonths: draft.HarvestCycleMonths,
                    suggestedIrrigationType: draft.SuggestedIrrigationType,
                    minSoilMoisture: draft.MinSoilMoisture,
                    maxTemperature: draft.MaxTemperature,
                    minHumidity: draft.MinHumidity,
                    notes: draft.Notes,
                    model: "openai",
                    generatedAt: generatedAt,
                    suggestedImage: draft.SuggestedImage);

                if (!createResult.IsSuccess)
                {
                    _logger.LogWarning(
                        "Skipping invalid AI suggestion for property {PropertyId}. Errors: {Errors}",
                        property.Id,
                        string.Join(" | ", createResult.ValidationErrors.Select(e => e.ErrorMessage)));
                    continue;
                }

                aggregates.Add(createResult.Value);
            }

            if (aggregates.Count == 0)
            {
                _logger.LogInformation(
                    "Crop suggestion generation for property {PropertyId} produced no persistable suggestions.",
                    property.Id);
                return;
            }

            _cropTypeRepository.AddRange(aggregates);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            _logger.LogInformation(
                "Persisted {Count} AI crop type suggestions for property {PropertyId}",
                aggregates.Count,
                property.Id);
        }

        private static IReadOnlyList<CropTypeSuggestionDraft> NormalizeDrafts(
            IReadOnlyList<CropTypeSuggestionDraft> drafts,
            int maxSuggestions)
        {
            if (drafts is null || drafts.Count == 0)
            {
                return [];
            }

            var result = new List<CropTypeSuggestionDraft>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var draft in drafts)
            {
                var cropType = string.IsNullOrWhiteSpace(draft.CropType)
                    ? string.Empty
                    : draft.CropType.Trim();

                if (cropType.Length == 0 || !seen.Add(cropType))
                {
                    continue;
                }

                result.Add(new CropTypeSuggestionDraft(
                    CropType: cropType,
                    ConfidenceScore: ClampNullable(draft.ConfidenceScore, 0, 100),
                    PlantingWindow: TrimNullable(draft.PlantingWindow, 200),
                    HarvestCycleMonths: ClampNullableInt(draft.HarvestCycleMonths, 1, 36),
                    SuggestedIrrigationType: TrimNullable(draft.SuggestedIrrigationType, 100),
                    MinSoilMoisture: ClampNullable(draft.MinSoilMoisture, 0, 100),
                    MaxTemperature: ClampNullable(draft.MaxTemperature, -30, 80),
                    MinHumidity: ClampNullable(draft.MinHumidity, 0, 100),
                    Notes: TrimNullable(draft.Notes, 500),
                    SuggestedImage: TrimNullable(draft.SuggestedImage, 10)));

                if (result.Count >= maxSuggestions)
                {
                    break;
                }
            }

            return result;
        }

        private static double? ClampNullable(double? value, double min, double max)
            => value.HasValue ? Math.Clamp(value.Value, min, max) : null;

        private static int? ClampNullableInt(int? value, int min, int max)
            => value.HasValue ? Math.Clamp(value.Value, min, max) : null;

        private static string? TrimNullable(string? value, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            var trimmed = value.Trim();
            return trimmed.Length <= maxLength
                ? trimmed
                : trimmed[..maxLength];
        }
    }
}
