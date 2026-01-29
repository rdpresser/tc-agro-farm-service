namespace TC.Agro.Farm.Application.UseCases.Plots.GetPlotById
{
    internal sealed class GetPlotByIdQueryHandler : BaseQueryHandler<GetPlotByIdQuery, PlotByIdResponse>
    {
        private readonly IPlotReadStore _plotReadStore;
        private readonly ILogger<GetPlotByIdQueryHandler> _logger;

        public GetPlotByIdQueryHandler(
            IPlotReadStore plotReadStore,
            ILogger<GetPlotByIdQueryHandler> logger)
        {
            _plotReadStore = plotReadStore ?? throw new ArgumentNullException(nameof(plotReadStore));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public override async Task<Result<PlotByIdResponse>> ExecuteAsync(
            GetPlotByIdQuery query,
            CancellationToken ct = default)
        {
            _logger.LogDebug("Getting plot {PlotId}", query.Id);

            var plotResponse = await _plotReadStore
                .GetByIdAsync(query.Id, ct)
                .ConfigureAwait(false);

            if (plotResponse is null)
            {
                _logger.LogWarning("Plot {PlotId} not found", query.Id);
                AddError(x => x.Id, "Plot not found.", FarmDomainErrors.PlotNotFound.ErrorCode);
                return BuildNotFoundResult();
            }

            return plotResponse;
        }
    }
}
