namespace TC.Agro.Farm.Application.UseCases.CropTypes.GetById
{
    public sealed record GetCropTypeByIdQuery : ICachedQuery<GetCropTypeByIdResponse>
    {
        public Guid Id { get; init; }
        public bool IncludeInactive { get; init; }

        private string? _cacheKey;
        public string GetCacheKey => _cacheKey ?? $"GetCropTypeByIdQuery-{Id}-{IncludeInactive}";
        public TimeSpan? Duration => null;
        public TimeSpan? DistributedCacheDuration => null;

        public IReadOnlyCollection<string> CacheTags =>
        [
            CacheTagCatalog.CropTypes,
            CacheTagCatalog.CropTypeById
        ];

        public void SetCacheKey(string cacheKey)
            => _cacheKey = $"GetCropTypeByIdQuery-{Id}-{IncludeInactive}-{cacheKey}";
    }
}
