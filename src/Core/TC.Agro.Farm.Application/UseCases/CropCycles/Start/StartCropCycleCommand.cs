namespace TC.Agro.Farm.Application.UseCases.CropCycles.Start
{
    /// <summary>
    /// Command to start a new crop cycle on a plot.
    /// </summary>
    public sealed record StartCropCycleCommand(
        Guid PlotId,
        Guid CropTypeCatalogId,
        DateTimeOffset StartedAt,
        DateTimeOffset? ExpectedHarvestDate,
        string Status = CropCycleStatus.Planned,
        Guid? SelectedCropTypeSuggestionId = null,
        Guid? OwnerId = null,
        string? Notes = null) : IBaseCommand<StartCropCycleResponse>, IInvalidateCache
    {
        public IReadOnlyCollection<string> CacheTags =>
        [
            CacheTagCatalog.CropCycles,
            CacheTagCatalog.CropCycleList,
            CacheTagCatalog.Plots,
            CacheTagCatalog.PlotById
        ];
    }
}
