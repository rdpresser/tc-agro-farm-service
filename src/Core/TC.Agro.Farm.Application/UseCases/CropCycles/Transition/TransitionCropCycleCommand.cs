namespace TC.Agro.Farm.Application.UseCases.CropCycles.Transition
{
    /// <summary>
    /// Command to transition a crop cycle to a different active status.
    /// </summary>
    public sealed record TransitionCropCycleCommand(
        Guid CropCycleId,
        string NewStatus,
        DateTimeOffset OccurredAt,
        string? Notes = null) : IBaseCommand<TransitionCropCycleResponse>, IInvalidateCache
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
