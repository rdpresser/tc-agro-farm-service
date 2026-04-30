namespace TC.Agro.Farm.Application.UseCases.CropTypes.Options
{
    public sealed record CropTypeOptionResponse(
        Guid Id,
        string CropType,
        string? SuggestedImage,
        string Source,
        bool IsStale,
        bool IsActive,
        Guid CropTypeCatalogId,
        Guid? SelectedCropTypeSuggestionId = null,
        string? PlantingWindow = null,
        int? HarvestCycleMonths = null,
        string? SuggestedIrrigationType = null,
        double? MinSoilMoisture = null,
        double? MaxTemperature = null,
        double? MinHumidity = null);
}
