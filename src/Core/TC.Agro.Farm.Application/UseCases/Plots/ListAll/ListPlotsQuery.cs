using TC.Agro.SharedKernel.Infrastructure.Pagination;

namespace TC.Agro.Farm.Application.UseCases.Plots.ListAll
{
    /// <summary>
    /// Query to get a paginated list of ALL plots.
    /// </summary>
    public sealed record ListPlotsQuery : ICachedQuery<PaginatedResponse<ListPlotsResponse>>
    {
        public Guid? OwnerId { get; init; }
        public Guid? PropertyId { get; init; }
        public int PageNumber { get; init; } = 1;
        public int PageSize { get; init; } = 10;
        public string SortBy { get; init; } = "createdat";
        public string SortDirection { get; init; } = "desc";
        public string? Filter { get; init; }
        public string? CropType { get; init; }

        private string? _cacheKey;
        public string GetCacheKey
        {
            get => _cacheKey ?? $"ListPlotsQuery-{OwnerId}-{PageNumber}-{PageSize}-{SortBy}-{SortDirection}-{Filter}-{CropType}-{PropertyId}";
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
            _cacheKey = $"ListPlotsQuery-{OwnerId}-{PageNumber}-{PageSize}-{SortBy}-{SortDirection}-{Filter}-{CropType}-{PropertyId}-{cacheKey}";
        }
    }
}
