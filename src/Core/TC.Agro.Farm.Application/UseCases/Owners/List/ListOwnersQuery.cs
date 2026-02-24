using TC.Agro.SharedKernel.Infrastructure.Pagination;

namespace TC.Agro.Farm.Application.UseCases.Owners.List
{
    /// <summary>
    /// Query to get a paginated list of active owner snapshots.
    /// </summary>
    public sealed record ListOwnersQuery : ICachedQuery<PaginatedResponse<ListOwnersResponse>>
    {
        public int PageNumber { get; init; } = 1;
        public int PageSize { get; init; } = 10;
        public string SortBy { get; init; } = "createdat";
        public string SortDirection { get; init; } = "desc";
        public string? Filter { get; init; }

        private string? _cacheKey;
        public string GetCacheKey
        {
            get => _cacheKey ?? $"ListOwnersQuery-{PageNumber}-{PageSize}-{SortBy}-{SortDirection}-{Filter}";
        }

        public TimeSpan? Duration => null;
        public TimeSpan? DistributedCacheDuration => null;

        public IReadOnlyCollection<string> CacheTags =>
        [
            CacheTagCatalog.Owners,
            CacheTagCatalog.OwnerList
        ];

        public void SetCacheKey(string cacheKey)
        {
            _cacheKey = $"ListOwnersQuery-{PageNumber}-{PageSize}-{SortBy}-{SortDirection}-{Filter}-{cacheKey}";
        }
    }
}
