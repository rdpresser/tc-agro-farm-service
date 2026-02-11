using TC.Agro.SharedKernel.Infrastructure.Pagination;

namespace TC.Agro.Farm.Application.UseCases.Properties.List
{
    internal sealed class ListPropertiesHandler : BaseQueryHandler<ListPropertiesQuery, PaginatedResponse<ListPropertiesResponse>>
    {
        private readonly IPropertyReadStore _propertyReadStore;
        private readonly ILogger<ListPropertiesHandler> _logger;

        public ListPropertiesHandler(
            IPropertyReadStore propertyReadStore,
            IUserContext userContext,
            ILogger<ListPropertiesHandler> logger)
        {
            _propertyReadStore = propertyReadStore ?? throw new ArgumentNullException(nameof(propertyReadStore));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public override async Task<Result<PaginatedResponse<ListPropertiesResponse>>> ExecuteAsync(
            ListPropertiesQuery query,
            CancellationToken ct = default)
        {
            _logger.LogDebug(
                "Getting property list. Page: {PageNumber}, Size: {PageSize}, Filter: {Filter}",
                query.PageNumber,
                query.PageSize,
                query.Filter);

            var (properties, totalCount) = await _propertyReadStore
                .GetPropertyListAsync(query, ct)
                .ConfigureAwait(false);

            if (properties is null || !properties.Any())
            {
                return Result<PaginatedResponse<ListPropertiesResponse>>.Success(
                    new PaginatedResponse<ListPropertiesResponse>([], totalCount, query.PageNumber, query.PageSize));
            }

            var response = new PaginatedResponse<ListPropertiesResponse>(
                data: [.. properties],
                totalCount: totalCount,
                pageNumber: query.PageNumber,
                pageSize: query.PageSize
            );

            return Result.Success(response);
        }
    }
}
