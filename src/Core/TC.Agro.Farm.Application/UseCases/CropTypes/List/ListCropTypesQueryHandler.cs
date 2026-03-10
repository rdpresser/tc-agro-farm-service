using TC.Agro.SharedKernel.Infrastructure.Pagination;

namespace TC.Agro.Farm.Application.UseCases.CropTypes.List
{
    internal sealed class ListCropTypesQueryHandler : BaseQueryHandler<ListCropTypesQuery, PaginatedResponse<ListCropTypesResponse>>
    {
        private readonly ICropTypeSuggestionReadStore _readStore;
        private readonly ILogger<ListCropTypesQueryHandler> _logger;

        public ListCropTypesQueryHandler(
            ICropTypeSuggestionReadStore readStore,
            IUserContext userContext,
            ILogger<ListCropTypesQueryHandler> logger)
        {
            _readStore = readStore ?? throw new ArgumentNullException(nameof(readStore));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public override async Task<Result<PaginatedResponse<ListCropTypesResponse>>> ExecuteAsync(
            ListCropTypesQuery query,
            CancellationToken ct = default)
        {
            _logger.LogDebug(
                "Listing crop types. OwnerId={OwnerId}, PropertyId={PropertyId}, IncludeStale={IncludeStale}, IncludeInactive={IncludeInactive}, Page={PageNumber}, Size={PageSize}",
                query.OwnerId,
                query.PropertyId,
                query.IncludeStale,
                query.IncludeInactive,
                query.PageNumber,
                query.PageSize);

            var (cropTypes, totalCount) = await _readStore
                .ListAsync(query, ct)
                .ConfigureAwait(false);

            var response = new PaginatedResponse<ListCropTypesResponse>(
                data: [.. cropTypes],
                totalCount: totalCount,
                pageNumber: query.PageNumber,
                pageSize: query.PageSize);

            return Result.Success(response);
        }
    }
}
