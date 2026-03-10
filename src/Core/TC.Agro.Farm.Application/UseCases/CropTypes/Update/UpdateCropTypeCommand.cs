namespace TC.Agro.Farm.Application.UseCases.CropTypes.Update
{
    public sealed record UpdateCropTypeCommand(
        Guid CropTypeId,
        string CropType,
        string? PlantingWindow,
        int? HarvestCycleMonths,
        string? SuggestedIrrigationType,
        double? MinSoilMoisture,
        double? MaxTemperature,
        double? MinHumidity,
        string? Notes) : IBaseCommand<UpdateCropTypeResponse>, IInvalidateCache
    {
        public IReadOnlyCollection<string> CacheTags =>
        [
            CacheTagCatalog.CropTypes,
            CacheTagCatalog.CropTypeList,
            CacheTagCatalog.CropTypeById
        ];
    }
}
