namespace TC.Agro.Farm.Application.UseCases.Plots.GetPlotById
{
    /// <summary>
    /// Query to get a plot by its unique identifier.
    /// </summary>
    public sealed record GetPlotByIdQuery : ICachedQuery<PlotByIdResponse>
    {
        public Guid Id { get; init; }

        private string? _cacheKey;
        public string GetCacheKey => _cacheKey ?? $"GetPlotByIdQuery-{Id}";
        public TimeSpan? Duration => null;
        public TimeSpan? DistributedCacheDuration => null;

        public IReadOnlyCollection<string> CacheTags =>
        [
            CacheTagCatalog.Plots,
            CacheTagCatalog.PlotById
        ];

        public void SetCacheKey(string cacheKey)
            => _cacheKey = $"GetPlotByIdQuery-{Id}-{cacheKey}";
    }
}
