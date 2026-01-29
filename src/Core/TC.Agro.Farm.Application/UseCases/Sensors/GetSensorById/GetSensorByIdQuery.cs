namespace TC.Agro.Farm.Application.UseCases.Sensors.GetSensorById
{
    /// <summary>
    /// Query to get a sensor by its unique identifier.
    /// </summary>
    public sealed record GetSensorByIdQuery : ICachedQuery<SensorByIdResponse>
    {
        public Guid Id { get; init; }

        private string? _cacheKey;
        public string GetCacheKey => _cacheKey ?? $"GetSensorByIdQuery-{Id}";
        public TimeSpan? Duration => null;
        public TimeSpan? DistributedCacheDuration => null;

        public void SetCacheKey(string cacheKey)
            => _cacheKey = $"GetSensorByIdQuery-{Id}-{cacheKey}";
    }
}
