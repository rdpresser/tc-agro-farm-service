namespace TC.Agro.Farm.Application.UseCases.Properties.GetPropertyById
{
    /// <summary>
    /// Query to get a property by its unique identifier.
    /// </summary>
    public sealed record GetPropertyByIdQuery : ICachedQuery<PropertyByIdResponse>
    {
        public Guid Id { get; init; }

        private string? _cacheKey;
        public string GetCacheKey => _cacheKey ?? $"GetPropertyByIdQuery-{Id}";
        public TimeSpan? Duration => null;
        public TimeSpan? DistributedCacheDuration => null;

        public void SetCacheKey(string cacheKey)
            => _cacheKey = $"GetPropertyByIdQuery-{Id}-{cacheKey}";
    }
}
