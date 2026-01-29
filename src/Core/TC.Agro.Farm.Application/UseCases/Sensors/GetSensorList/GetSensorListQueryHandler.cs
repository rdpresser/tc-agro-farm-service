namespace TC.Agro.Farm.Application.UseCases.Sensors.GetSensorList
{
    internal sealed class GetSensorListQueryHandler : BaseQueryHandler<GetSensorListQuery, IReadOnlyList<SensorListResponse>>
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

        public override async Task<Result<IReadOnlyList<SensorListResponse>>> ExecuteAsync(
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

            var sensors = await _sensorReadStore
                .GetSensorListAsync(query, ct)
                .ConfigureAwait(false);

            if (sensors is null || !sensors.Any())
            {
                return Result<IReadOnlyList<SensorListResponse>>.Success([]);
            }

            return Result.Success<IReadOnlyList<SensorListResponse>>([.. sensors]);
        }
    }
}
