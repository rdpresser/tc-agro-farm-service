namespace TC.Agro.Farm.Application.UseCases.Plots.Update
{
    /// <summary>
    /// Command to update an existing plot.
    /// </summary>
    public sealed record UpdatePlotCommand(
        Guid PlotId,
        string Name,
        string CropType,
        double AreaHectares,
        double? Latitude,
        double? Longitude,
        string? BoundaryGeoJson,
        DateTimeOffset PlantingDate,
        DateTimeOffset ExpectedHarvestDate,
        string IrrigationType,
        string? AdditionalNotes,
        Guid? CropTypeCatalogId = null,
        Guid? SelectedCropTypeSuggestionId = null) : IBaseCommand<UpdatePlotResponse>, IInvalidateCache
    {
        public IReadOnlyCollection<string> CacheTags =>
        [
            CacheTagCatalog.Plots,
            CacheTagCatalog.PlotList,
            CacheTagCatalog.PlotById
        ];
    }
}
