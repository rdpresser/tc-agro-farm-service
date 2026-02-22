using TC.Agro.SharedKernel.Infrastructure.Pagination;

namespace TC.Agro.Farm.Application.UseCases.Sensors.ListAll
{
    internal sealed class ListSensorsQueryHandler : BaseQueryHandler<ListSensorsQuery, PaginatedResponse<ListSensorsResponse>>
    {
        private readonly ISensorReadStore _sensorReadStore;
        private readonly ILogger<ListSensorsQueryHandler> _logger;

        public ListSensorsQueryHandler(
            ISensorReadStore sensorReadStore,
            ILogger<ListSensorsQueryHandler> logger)
        {
            _sensorReadStore = sensorReadStore ?? throw new ArgumentNullException(nameof(sensorReadStore));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public override async Task<Result<PaginatedResponse<ListSensorsResponse>>> ExecuteAsync(
            ListSensorsQuery query,
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
                .ListSensorsAsync(query, ct)
                .ConfigureAwait(false);

            if (sensors is null || !sensors.Any())
            {
                return Result<PaginatedResponse<ListSensorsResponse>>.Success(
                    new PaginatedResponse<ListSensorsResponse>([], totalCount, query.PageNumber, query.PageSize));
            }

            var response = new PaginatedResponse<ListSensorsResponse>(
                data: [.. sensors],
                totalCount: totalCount,
                pageNumber: query.PageNumber,
                pageSize: query.PageSize
            );

            return Result.Success(response);
        }
    }
}
