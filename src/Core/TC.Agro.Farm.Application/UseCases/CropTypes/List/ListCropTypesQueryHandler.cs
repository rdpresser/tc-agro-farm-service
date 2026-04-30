using TC.Agro.SharedKernel.Infrastructure.Pagination;

namespace TC.Agro.Farm.Application.UseCases.CropTypes.List
{
    internal sealed class ListCropTypesQueryHandler : BaseQueryHandler<ListCropTypesQuery, PaginatedResponse<ListCropTypesResponse>>
    {
        private const string CatalogSource = "Catalog";
        private const int MaxMixedQueryPageSize = 5000;

        private readonly ICropTypeCatalogReadStore _catalogReadStore;
        private readonly ICropTypeSuggestionReadStore _suggestionReadStore;
        private readonly ILogger<ListCropTypesQueryHandler> _logger;

        public ListCropTypesQueryHandler(
            ICropTypeCatalogReadStore catalogReadStore,
            ICropTypeSuggestionReadStore suggestionReadStore,
            IUserContext userContext,
            ILogger<ListCropTypesQueryHandler> logger)
        {
            _catalogReadStore = catalogReadStore ?? throw new ArgumentNullException(nameof(catalogReadStore));
            _suggestionReadStore = suggestionReadStore ?? throw new ArgumentNullException(nameof(suggestionReadStore));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public override async Task<Result<PaginatedResponse<ListCropTypesResponse>>> ExecuteAsync(
            ListCropTypesQuery query,
            CancellationToken ct = default)
        {
            _logger.LogDebug(
                "Listing crop types. OwnerId={OwnerId}, IncludeSuggestions={IncludeSuggestions}, IncludeStale={IncludeStale}, IncludeInactive={IncludeInactive}, Page={PageNumber}, Size={PageSize}",
                query.OwnerId,
                query.IncludeSuggestions,
                query.IncludeStale,
                query.IncludeInactive,
                query.PageNumber,
                query.PageSize);

            var (cropTypes, totalCount) = query.IncludeSuggestions
                ? await ListMixedAsync(query, ct).ConfigureAwait(false)
                : await _catalogReadStore.ListAsync(query, ct).ConfigureAwait(false);

            var response = new PaginatedResponse<ListCropTypesResponse>(
                data: [.. cropTypes],
                totalCount: totalCount,
                pageNumber: query.PageNumber,
                pageSize: query.PageSize);

            return Result.Success(response);
        }

        private async Task<(IReadOnlyList<ListCropTypesResponse> CropTypes, int TotalCount)> ListMixedAsync(
            ListCropTypesQuery query,
            CancellationToken ct)
        {
            var includeCatalog = ShouldIncludeCatalog(query.Source);
            var includeSuggestions = ShouldIncludeSuggestions(query.Source);

            var mixedQuery = query with
            {
                PageNumber = 1,
                PageSize = CalculateMixedQueryFetchSize(query.PageNumber, query.PageSize)
            };

            var catalogTask = includeCatalog
                ? _catalogReadStore.ListAsync(mixedQuery, ct)
                : Task.FromResult<(IReadOnlyList<ListCropTypesResponse> CropTypes, int TotalCount)>(([], 0));

            var suggestionTask = includeSuggestions
                ? _suggestionReadStore.ListAsync(mixedQuery, ct)
                : Task.FromResult<(IReadOnlyList<ListCropTypesResponse> CropTypes, int TotalCount)>(([], 0));

            await Task.WhenAll(catalogTask, suggestionTask).ConfigureAwait(false);

            var (catalogRows, catalogTotal) = await catalogTask.ConfigureAwait(false);
            var (suggestionRows, suggestionTotal) = await suggestionTask.ConfigureAwait(false);

            var mergedRows = catalogRows
                .Concat(suggestionRows)
                .ToList();

            var sortedRows = ApplySorting(mergedRows, query.SortBy, query.SortDirection);
            var pagedRows = sortedRows
                .Skip((query.PageNumber - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToList();

            return (pagedRows, catalogTotal + suggestionTotal);
        }

        private static bool ShouldIncludeCatalog(string? source)
            => string.IsNullOrWhiteSpace(source)
               || string.Equals(source, CatalogSource, StringComparison.OrdinalIgnoreCase);

        private static bool ShouldIncludeSuggestions(string? source)
            => string.IsNullOrWhiteSpace(source)
               || !string.Equals(source, CatalogSource, StringComparison.OrdinalIgnoreCase);

        private static int CalculateMixedQueryFetchSize(int pageNumber, int pageSize)
        {
            var normalizedPageSize = Math.Clamp(pageSize, 1, MaxMixedQueryPageSize);
            var normalizedPageNumber = Math.Max(pageNumber, 1);

            var requested = (long)normalizedPageNumber * normalizedPageSize;
            if (requested >= MaxMixedQueryPageSize)
            {
                return MaxMixedQueryPageSize;
            }

            return (int)requested;
        }

        private static IOrderedEnumerable<ListCropTypesResponse> ApplySorting(
            IEnumerable<ListCropTypesResponse> rows,
            string? sortBy,
            string? sortDirection)
        {
            var isDesc = string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase);

            return NormalizeSortBy(sortBy) switch
            {
                "croptype" => isDesc
                    ? rows.OrderByDescending(x => x.CropType, StringComparer.OrdinalIgnoreCase)
                    : rows.OrderBy(x => x.CropType, StringComparer.OrdinalIgnoreCase),
                "createdat" => isDesc
                    ? rows.OrderByDescending(x => x.CreatedAt)
                    : rows.OrderBy(x => x.CreatedAt),
                "updatedat" => isDesc
                    ? rows.OrderByDescending(x => x.UpdatedAt ?? DateTimeOffset.MinValue)
                    : rows.OrderBy(x => x.UpdatedAt ?? DateTimeOffset.MinValue),
                "propertyname" => isDesc
                    ? rows.OrderByDescending(x => x.PropertyName, StringComparer.OrdinalIgnoreCase)
                    : rows.OrderBy(x => x.PropertyName, StringComparer.OrdinalIgnoreCase),
                "ownername" => isDesc
                    ? rows.OrderByDescending(x => x.OwnerName, StringComparer.OrdinalIgnoreCase)
                    : rows.OrderBy(x => x.OwnerName, StringComparer.OrdinalIgnoreCase),
                _ => isDesc
                    ? rows.OrderByDescending(x => x.CreatedAt)
                    : rows.OrderBy(x => x.CreatedAt)
            };
        }

        private static string NormalizeSortBy(string? sortBy)
            => string.IsNullOrWhiteSpace(sortBy)
                ? "createdat"
                : sortBy.Trim().ToLowerInvariant();
    }
}
