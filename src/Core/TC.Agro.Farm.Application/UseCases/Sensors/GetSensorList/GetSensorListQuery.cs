using TC.Agro.SharedKernel.Infrastructure.Pagination;

namespace TC.Agro.Farm.Application.UseCases.Sensors.GetSensorList
{
    /// <summary>
    /// Query to get a paginated list of sensors.
    /// </summary>
    public sealed record GetSensorListQuery : ICachedQuery<PaginatedResponse<SensorListResponse>>
    {
        public int PageNumber { get; init; } = 1;
        public int PageSize { get; init; } = 10;
        public string SortBy { get; init; } = "installedAt";
        public string SortDirection { get; init; } = "desc";
        public string Filter { get; init; } = "";
        public Guid? PlotId { get; init; }
        public Guid? PropertyId { get; init; }
        public string? Type { get; init; }
        public string? Status { get; init; }

        private string? _cacheKey;
        public string GetCacheKey
        {
            get => _cacheKey ?? $"GetSensorListQuery-{PageNumber}-{PageSize}-{SortBy}-{SortDirection}-{Filter}-{PlotId}-{PropertyId}-{Type}-{Status}";
        }

        public TimeSpan? Duration => null;
        public TimeSpan? DistributedCacheDuration => null;
        public IReadOnlyCollection<string> CacheTags => new[]
        {
            global::TC.Agro.Farm.Application.Abstractions.CacheTags.Sensors,
            global::TC.Agro.Farm.Application.Abstractions.CacheTags.SensorList
        };

        public void SetCacheKey(string cacheKey)
        {
            _cacheKey = $"GetSensorListQuery-{PageNumber}-{PageSize}-{SortBy}-{SortDirection}-{Filter}-{PlotId}-{PropertyId}-{Type}-{Status}-{cacheKey}";
        }
    }
}
