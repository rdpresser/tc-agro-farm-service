using TC.Agro.Farm.Application.UseCases.CropTypes.List;

namespace TC.Agro.Farm.Application.UseCases.CropTypes.Options
{
    internal sealed class ListCropTypeOptionsQueryHandler : BaseQueryHandler<ListCropTypeOptionsQuery, IReadOnlyList<CropTypeOptionResponse>>
    {
        private const string CatalogSource = "Catalog";

        private readonly ICropTypeCatalogReadStore _catalogReadStore;
        private readonly ICropTypeSuggestionReadStore _suggestionReadStore;
        private readonly ILogger<ListCropTypeOptionsQueryHandler> _logger;

        public ListCropTypeOptionsQueryHandler(
            ICropTypeCatalogReadStore catalogReadStore,
            ICropTypeSuggestionReadStore suggestionReadStore,
            IUserContext userContext,
            ILogger<ListCropTypeOptionsQueryHandler> logger)
        {
            _catalogReadStore = catalogReadStore ?? throw new ArgumentNullException(nameof(catalogReadStore));
            _suggestionReadStore = suggestionReadStore ?? throw new ArgumentNullException(nameof(suggestionReadStore));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public override async Task<Result<IReadOnlyList<CropTypeOptionResponse>>> ExecuteAsync(
            ListCropTypeOptionsQuery query,
            CancellationToken ct = default)
        {
            _logger.LogDebug(
                "Listing crop type options. OwnerId={OwnerId}, IncludeSuggestions={IncludeSuggestions}, IncludeStale={IncludeStale}, IncludeInactive={IncludeInactive}, Filter={Filter}, Limit={Limit}",
                query.OwnerId,
                query.IncludeSuggestions,
                query.IncludeStale,
                query.IncludeInactive,
                query.Filter,
                query.Limit);

            var listQuery = new ListCropTypesQuery
            {
                OwnerId = query.OwnerId,
                Source = query.Source,
                IncludeSuggestions = query.IncludeSuggestions,
                IncludeStale = query.IncludeStale,
                IncludeInactive = query.IncludeInactive,
                Filter = query.Filter,
                PageNumber = 1,
                PageSize = Math.Clamp(query.Limit, 1, 500),
                SortBy = "cropType",
                SortDirection = "asc"
            };

            IReadOnlyList<ListCropTypesResponse> rows;

            if (query.IncludeSuggestions)
            {
                var includeCatalog = ShouldIncludeCatalog(query.Source);
                var includeSuggestions = ShouldIncludeSuggestions(query.Source);

                var catalogTask = includeCatalog
                    ? _catalogReadStore.ListAsync(listQuery, ct)
                    : Task.FromResult<(IReadOnlyList<ListCropTypesResponse> CropTypes, int TotalCount)>(([], 0));

                var suggestionTask = includeSuggestions
                    ? _suggestionReadStore.ListAsync(listQuery, ct)
                    : Task.FromResult<(IReadOnlyList<ListCropTypesResponse> CropTypes, int TotalCount)>(([], 0));

                await Task.WhenAll(catalogTask, suggestionTask).ConfigureAwait(false);

                var (catalogRows, _) = await catalogTask.ConfigureAwait(false);
                var (suggestionRows, _) = await suggestionTask.ConfigureAwait(false);

                rows = catalogRows
                    .Concat(suggestionRows)
                    .OrderBy(x => x.CropType, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(x => x.Source, StringComparer.OrdinalIgnoreCase)
                    .Take(listQuery.PageSize)
                    .ToList();
            }
            else
            {
                (rows, _) = await _catalogReadStore
                    .ListAsync(listQuery, ct)
                    .ConfigureAwait(false);
            }

            var options = rows
                .Select(row => new CropTypeOptionResponse(
                    Id: row.Id,
                    CropType: row.CropType,
                    SuggestedImage: row.SuggestedImage,
                    Source: row.Source,
                    IsStale: row.IsStale,
                    IsActive: row.IsActive,
                    CropTypeCatalogId: row.CropTypeCatalogId,
                    SelectedCropTypeSuggestionId: row.SelectedCropTypeSuggestionId,
                    PlantingWindow: row.PlantingWindow,
                    HarvestCycleMonths: row.HarvestCycleMonths,
                    SuggestedIrrigationType: row.SuggestedIrrigationType,
                    MinSoilMoisture: row.MinSoilMoisture,
                    MaxTemperature: row.MaxTemperature,
                    MinHumidity: row.MinHumidity))
                .ToList();

            return Result.Success<IReadOnlyList<CropTypeOptionResponse>>(options);
        }

        private static bool ShouldIncludeCatalog(string? source)
            => string.IsNullOrWhiteSpace(source)
               || string.Equals(source, CatalogSource, StringComparison.OrdinalIgnoreCase);

        private static bool ShouldIncludeSuggestions(string? source)
            => string.IsNullOrWhiteSpace(source)
               || !string.Equals(source, CatalogSource, StringComparison.OrdinalIgnoreCase);
    }
}
