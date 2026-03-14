namespace TC.Agro.Farm.Application.UseCases.CropTypes.Create
{
    public sealed record CreateCropTypeCommand(
        string CropType,
        string? PlantingWindow,
        int? HarvestCycleMonths,
        string? SuggestedIrrigationType,
        double? MinSoilMoisture,
        double? MaxTemperature,
        double? MinHumidity,
        string? Notes,
        string? SuggestedImage = null) : IBaseCommand<CreateCropTypeResponse>, IInvalidateCache
    {
        public IReadOnlyCollection<string> CacheTags =>
        [
            CacheTagCatalog.CropTypes,
            CacheTagCatalog.CropTypeList,
            CacheTagCatalog.CropTypeById
        ];
    }
}
