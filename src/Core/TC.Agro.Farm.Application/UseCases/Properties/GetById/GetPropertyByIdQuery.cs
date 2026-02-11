namespace TC.Agro.Farm.Application.UseCases.Properties.GetById
{
    /// <summary>
    /// Query to get a property by its unique identifier.
    /// </summary>
    public sealed record GetPropertyByIdQuery : ICachedQuery<GetPropertyByIdResponse>
    {
        public Guid Id { get; init; }

        private string? _cacheKey;
        public string GetCacheKey => _cacheKey ?? $"GetPropertyByIdQuery-{Id}";
        public TimeSpan? Duration => null;
        public TimeSpan? DistributedCacheDuration => null;

        public IReadOnlyCollection<string> CacheTags =>
        [
            CacheTagCatalog.Properties,
            CacheTagCatalog.PropertyById
        ];

        public void SetCacheKey(string cacheKey)
            => _cacheKey = $"GetPropertyByIdQuery-{Id}-{cacheKey}";
    }
}
