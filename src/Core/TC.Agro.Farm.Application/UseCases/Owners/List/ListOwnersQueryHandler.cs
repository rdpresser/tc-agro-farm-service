using TC.Agro.SharedKernel.Infrastructure.Pagination;

namespace TC.Agro.Farm.Application.UseCases.Owners.List
{
    internal sealed class ListOwnersQueryHandler : BaseQueryHandler<ListOwnersQuery, PaginatedResponse<ListOwnersResponse>>
    {
        private readonly IOwnerReadStore _ownerReadStore;
        private readonly ILogger<ListOwnersQueryHandler> _logger;

        public ListOwnersQueryHandler(
            IOwnerReadStore ownerReadStore,
            ILogger<ListOwnersQueryHandler> logger)
        {
            _ownerReadStore = ownerReadStore ?? throw new ArgumentNullException(nameof(ownerReadStore));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public override async Task<Result<PaginatedResponse<ListOwnersResponse>>> ExecuteAsync(
            ListOwnersQuery query,
            CancellationToken ct = default)
        {
            _logger.LogDebug(
                "Getting owner list. Page: {PageNumber}, Size: {PageSize}, Filter: {Filter}",
                query.PageNumber,
                query.PageSize,
                query.Filter);

            var (owners, totalCount) = await _ownerReadStore
                .ListOwnersAsync(query, ct)
                .ConfigureAwait(false);

            if (owners is null || !owners.Any())
            {
                return Result<PaginatedResponse<ListOwnersResponse>>.Success(
                    new PaginatedResponse<ListOwnersResponse>([], totalCount, query.PageNumber, query.PageSize));
            }

            var response = new PaginatedResponse<ListOwnersResponse>(
                data: [.. owners],
                totalCount: totalCount,
                pageNumber: query.PageNumber,
                pageSize: query.PageSize
            );

            return Result.Success(response);
        }
    }
}
