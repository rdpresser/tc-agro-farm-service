using System.Globalization;
using System.Linq.Expressions;
using TC.Agro.Farm.Application.UseCases.CropTypes.GetById;
using TC.Agro.Farm.Application.UseCases.CropTypes.List;

namespace TC.Agro.Farm.Infrastructure.Repositories
{
    public sealed class CropTypeCatalogReadStore : ICropTypeCatalogReadStore
    {
        private const string CatalogSource = "Catalog";

        private readonly ApplicationDbContext _dbContext;
        private readonly IUserContext _userContext;

        public CropTypeCatalogReadStore(ApplicationDbContext dbContext, IUserContext userContext)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
        }

        public async Task<GetCropTypeByIdResponse?> GetByIdAsync(
            Guid id,
            bool includeInactive = false,
            CancellationToken cancellationToken = default)
        {
            var catalog = await BuildCatalogQuery(includeInactive)
                .AsNoTracking()
                .Where(x => x.Id == id)
                .Select(CatalogProjection.Selector)
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);

            if (catalog is not null)
            {
                var propertyContext = await ResolvePropertyContextByOwnerAsync(
                    catalog.OwnerId,
                    includeInactive,
                    cancellationToken).ConfigureAwait(false);

                return ToGetByIdResponse(catalog, propertyContext);
            }

            var suggestion = await BuildSuggestionQuery(includeInactive)
                .AsNoTracking()
                .Where(x => x.Id == id)
                .Select(SuggestionProjection.Selector)
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);

            if (suggestion is null)
            {
                return null;
            }

            var resolvedCatalog = await ResolveCatalogForSuggestionAsync(
                suggestion.CropType,
                suggestion.OwnerId,
                includeInactive,
                cancellationToken).ConfigureAwait(false);

            return ToGetByIdResponse(suggestion, resolvedCatalog);
        }

        public async Task<(IReadOnlyList<ListCropTypesResponse> CropTypes, int TotalCount)> ListAsync(
            ListCropTypesQuery query,
            CancellationToken cancellationToken = default)
        {
            if (!string.IsNullOrWhiteSpace(query.Source) &&
                !string.Equals(query.Source, CatalogSource, StringComparison.OrdinalIgnoreCase))
            {
                return ([], 0);
            }

            var catalogQuery = BuildCatalogQuery(query.IncludeInactive, query.OwnerId)
                .AsNoTracking()
                .ApplyTextFilter(query.Filter);

            var totalCount = await catalogQuery
                .CountAsync(cancellationToken)
                .ConfigureAwait(false);

            var rows = await catalogQuery
                .ApplySorting(query.SortBy, query.SortDirection)
                .ApplyPagination(query.PageNumber, query.PageSize)
                .Select(CatalogProjection.Selector)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            var ownerIds = rows
                .Where(catalog => catalog.OwnerId.HasValue && catalog.OwnerId.Value != Guid.Empty)
                .Select(catalog => catalog.OwnerId!.Value)
                .Distinct()
                .ToArray();

            var propertyContexts = await ResolvePropertyContextByOwnersAsync(
                ownerIds,
                query.IncludeInactive,
                cancellationToken).ConfigureAwait(false);

            return (
                rows
                    .Select(catalog =>
                    {
                        propertyContexts.TryGetValue(catalog.OwnerId ?? Guid.Empty, out var propertyContext);
                        return ToListResponse(catalog, propertyContext);
                    })
                    .ToList(),
                totalCount);
        }

        private IQueryable<CropTypeCatalogAggregate> BuildCatalogQuery(bool includeInactive, Guid? ownerId = null)
        {
            var query = includeInactive
                ? _dbContext.CropTypeCatalogs.IgnoreQueryFilters()
                : _dbContext.CropTypeCatalogs;

            if (_userContext.IsAdmin)
            {
                if (ownerId.HasValue && ownerId.Value != Guid.Empty)
                {
                    return query.Where(x => x.OwnerId == null || x.OwnerId == ownerId.Value);
                }

                return query;
            }

            return query.Where(x => x.OwnerId == null || x.OwnerId == _userContext.Id);
        }

        private IQueryable<CropTypeSuggestionAggregate> BuildSuggestionQuery(bool includeInactive, Guid? ownerId = null)
        {
            var query = includeInactive
                ? _dbContext.CropTypeSuggestions.IgnoreQueryFilters()
                : _dbContext.CropTypeSuggestions;

            if (_userContext.IsAdmin)
            {
                if (ownerId.HasValue && ownerId.Value != Guid.Empty)
                {
                    return query.Where(x => x.OwnerId == ownerId.Value);
                }

                return query;
            }

            return query.Where(x => x.OwnerId == _userContext.Id);
        }

        private IQueryable<PropertyAggregate> BuildPropertyQuery(bool includeInactive, Guid? ownerId = null)
        {
            var query = includeInactive
                ? _dbContext.Properties.IgnoreQueryFilters()
                : _dbContext.Properties;

            if (_userContext.IsAdmin)
            {
                if (ownerId.HasValue && ownerId.Value != Guid.Empty)
                {
                    return query.Where(x => x.OwnerId == ownerId.Value);
                }

                return query;
            }

            return query.Where(x => x.OwnerId == _userContext.Id);
        }

        private async Task<PropertyProjection?> ResolvePropertyContextByOwnerAsync(
            Guid? ownerId,
            bool includeInactive,
            CancellationToken cancellationToken)
        {
            if (!ownerId.HasValue || ownerId.Value == Guid.Empty)
            {
                return null;
            }

            return await BuildPropertyQuery(includeInactive, ownerId.Value)
                .AsNoTracking()
                .OrderBy(x => x.Name.Value)
                .Select(x => new PropertyProjection(x.Id, x.OwnerId, x.Name.Value))
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);
        }

        private async Task<Dictionary<Guid, PropertyProjection>> ResolvePropertyContextByOwnersAsync(
            IReadOnlyCollection<Guid> ownerIds,
            bool includeInactive,
            CancellationToken cancellationToken)
        {
            if (ownerIds.Count == 0)
            {
                return new Dictionary<Guid, PropertyProjection>();
            }

            var ownerIdsArray = ownerIds
                .Where(ownerId => ownerId != Guid.Empty)
                .Distinct()
                .ToArray();

            if (ownerIdsArray.Length == 0)
            {
                return new Dictionary<Guid, PropertyProjection>();
            }

            var properties = await BuildPropertyQuery(includeInactive)
                .AsNoTracking()
                .Where(x => ownerIdsArray.Contains(x.OwnerId))
                .OrderBy(x => x.Name.Value)
                .ThenBy(x => x.Id)
                .Select(x => new PropertyProjection(x.Id, x.OwnerId, x.Name.Value))
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            return properties
                .GroupBy(x => x.OwnerId)
                .ToDictionary(group => group.Key, group => group.First());
        }

        private async Task<CatalogProjection?> ResolveCatalogForSuggestionAsync(
            string cropType,
            Guid ownerId,
            bool includeInactive,
            CancellationToken cancellationToken)
        {
            var normalizedCropType = NormalizeCropType(cropType);

            return await BuildCatalogQuery(includeInactive, ownerId)
                .AsNoTracking()
                .Where(x => x.CropTypeName.Value.ToLower() == normalizedCropType)
                .OrderByDescending(x => x.OwnerId == ownerId)
                .ThenBy(x => x.IsSystemDefined)
                .Select(CatalogProjection.Selector)
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);
        }

        private static string NormalizeCropType(string cropType)
            => string.IsNullOrWhiteSpace(cropType)
                ? string.Empty
                : cropType.Trim().ToLowerInvariant();

        private static string? BuildPlantingWindow(CatalogProjection catalog)
        {
            if (!catalog.TypicalPlantingStartMonth.HasValue || !catalog.TypicalPlantingEndMonth.HasValue)
            {
                return null;
            }

            var dateTimeFormat = CultureInfo.InvariantCulture.DateTimeFormat;
            var start = dateTimeFormat.GetAbbreviatedMonthName(catalog.TypicalPlantingStartMonth.Value);
            var end = dateTimeFormat.GetAbbreviatedMonthName(catalog.TypicalPlantingEndMonth.Value);

            return string.Equals(start, end, StringComparison.OrdinalIgnoreCase)
                ? start
                : $"{start} to {end}";
        }

        private static ListCropTypesResponse ToListResponse(
            CatalogProjection catalog,
            PropertyProjection? propertyContext)
            => new(
                Id: catalog.Id,
                PropertyId: propertyContext?.Id ?? Guid.Empty,
                OwnerId: propertyContext?.OwnerId ?? catalog.OwnerId ?? Guid.Empty,
                PropertyName: propertyContext?.PropertyName ?? string.Empty,
                OwnerName: catalog.OwnerName ?? "System",
                CropType: catalog.CropType,
                SuggestedImage: catalog.SuggestedImage,
                Source: CatalogSource,
                IsOverride: false,
                IsStale: false,
                ConfidenceScore: null,
                PlantingWindow: BuildPlantingWindow(catalog),
                HarvestCycleMonths: catalog.TypicalHarvestCycleMonths,
                SuggestedIrrigationType: catalog.RecommendedIrrigationType,
                MinSoilMoisture: catalog.MinSoilMoisture,
                MaxTemperature: catalog.MaxTemperature,
                MinHumidity: catalog.MinHumidity,
                Notes: catalog.Description,
                Model: null,
                GeneratedAt: null,
                IsActive: catalog.IsActive,
                CreatedAt: catalog.CreatedAt,
                UpdatedAt: catalog.UpdatedAt,
                CropTypeCatalogId: catalog.Id,
                SelectedCropTypeSuggestionId: null);

        private static GetCropTypeByIdResponse ToGetByIdResponse(
            CatalogProjection catalog,
            PropertyProjection? propertyContext)
            => new(
                Id: catalog.Id,
                PropertyId: propertyContext?.Id ?? Guid.Empty,
                OwnerId: propertyContext?.OwnerId ?? catalog.OwnerId ?? Guid.Empty,
                PropertyName: propertyContext?.PropertyName ?? string.Empty,
                OwnerName: catalog.OwnerName ?? "System",
                CropType: catalog.CropType,
                SuggestedImage: catalog.SuggestedImage,
                Source: CatalogSource,
                IsOverride: false,
                IsStale: false,
                ConfidenceScore: null,
                PlantingWindow: BuildPlantingWindow(catalog),
                HarvestCycleMonths: catalog.TypicalHarvestCycleMonths,
                SuggestedIrrigationType: catalog.RecommendedIrrigationType,
                MinSoilMoisture: catalog.MinSoilMoisture,
                MaxTemperature: catalog.MaxTemperature,
                MinHumidity: catalog.MinHumidity,
                Notes: catalog.Description,
                Model: null,
                GeneratedAt: null,
                IsActive: catalog.IsActive,
                CreatedAt: catalog.CreatedAt,
                UpdatedAt: catalog.UpdatedAt,
                CropTypeCatalogId: catalog.Id,
                SelectedCropTypeSuggestionId: null);

        private static GetCropTypeByIdResponse ToGetByIdResponse(
            SuggestionProjection suggestion,
            CatalogProjection? catalog)
            => new(
                Id: suggestion.Id,
                PropertyId: suggestion.PropertyId,
                OwnerId: suggestion.OwnerId,
                PropertyName: suggestion.PropertyName,
                OwnerName: suggestion.OwnerName,
                CropType: catalog?.CropType ?? suggestion.CropType,
                SuggestedImage: suggestion.SuggestedImage ?? catalog?.SuggestedImage,
                Source: suggestion.Source,
                IsOverride: suggestion.IsOverride,
                IsStale: suggestion.IsStale,
                ConfidenceScore: suggestion.ConfidenceScore,
                PlantingWindow: suggestion.PlantingWindow ?? (catalog is null ? null : BuildPlantingWindow(catalog)),
                HarvestCycleMonths: suggestion.HarvestCycleMonths ?? catalog?.TypicalHarvestCycleMonths,
                SuggestedIrrigationType: suggestion.SuggestedIrrigationType ?? catalog?.RecommendedIrrigationType,
                MinSoilMoisture: suggestion.MinSoilMoisture ?? catalog?.MinSoilMoisture,
                MaxTemperature: suggestion.MaxTemperature ?? catalog?.MaxTemperature,
                MinHumidity: suggestion.MinHumidity ?? catalog?.MinHumidity,
                Notes: suggestion.Notes ?? catalog?.Description,
                Model: suggestion.Model,
                GeneratedAt: suggestion.GeneratedAt,
                IsActive: suggestion.IsActive,
                CreatedAt: suggestion.CreatedAt,
                UpdatedAt: suggestion.UpdatedAt,
                CropTypeCatalogId: catalog?.Id ?? Guid.Empty,
                SelectedCropTypeSuggestionId: suggestion.Id);

        private sealed record PropertyProjection(
            Guid Id,
            Guid OwnerId,
            string PropertyName);

        private sealed record CatalogProjection(
            Guid Id,
            Guid? OwnerId,
            string? OwnerName,
            string CropType,
            string? SuggestedImage,
            string? Description,
            int? TypicalPlantingStartMonth,
            int? TypicalPlantingEndMonth,
            int? TypicalHarvestCycleMonths,
            string? RecommendedIrrigationType,
            double? MinSoilMoisture,
            double? MaxTemperature,
            double? MinHumidity,
            bool IsActive,
            DateTimeOffset CreatedAt,
            DateTimeOffset? UpdatedAt)
        {
            public static Expression<Func<CropTypeCatalogAggregate, CatalogProjection>> Selector => catalog => new CatalogProjection(
                catalog.Id,
                catalog.OwnerId,
                catalog.Owner != null ? catalog.Owner.Name : null,
                catalog.CropTypeName.Value,
                catalog.SuggestedImage,
                catalog.Description,
                catalog.TypicalPlantingStartMonth,
                catalog.TypicalPlantingEndMonth,
                catalog.TypicalHarvestCycleMonths,
                catalog.RecommendedIrrigationType,
                catalog.MinSoilMoisture,
                catalog.MaxTemperature,
                catalog.MinHumidity,
                catalog.IsActive,
                catalog.CreatedAt,
                catalog.UpdatedAt);
        }

        private sealed record SuggestionProjection(
            Guid Id,
            Guid PropertyId,
            Guid OwnerId,
            string PropertyName,
            string OwnerName,
            string CropType,
            string? SuggestedImage,
            string Source,
            bool IsOverride,
            bool IsStale,
            double? ConfidenceScore,
            string? PlantingWindow,
            int? HarvestCycleMonths,
            string? SuggestedIrrigationType,
            double? MinSoilMoisture,
            double? MaxTemperature,
            double? MinHumidity,
            string? Notes,
            string? Model,
            DateTimeOffset? GeneratedAt,
            bool IsActive,
            DateTimeOffset CreatedAt,
            DateTimeOffset? UpdatedAt)
        {
            public static Expression<Func<CropTypeSuggestionAggregate, SuggestionProjection>> Selector => suggestion => new SuggestionProjection(
                suggestion.Id,
                suggestion.PropertyId,
                suggestion.OwnerId,
                suggestion.Property.Name.Value,
                suggestion.Owner.Name,
                suggestion.CropName.Value,
                suggestion.SuggestedImage,
                suggestion.Source,
                suggestion.IsOverride,
                suggestion.IsStale,
                suggestion.ConfidenceScore,
                suggestion.PlantingWindow,
                suggestion.HarvestCycleMonths,
                suggestion.SuggestedIrrigationType,
                suggestion.MinSoilMoisture,
                suggestion.MaxTemperature,
                suggestion.MinHumidity,
                suggestion.Notes,
                suggestion.Model,
                suggestion.GeneratedAt,
                suggestion.IsActive,
                suggestion.CreatedAt,
                suggestion.UpdatedAt);
        }
    }

    internal static class CropTypeCatalogReadStoreListExtensions
    {
        public static List<ListCropTypesResponse> ApplyTextFilter(this List<ListCropTypesResponse> rows, string? filter)
        {
            if (string.IsNullOrWhiteSpace(filter))
            {
                return rows;
            }

            var normalizedFilter = filter.Trim();

            return rows.Where(row =>
                    row.CropType.Contains(normalizedFilter, StringComparison.OrdinalIgnoreCase) ||
                    row.PropertyName.Contains(normalizedFilter, StringComparison.OrdinalIgnoreCase) ||
                    row.OwnerName.Contains(normalizedFilter, StringComparison.OrdinalIgnoreCase) ||
                    row.Source.Contains(normalizedFilter, StringComparison.OrdinalIgnoreCase) ||
                    (!string.IsNullOrWhiteSpace(row.Notes) && row.Notes.Contains(normalizedFilter, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrWhiteSpace(row.SuggestedIrrigationType) && row.SuggestedIrrigationType.Contains(normalizedFilter, StringComparison.OrdinalIgnoreCase)))
                .ToList();
        }

        public static IEnumerable<ListCropTypesResponse> ApplySorting(
            this IEnumerable<ListCropTypesResponse> rows,
            string? sortBy,
            string? sortDirection)
        {
            var isAscending = string.Equals(sortDirection, "asc", StringComparison.OrdinalIgnoreCase);

            return (sortBy ?? string.Empty).Trim().ToLowerInvariant() switch
            {
                "croptype" => isAscending
                    ? rows.OrderBy(row => row.CropType)
                    : rows.OrderByDescending(row => row.CropType),
                "propertyname" => isAscending
                    ? rows.OrderBy(row => row.PropertyName)
                    : rows.OrderByDescending(row => row.PropertyName),
                "confidence" => isAscending
                    ? rows.OrderBy(row => row.ConfidenceScore)
                    : rows.OrderByDescending(row => row.ConfidenceScore),
                "source" => isAscending
                    ? rows.OrderBy(row => row.Source)
                    : rows.OrderByDescending(row => row.Source),
                "isstale" => isAscending
                    ? rows.OrderBy(row => row.IsStale)
                    : rows.OrderByDescending(row => row.IsStale),
                "generatedat" => isAscending
                    ? rows.OrderBy(row => row.GeneratedAt)
                    : rows.OrderByDescending(row => row.GeneratedAt),
                "updatedat" => isAscending
                    ? rows.OrderBy(row => row.UpdatedAt)
                    : rows.OrderByDescending(row => row.UpdatedAt),
                _ => isAscending
                    ? rows.OrderBy(row => row.CreatedAt)
                    : rows.OrderByDescending(row => row.CreatedAt)
            };
        }
    }
}
