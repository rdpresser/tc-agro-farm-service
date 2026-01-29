namespace TC.Agro.Farm.Application.UseCases.Plots.GetPlotList
{
    internal sealed class GetPlotListQueryHandler : BaseQueryHandler<GetPlotListQuery, IReadOnlyList<PlotListResponse>>
    {
        private readonly IPlotReadStore _plotReadStore;
        private readonly ILogger<GetPlotListQueryHandler> _logger;

        public GetPlotListQueryHandler(
            IPlotReadStore plotReadStore,
            ILogger<GetPlotListQueryHandler> logger)
        {
            _plotReadStore = plotReadStore ?? throw new ArgumentNullException(nameof(plotReadStore));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public override async Task<Result<IReadOnlyList<PlotListResponse>>> ExecuteAsync(
            GetPlotListQuery query,
            CancellationToken ct = default)
        {
            _logger.LogDebug(
                "Getting plot list. Page: {PageNumber}, Size: {PageSize}, PropertyId: {PropertyId}, CropType: {CropType}",
                query.PageNumber,
                query.PageSize,
                query.PropertyId,
                query.CropType);

            var plots = await _plotReadStore
                .GetPlotListAsync(query, ct)
                .ConfigureAwait(false);

            if (plots is null || !plots.Any())
            {
                return Result<IReadOnlyList<PlotListResponse>>.Success([]);
            }

            return Result.Success<IReadOnlyList<PlotListResponse>>([.. plots]);
        }
    }
}
