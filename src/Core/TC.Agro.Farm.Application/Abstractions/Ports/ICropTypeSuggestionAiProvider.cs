namespace TC.Agro.Farm.Application.Abstractions.Ports
{
    /// <summary>
    /// Abstraction for generating location-aware crop type suggestions.
    /// </summary>
    public interface ICropTypeSuggestionAiProvider
    {
        Task<IReadOnlyList<CropTypeSuggestionDraft>> GenerateSuggestionsAsync(
            CropTypeSuggestionAiRequest request,
            CancellationToken cancellationToken = default);
    }

    public sealed record CropTypeSuggestionAiRequest(
        Guid PropertyId,
        Guid OwnerId,
        string City,
        string State,
        string Country,
        double Latitude,
        double Longitude,
        int SuggestionCount = 15);

    public sealed record CropTypeSuggestionDraft(
        string CropType,
        double? ConfidenceScore,
        string? PlantingWindow,
        int? HarvestCycleMonths,
        string? SuggestedIrrigationType,
        double? MinSoilMoisture,
        double? MaxTemperature,
        double? MinHumidity,
        string? Notes);
}
