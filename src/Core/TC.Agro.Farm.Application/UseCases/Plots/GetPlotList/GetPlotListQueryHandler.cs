using TC.Agro.SharedKernel.Infrastructure.Pagination;

namespace TC.Agro.Farm.Application.UseCases.Plots.GetPlotList
{
    internal sealed class GetPlotListQueryHandler : BaseQueryHandler<GetPlotListQuery, PaginatedResponse<PlotListResponse>>
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

        public override async Task<Result<PaginatedResponse<PlotListResponse>>> ExecuteAsync(
            GetPlotListQuery query,
            CancellationToken ct = default)
        {
            _logger.LogDebug(
                "Getting plot list. Page: {PageNumber}, Size: {PageSize}, PropertyId: {PropertyId}, CropType: {CropType}",
                query.PageNumber,
                query.PageSize,
                query.PropertyId,
                query.CropType);

            var (plots, totalCount) = await _plotReadStore
                .GetPlotListAsync(query, ct)
                .ConfigureAwait(false);

            if (plots is null || !plots.Any())
            {
                return Result<PaginatedResponse<PlotListResponse>>.Success(
                    new PaginatedResponse<PlotListResponse>([], totalCount, query.PageNumber, query.PageSize));
            }

            var response = new PaginatedResponse<PlotListResponse>(
                data: [.. plots],
                totalCount: totalCount,
                pageNumber: query.PageNumber,
                pageSize: query.PageSize
            );

            return Result.Success(response);
        }
    }
}
