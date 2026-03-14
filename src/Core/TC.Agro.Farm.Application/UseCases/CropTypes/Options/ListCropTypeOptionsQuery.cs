namespace TC.Agro.Farm.Application.UseCases.CropTypes.Options
{
    public sealed record ListCropTypeOptionsQuery : ICachedQuery<IReadOnlyList<CropTypeOptionResponse>>
    {
        public Guid? OwnerId { get; init; }
        public Guid? PropertyId { get; init; }
        public string? Source { get; init; }
        public bool IncludeStale { get; init; }
        public bool IncludeInactive { get; init; }
        public string? Filter { get; init; }
        public int Limit { get; init; } = 200;

        private string? _cacheKey;
        public string GetCacheKey => _cacheKey ??
            $"ListCropTypeOptionsQuery-{OwnerId}-{PropertyId}-{Source}-{IncludeStale}-{IncludeInactive}-{Filter}-{Limit}";

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
                $"ListCropTypeOptionsQuery-{OwnerId}-{PropertyId}-{Source}-{IncludeStale}-{IncludeInactive}-{Filter}-{Limit}-{cacheKey}";
        }
    }
}
