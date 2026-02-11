using TC.Agro.SharedKernel.Infrastructure.Pagination;

namespace TC.Agro.Farm.Application.UseCases.Plots.ListByProperty
{
    internal sealed class ListPlotsFromPropertyQueryHandler : BaseQueryHandler<ListPlotsFromPropertyQuery, PaginatedResponse<ListPlotsFromPropertyResponse>>
    {
        private readonly IPlotReadStore _plotReadStore;
        private readonly ILogger<ListPlotsFromPropertyQueryHandler> _logger;

        public ListPlotsFromPropertyQueryHandler(
            IPlotReadStore plotReadStore,
            ILogger<ListPlotsFromPropertyQueryHandler> logger)
        {
            _plotReadStore = plotReadStore ?? throw new ArgumentNullException(nameof(plotReadStore));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public override async Task<Result<PaginatedResponse<ListPlotsFromPropertyResponse>>> ExecuteAsync(
            ListPlotsFromPropertyQuery query,
            CancellationToken ct = default)
        {
            _logger.LogDebug(
                "Getting plot list. Page: {PageNumber}, Size: {PageSize}, PropertyId: {PropertyId}, CropType: {CropType}",
                query.PageNumber,
                query.PageSize,
                query.Id,
                query.CropType);

            var (plots, totalCount) = await _plotReadStore
                .ListPlotsFromPropertyAsync(query, ct)
                .ConfigureAwait(false);

            if (plots is null || !plots.Any())
            {
                return Result<PaginatedResponse<ListPlotsFromPropertyResponse>>.Success(
                    new PaginatedResponse<ListPlotsFromPropertyResponse>([], totalCount, query.PageNumber, query.PageSize));
            }

            var response = new PaginatedResponse<ListPlotsFromPropertyResponse>(
                data: [.. plots],
                totalCount: totalCount,
                pageNumber: query.PageNumber,
                pageSize: query.PageSize
            );

            return Result.Success(response);
        }
    }
}
