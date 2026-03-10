namespace TC.Agro.Farm.Application.UseCases.CropTypes.Create
{
    public sealed record CreateCropTypeResponse(
        Guid Id,
        Guid PropertyId,
        Guid OwnerId,
        string CropType,
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
        DateTimeOffset CreatedAt);
}
