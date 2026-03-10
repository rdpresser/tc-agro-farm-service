using TC.Agro.SharedKernel.Infrastructure.Pagination;

namespace TC.Agro.Farm.Application.UseCases.CropTypes.List
{
    public sealed record ListCropTypesQuery : ICachedQuery<PaginatedResponse<ListCropTypesResponse>>
    {
        public Guid? OwnerId { get; init; }
        public Guid? PropertyId { get; init; }
        public string? Source { get; init; }
        public bool IncludeStale { get; init; }
        public bool IncludeInactive { get; init; }

        public int PageNumber { get; init; } = 1;
        public int PageSize { get; init; } = 10;
        public string SortBy { get; init; } = "createdAt";
        public string SortDirection { get; init; } = "desc";
        public string? Filter { get; init; }

        private string? _cacheKey;
        public string GetCacheKey => _cacheKey ??
            $"ListCropTypesQuery-{OwnerId}-{PropertyId}-{Source}-{IncludeStale}-{IncludeInactive}-{PageNumber}-{PageSize}-{SortBy}-{SortDirection}-{Filter}";

        public TimeSpan? Duration => null;
        public TimeSpan? DistributedCacheDuration => null;

        public IReadOnlyCollection<string> CacheTags =>
        [
            CacheTagCatalog.CropTypes,
            CacheTagCatalog.CropTypeList
        ];

        public void SetCacheKey(string cacheKey)
        {
            _cacheKey =
                $"ListCropTypesQuery-{OwnerId}-{PropertyId}-{Source}-{IncludeStale}-{IncludeInactive}-{PageNumber}-{PageSize}-{SortBy}-{SortDirection}-{Filter}-{cacheKey}";
        }
    }
}
