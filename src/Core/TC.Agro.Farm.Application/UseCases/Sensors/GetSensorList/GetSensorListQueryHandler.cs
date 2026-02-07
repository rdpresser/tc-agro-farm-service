using TC.Agro.SharedKernel.Infrastructure.Pagination;

namespace TC.Agro.Farm.Application.UseCases.Sensors.GetSensorList
{
    internal sealed class GetSensorListQueryHandler : BaseQueryHandler<GetSensorListQuery, PaginatedResponse<SensorListResponse>>
    {
        private readonly ISensorReadStore _sensorReadStore;
        private readonly ILogger<GetSensorListQueryHandler> _logger;

        public GetSensorListQueryHandler(
            ISensorReadStore sensorReadStore,
            ILogger<GetSensorListQueryHandler> logger)
        {
            _sensorReadStore = sensorReadStore ?? throw new ArgumentNullException(nameof(sensorReadStore));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public override async Task<Result<PaginatedResponse<SensorListResponse>>> ExecuteAsync(
            GetSensorListQuery query,
            CancellationToken ct = default)
        {
            _logger.LogDebug(
                "Getting sensor list. Page: {PageNumber}, Size: {PageSize}, PlotId: {PlotId}, Type: {Type}, Status: {Status}",
                query.PageNumber,
                query.PageSize,
                query.PlotId,
                query.Type,
                query.Status);

            var (sensors, totalCount) = await _sensorReadStore
                .GetSensorListAsync(query, ct)
                .ConfigureAwait(false);

            if (sensors is null || !sensors.Any())
            {
                return Result<PaginatedResponse<SensorListResponse>>.Success(
                    new PaginatedResponse<SensorListResponse>([], totalCount, query.PageNumber, query.PageSize));
            }

            var response = new PaginatedResponse<SensorListResponse>(
                data: [.. sensors],
                totalCount: totalCount,
                pageNumber: query.PageNumber,
                pageSize: query.PageSize
            );

            return Result.Success(response);
        }
    }
}
