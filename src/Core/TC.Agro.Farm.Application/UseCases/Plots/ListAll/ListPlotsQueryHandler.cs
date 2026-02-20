using TC.Agro.Farm.Application.UseCases.Plots.ListByProperty;
using TC.Agro.SharedKernel.Infrastructure.Pagination;

namespace TC.Agro.Farm.Application.UseCases.Plots.ListAll
{
    internal sealed class ListPlotsQueryHandler : BaseQueryHandler<ListPlotsQuery, PaginatedResponse<ListPlotsResponse>>
    {
        private readonly IPlotReadStore _plotReadStore;
        private readonly ILogger<ListPlotsQueryHandler> _logger;

        public ListPlotsQueryHandler(
            IPlotReadStore plotReadStore,
            ILogger<ListPlotsQueryHandler> logger)
        {
            _plotReadStore = plotReadStore ?? throw new ArgumentNullException(nameof(plotReadStore));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public override async Task<Result<PaginatedResponse<ListPlotsResponse>>> ExecuteAsync(
            ListPlotsQuery query,
            CancellationToken ct = default)
        {
            _logger.LogDebug(
                "Getting plot list. Page: {PageNumber}, Size: {PageSize}, CropType: {CropType}",
                query.PageNumber,
                query.PageSize,
                query.CropType);

            var (plots, totalCount) = await _plotReadStore
                .ListPlotsAsync(query, ct)
                .ConfigureAwait(false);

            if (plots is null || !plots.Any())
            {
                return Result<PaginatedResponse<ListPlotsResponse>>.Success(
                    new PaginatedResponse<ListPlotsResponse>([], totalCount, query.PageNumber, query.PageSize));
            }

            var response = new PaginatedResponse<ListPlotsResponse>(
                data: [.. plots],
                totalCount: totalCount,
                pageNumber: query.PageNumber,
                pageSize: query.PageSize
            );

            return Result.Success(response);
        }
    }
}
