using TC.Agro.SharedKernel.Infrastructure.Pagination;

namespace TC.Agro.Farm.Application.UseCases.Sensors.ListFromPlot
{
    internal sealed class ListSensorsFromPlotQueryHandler : BaseQueryHandler<ListSensorsFromPlotQuery, PaginatedResponse<ListSensorsFromPlotResponse>>
    {
        private readonly ISensorReadStore _sensorReadStore;
        private readonly ILogger<ListSensorsFromPlotQueryHandler> _logger;

        public ListSensorsFromPlotQueryHandler(
            ISensorReadStore sensorReadStore,
            ILogger<ListSensorsFromPlotQueryHandler> logger)
        {
            _sensorReadStore = sensorReadStore ?? throw new ArgumentNullException(nameof(sensorReadStore));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public override async Task<Result<PaginatedResponse<ListSensorsFromPlotResponse>>> ExecuteAsync(
            ListSensorsFromPlotQuery query,
            CancellationToken ct = default)
        {
            _logger.LogDebug(
                "Getting sensor list. Page: {PageNumber}, Size: {PageSize}, PlotId: {PlotId}, Type: {Type}, Status: {Status}",
                query.PageNumber,
                query.PageSize,
                query.Id,
                query.Type,
                query.Status);

            var (sensors, totalCount) = await _sensorReadStore
                .ListSensorsFromPlotAsync(query, ct)
                .ConfigureAwait(false);

            if (sensors is null || !sensors.Any())
            {
                return Result<PaginatedResponse<ListSensorsFromPlotResponse>>.Success(
                    new PaginatedResponse<ListSensorsFromPlotResponse>([], totalCount, query.PageNumber, query.PageSize));
            }

            var response = new PaginatedResponse<ListSensorsFromPlotResponse>(
                data: [.. sensors],
                totalCount: totalCount,
                pageNumber: query.PageNumber,
                pageSize: query.PageSize
            );

            return Result.Success(response);
        }
    }
}
