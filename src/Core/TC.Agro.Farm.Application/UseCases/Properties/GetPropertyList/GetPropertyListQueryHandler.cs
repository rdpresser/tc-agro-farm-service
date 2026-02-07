using TC.Agro.SharedKernel.Infrastructure.Pagination;

namespace TC.Agro.Farm.Application.UseCases.Properties.GetPropertyList
{
    internal sealed class GetPropertyListQueryHandler : BaseQueryHandler<GetPropertyListQuery, PaginatedResponse<PropertyListResponse>>
    {
        private readonly IPropertyReadStore _propertyReadStore;
        private readonly IUserContext _userContext;
        private readonly ILogger<GetPropertyListQueryHandler> _logger;

        public GetPropertyListQueryHandler(
            IPropertyReadStore propertyReadStore,
            IUserContext userContext,
            ILogger<GetPropertyListQueryHandler> logger)
        {
            _propertyReadStore = propertyReadStore ?? throw new ArgumentNullException(nameof(propertyReadStore));
            _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public override async Task<Result<PaginatedResponse<PropertyListResponse>>> ExecuteAsync(
            GetPropertyListQuery query,
            CancellationToken ct = default)
        {
            _logger.LogDebug(
                "Getting property list. Page: {PageNumber}, Size: {PageSize}, Filter: {Filter}",
                query.PageNumber,
                query.PageSize,
                query.Filter);

            // For non-admin users, force filter by their own OwnerId
            var effectiveQuery = query;
            if (_userContext.Role != AppConstants.AdminRole)
            {
                effectiveQuery = query with { OwnerId = _userContext.Id };
            }

            var (properties, totalCount) = await _propertyReadStore
                .GetPropertyListAsync(effectiveQuery, ct)
                .ConfigureAwait(false);

            if (properties is null || !properties.Any())
            {
                return Result<PaginatedResponse<PropertyListResponse>>.Success(
                    new PaginatedResponse<PropertyListResponse>([], totalCount, query.PageNumber, query.PageSize));
            }

            var response = new PaginatedResponse<PropertyListResponse>(
                data: [.. properties],
                totalCount: totalCount,
                pageNumber: query.PageNumber,
                pageSize: query.PageSize
            );

            return Result.Success(response);
        }
    }
}
