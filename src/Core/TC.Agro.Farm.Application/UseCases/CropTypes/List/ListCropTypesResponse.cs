namespace TC.Agro.Farm.Application.UseCases.CropTypes.List
{
    public sealed record ListCropTypesResponse(
        Guid Id,
        Guid PropertyId,
        Guid OwnerId,
        string PropertyName,
        string OwnerName,
        string CropType,
        string? SuggestedImage,
        string Source,
        bool IsOverride,
        bool IsStale,
        double? ConfidenceScore,
        string? PlantingWindow,
        int? HarvestCycleMonths,
        string? SuggestedIrrigationType,
        double? MinSoilMoisture,
        double? MaxTemperature,
        double? MinHumidity,
        string? Notes,
        string? Model,
        DateTimeOffset? GeneratedAt,
        bool IsActive,
        DateTimeOffset CreatedAt,
        DateTimeOffset? UpdatedAt,
        Guid CropTypeCatalogId,
        Guid? SelectedCropTypeSuggestionId = null);
}
