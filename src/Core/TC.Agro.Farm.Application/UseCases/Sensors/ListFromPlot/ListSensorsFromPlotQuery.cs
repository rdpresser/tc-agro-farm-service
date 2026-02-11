using TC.Agro.SharedKernel.Infrastructure.Pagination;

namespace TC.Agro.Farm.Application.UseCases.Sensors.ListFromPlot
{
    /// <summary>
    /// Query to get a paginated list of sensors.
    /// </summary>
    public sealed record ListSensorsFromPlotQuery : ICachedQuery<PaginatedResponse<ListSensorsFromPlotResponse>>
    {
        public Guid Id { get; init; }

        public int PageNumber { get; init; } = 1;
        public int PageSize { get; init; } = 10;
        public string SortBy { get; init; } = "installedAt";
        public string SortDirection { get; init; } = "desc";
        public string? Filter { get; init; }
        public string? Type { get; init; }
        public string? Status { get; init; }

        private string? _cacheKey;
        public string GetCacheKey
        {
            get => _cacheKey ?? $"GetSensorListQuery-{PageNumber}-{PageSize}-{SortBy}-{SortDirection}-{Filter}-{Id}-{Type}-{Status}";
        }

        public TimeSpan? Duration => null;
        public TimeSpan? DistributedCacheDuration => null;
        public IReadOnlyCollection<string> CacheTags => new[]
        {
            CacheTagCatalog.Sensors,
            CacheTagCatalog.SensorList
        };

        public void SetCacheKey(string cacheKey)
        {
            _cacheKey = $"GetSensorListQuery-{PageNumber}-{PageSize}-{SortBy}-{SortDirection}-{Filter}-{Id}-{Type}-{Status}-{cacheKey}";
        }
    }
}
