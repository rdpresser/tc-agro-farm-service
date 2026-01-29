namespace TC.Agro.Farm.Application.UseCases.Sensors.GetSensorById
{
    internal sealed class GetSensorByIdQueryHandler : BaseQueryHandler<GetSensorByIdQuery, SensorByIdResponse>
    {
        private readonly ISensorReadStore _sensorReadStore;
        private readonly ILogger<GetSensorByIdQueryHandler> _logger;

        public GetSensorByIdQueryHandler(
            ISensorReadStore sensorReadStore,
            ILogger<GetSensorByIdQueryHandler> logger)
        {
            _sensorReadStore = sensorReadStore ?? throw new ArgumentNullException(nameof(sensorReadStore));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public override async Task<Result<SensorByIdResponse>> ExecuteAsync(
            GetSensorByIdQuery query,
            CancellationToken ct = default)
        {
            _logger.LogDebug("Getting sensor {SensorId}", query.Id);

            var sensorResponse = await _sensorReadStore
                .GetByIdAsync(query.Id, ct)
                .ConfigureAwait(false);

            if (sensorResponse is null)
            {
                _logger.LogWarning("Sensor {SensorId} not found", query.Id);
                AddError(x => x.Id, "Sensor not found.", FarmDomainErrors.SensorNotFound.ErrorCode);
                return BuildNotFoundResult();
            }

            return sensorResponse;
        }
    }
}
