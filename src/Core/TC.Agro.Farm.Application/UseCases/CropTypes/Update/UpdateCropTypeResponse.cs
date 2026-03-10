namespace TC.Agro.Farm.Application.UseCases.CropTypes.Update
{
    public sealed record UpdateCropTypeResponse(
        Guid Id,
        Guid PropertyId,
        Guid OwnerId,
        string CropType,
        string Source,
        bool IsOverride,
        bool IsStale,
        string? PlantingWindow,
        int? HarvestCycleMonths,
        string? SuggestedIrrigationType,
        double? MinSoilMoisture,
        double? MaxTemperature,
        double? MinHumidity,
        string? Notes,
        DateTimeOffset? UpdatedAt);
}
