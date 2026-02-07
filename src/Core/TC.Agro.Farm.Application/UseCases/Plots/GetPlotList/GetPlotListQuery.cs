using TC.Agro.SharedKernel.Infrastructure.Pagination;

namespace TC.Agro.Farm.Application.UseCases.Plots.GetPlotList
{
    /// <summary>
    /// Query to get a paginated list of plots.
    /// </summary>
    public sealed record GetPlotListQuery : ICachedQuery<PaginatedResponse<PlotListResponse>>
    {
        public int PageNumber { get; init; } = 1;
        public int PageSize { get; init; } = 10;
        public string SortBy { get; init; } = "name";
        public string SortDirection { get; init; } = "asc";
        public string Filter { get; init; } = "";
        public Guid? PropertyId { get; init; }
        public string? CropType { get; init; }

        private string? _cacheKey;
        public string GetCacheKey
        {
            get => _cacheKey ?? $"GetPlotListQuery-{PageNumber}-{PageSize}-{SortBy}-{SortDirection}-{Filter}-{PropertyId}-{CropType}";
        }

        public TimeSpan? Duration => null;
        public TimeSpan? DistributedCacheDuration => null;

        public IReadOnlyCollection<string> CacheTags =>
        [
            CacheTagCatalog.Plots,
            CacheTagCatalog.PlotList
        ];

        public void SetCacheKey(string cacheKey)
        {
            _cacheKey = $"GetPlotListQuery-{PageNumber}-{PageSize}-{SortBy}-{SortDirection}-{Filter}-{PropertyId}-{CropType}-{cacheKey}";
        }
    }
}
