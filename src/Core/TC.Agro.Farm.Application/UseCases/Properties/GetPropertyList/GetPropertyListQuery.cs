using TC.Agro.SharedKernel.Infrastructure.Pagination;

namespace TC.Agro.Farm.Application.UseCases.Properties.GetPropertyList
{
    /// <summary>
    /// Query to get a paginated list of properties.
    /// </summary>
    public sealed record GetPropertyListQuery : ICachedQuery<PaginatedResponse<PropertyListResponse>>
    {
        public int PageNumber { get; init; } = 1;
        public int PageSize { get; init; } = 10;
        public string SortBy { get; init; } = "name";
        public string SortDirection { get; init; } = "asc";
        public string Filter { get; init; } = "";
        public Guid? OwnerId { get; init; }

        private string? _cacheKey;
        public string GetCacheKey
        {
            get => _cacheKey ?? $"GetPropertyListQuery-{PageNumber}-{PageSize}-{SortBy}-{SortDirection}-{Filter}-{OwnerId}";
        }

        public TimeSpan? Duration => null;
        public TimeSpan? DistributedCacheDuration => null;

        public IReadOnlyCollection<string> CacheTags =>
        [
            CacheTagCatalog.Properties,
            CacheTagCatalog.PropertyList
        ];

        public void SetCacheKey(string cacheKey)
        {
            _cacheKey = $"GetPropertyListQuery-{PageNumber}-{PageSize}-{SortBy}-{SortDirection}-{Filter}-{OwnerId}-{cacheKey}";
        }
    }
}
