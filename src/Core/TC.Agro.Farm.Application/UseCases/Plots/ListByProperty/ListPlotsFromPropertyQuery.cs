using TC.Agro.SharedKernel.Infrastructure.Pagination;

namespace TC.Agro.Farm.Application.UseCases.Plots.ListByProperty
{
    /// <summary>
    /// Query to get a paginated list of plots.
    /// </summary>
    public sealed record ListPlotsFromPropertyQuery : ICachedQuery<PaginatedResponse<ListPlotsFromPropertyResponse>>
    {
        public Guid Id { get; init; }

        public int PageNumber { get; init; } = 1;
        public int PageSize { get; init; } = 10;
        public string SortBy { get; init; } = "createdat";
        public string SortDirection { get; init; } = "desc";
        public string? Filter { get; init; }
        public string? CropType { get; init; }
        public Guid? CropTypeCatalogId { get; init; }

        private string? _cacheKey;
        public string GetCacheKey
        {
            get => _cacheKey ?? $"ListPlotsFromPropertyQuery-{PageNumber}-{PageSize}-{SortBy}-{SortDirection}-{Filter}-{CropType}-{CropTypeCatalogId}-{Id}";
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
            _cacheKey = $"ListPlotsFromPropertyQuery-{PageNumber}-{PageSize}-{SortBy}-{SortDirection}-{Filter}-{CropType}-{CropTypeCatalogId}-{Id}-{cacheKey}";
        }
    }
}
