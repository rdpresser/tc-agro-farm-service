using TC.Agro.Farm.Application.UseCases.CropTypes.List;

namespace TC.Agro.Farm.Application.UseCases.CropTypes.Options
{
    internal sealed class ListCropTypeOptionsQueryHandler : BaseQueryHandler<ListCropTypeOptionsQuery, IReadOnlyList<CropTypeOptionResponse>>
    {
        private readonly ICropTypeCatalogReadStore _readStore;
        private readonly ILogger<ListCropTypeOptionsQueryHandler> _logger;

        public ListCropTypeOptionsQueryHandler(
            ICropTypeCatalogReadStore readStore,
            IUserContext userContext,
            ILogger<ListCropTypeOptionsQueryHandler> logger)
        {
            _readStore = readStore ?? throw new ArgumentNullException(nameof(readStore));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public override async Task<Result<IReadOnlyList<CropTypeOptionResponse>>> ExecuteAsync(
            ListCropTypeOptionsQuery query,
            CancellationToken ct = default)
        {
            _logger.LogDebug(
                "Listing crop type options. OwnerId={OwnerId}, PropertyId={PropertyId}, IncludeStale={IncludeStale}, IncludeInactive={IncludeInactive}, Filter={Filter}, Limit={Limit}",
                query.OwnerId,
                query.PropertyId,
                query.IncludeStale,
                query.IncludeInactive,
                query.Filter,
                query.Limit);

            var listQuery = new ListCropTypesQuery
            {
                OwnerId = query.OwnerId,
                PropertyId = query.PropertyId,
                Source = query.Source,
                IncludeStale = query.IncludeStale,
                IncludeInactive = query.IncludeInactive,
                Filter = query.Filter,
                PageNumber = 1,
                PageSize = Math.Clamp(query.Limit, 1, 500),
                SortBy = "cropType",
                SortDirection = "asc"
            };

            var (rows, _) = await _readStore
                .ListAsync(listQuery, ct)
                .ConfigureAwait(false);

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
    }
}
