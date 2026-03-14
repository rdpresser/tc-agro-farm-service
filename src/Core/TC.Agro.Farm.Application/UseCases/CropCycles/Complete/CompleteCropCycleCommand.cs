namespace TC.Agro.Farm.Application.UseCases.CropCycles.Complete
{
    /// <summary>
    /// Command to complete (close) an active crop cycle.
    /// </summary>
    public sealed record CompleteCropCycleCommand(
        Guid CropCycleId,
        DateTimeOffset EndedAt,
        string FinalStatus = CropCycleStatus.Harvested,
        string? Notes = null) : IBaseCommand<CompleteCropCycleResponse>, IInvalidateCache
    {
        public IReadOnlyCollection<string> CacheTags =>
        [
            CacheTagCatalog.CropCycles,
            CacheTagCatalog.CropCycleList,
            CacheTagCatalog.CropCycleById,
            CacheTagCatalog.Plots,
            CacheTagCatalog.PlotById
        ];
    }
}
