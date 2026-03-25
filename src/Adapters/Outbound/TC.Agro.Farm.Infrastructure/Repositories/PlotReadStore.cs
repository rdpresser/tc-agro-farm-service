using TC.Agro.Farm.Application.UseCases.Plots.ListAll;

namespace TC.Agro.Farm.Infrastructure.Repositories
{
    public sealed class PlotReadStore : IPlotReadStore
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IUserContext _userContext;

        public PlotReadStore(ApplicationDbContext dbContext, IUserContext userContext)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
        }

        private IQueryable<PlotAggregate> FilteredDbSet => _userContext.IsAdmin
            ? _dbContext.Plots
            : _dbContext.Plots.Where(x => x.OwnerId == _userContext.Id);

        private IQueryable<PlotReadProjection> BuildProjectionQuery()
        {
            var cropCycles = _dbContext.CropCycles.AsNoTracking();

            return FilteredDbSet
                .AsNoTracking()
                .GroupJoin(
                    cropCycles,
                    p => p.Id,
                    c => c.PlotId,
                    (p, cycles) => new
                    {
                        Plot = p,
                        FallbackCropType = p.CropTypeCatalog == null ? string.Empty : p.CropTypeCatalog.CropTypeName.Value,
                        FallbackAdditionalNotes = p.AdditionalNotes != null ? p.AdditionalNotes.Value : null,
                        LatestCycle = cycles
                            .OrderByDescending(c => c.EndedAt == null)
                            .ThenByDescending(c => c.StartedAt)
                            .Select(c => new
                            {
                                CropType = c.CropTypeCatalog.CropTypeName.Value,
                                StartedAt = (DateTimeOffset?)c.StartedAt,
                                c.ExpectedHarvestDate,
                                IrrigationType = c.IrrigationType.Value,
                                c.Notes,
                                CropTypeCatalogId = (Guid?)c.CropTypeCatalogId,
                                c.SelectedCropTypeSuggestionId
                            })
                            .FirstOrDefault()
                    })
                .Select(x => new PlotReadProjection(
                    x.Plot.Id,
                    x.Plot.PropertyId,
                    x.Plot.OwnerId,
                    x.Plot.Owner.Name,
                    x.Plot.Property.Name.Value,
                    x.Plot.Name.Value,
                    x.LatestCycle != null ? x.LatestCycle.CropType : x.FallbackCropType,
                    x.Plot.AreaHectares.Hectares,
                    x.Plot.Latitude ?? x.Plot.Property.Location.Latitude,
                    x.Plot.Longitude ?? x.Plot.Property.Location.Longitude,
                    x.Plot.BoundaryGeoJson,
                    x.Plot.IsActive,
                    x.Plot.Sensors.Count,
                    x.Plot.CreatedAt,
                    x.Plot.UpdatedAt,
                    x.LatestCycle != null && x.LatestCycle.StartedAt.HasValue
                        ? x.LatestCycle.StartedAt.Value
                        : x.Plot.PlantingDate,
                    x.LatestCycle != null && x.LatestCycle.ExpectedHarvestDate.HasValue
                        ? x.LatestCycle.ExpectedHarvestDate.Value
                        : x.Plot.ExpectedHarvestDate,
                    x.LatestCycle != null
                        ? x.LatestCycle.IrrigationType
                        : x.Plot.IrrigationType.Value,
                    x.LatestCycle != null ? x.LatestCycle.Notes : x.FallbackAdditionalNotes,
                    x.LatestCycle != null && x.LatestCycle.CropTypeCatalogId.HasValue
                        ? x.LatestCycle.CropTypeCatalogId.Value
                        : x.Plot.CropTypeCatalogId,
                    x.LatestCycle != null
                        ? x.LatestCycle.SelectedCropTypeSuggestionId
                        : x.Plot.SelectedCropTypeSuggestionId));
        }

        private static IQueryable<PlotReadProjection> ApplyTextFilter(
            IQueryable<PlotReadProjection> query,
            string? filter)
        {
            if (string.IsNullOrWhiteSpace(filter))
            {
                return query;
            }

            var pattern = $"%{filter.Trim()}%";
            return query.Where(p =>
                EF.Functions.ILike(p.Name, pattern) ||
                EF.Functions.ILike(p.PropertyName, pattern) ||
                EF.Functions.ILike(p.CropType, pattern));
        }

        private static IQueryable<PlotReadProjection> ApplySorting(
            IQueryable<PlotReadProjection> query,
            string? sortBy,
            string? sortDirection)
        {
            if (string.IsNullOrWhiteSpace(sortBy))
            {
                return query.OrderByDescending(p => p.CreatedAt);
            }

            var isAscending = string.Equals(sortDirection, "asc", StringComparison.OrdinalIgnoreCase);

            return sortBy.ToLowerInvariant() switch
            {
                "name" => isAscending ? query.OrderBy(p => p.Name) : query.OrderByDescending(p => p.Name),
                "croptype" => isAscending ? query.OrderBy(p => p.CropType) : query.OrderByDescending(p => p.CropType),
                "areahectares" => isAscending ? query.OrderBy(p => p.AreaHectares) : query.OrderByDescending(p => p.AreaHectares),
                "createdat" => isAscending ? query.OrderBy(p => p.CreatedAt) : query.OrderByDescending(p => p.CreatedAt),
                "propertyname" => isAscending ? query.OrderBy(p => p.PropertyName) : query.OrderByDescending(p => p.PropertyName),
                "sensorscount" => isAscending ? query.OrderBy(p => p.SensorCount) : query.OrderByDescending(p => p.SensorCount),
                _ => query.OrderByDescending(p => p.CreatedAt)
            };
        }

        /// <inheritdoc />
        public async Task<GetPlotByIdResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var plot = await BuildProjectionQuery()
                .Where(p => p.Id == id)
                .Select(p => new GetPlotByIdResponse(
                    p.Id,
                    p.PropertyId,
                    p.OwnerId,
                    p.OwnerName,
                    p.PropertyName,
                    p.Name,
                    p.CropType,
                    p.AreaHectares,
                    p.Latitude,
                    p.Longitude,
                    p.BoundaryGeoJson,
                    p.IsActive,
                    p.SensorCount,
                    p.CreatedAt,
                    p.UpdatedAt,
                    p.PlantingDate,
                    p.ExpectedHarvestDate,
                    p.IrrigationType,
                    p.AdditionalNotes,
                    p.CropTypeCatalogId,
                    p.SelectedCropTypeSuggestionId))
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);

            return plot;
        }

        /// <inheritdoc />
        public async Task<(IReadOnlyList<ListPlotsFromPropertyResponse> Plots, int TotalCount)> ListPlotsFromPropertyAsync(
            ListPlotsFromPropertyQuery query,
            CancellationToken cancellationToken = default)
        {
            var plotsQuery = BuildProjectionQuery()
                .Where(p => p.PropertyId == query.Id);

            if (query.CropTypeCatalogId.HasValue && query.CropTypeCatalogId.Value != Guid.Empty)
            {
                plotsQuery = plotsQuery.Where(p => p.CropTypeCatalogId == query.CropTypeCatalogId.Value);
            }

            // Apply optional crop type filter (using index on crop_type)
            if (!string.IsNullOrWhiteSpace(query.CropType))
            {
                plotsQuery = plotsQuery.Where(p => EF.Functions.ILike(p.CropType, $"%{query.CropType.Trim()}%"));
            }

            plotsQuery = ApplyTextFilter(plotsQuery, query.Filter);

            var totalCount = await plotsQuery
                .CountAsync(cancellationToken)
                .ConfigureAwait(false);

            plotsQuery = ApplySorting(plotsQuery, query.SortBy, query.SortDirection);

            var plots = await plotsQuery
                .ApplyPagination(query.PageNumber, query.PageSize)
                .Select(p => new ListPlotsFromPropertyResponse(
                    p.Id,
                    p.PropertyId,
                    p.OwnerId,
                    p.OwnerName,
                    p.PropertyName,
                    p.Name,
                    p.CropType,
                    p.AreaHectares,
                    p.Latitude,
                    p.Longitude,
                    p.IsActive,
                    p.SensorCount,
                    p.CreatedAt,
                    p.PlantingDate,
                    p.ExpectedHarvestDate,
                    p.IrrigationType,
                    p.AdditionalNotes,
                    p.CropTypeCatalogId,
                    p.SelectedCropTypeSuggestionId))
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            return ([.. plots], totalCount);
        }

        /// <inheritdoc />
        public async Task<(IReadOnlyList<ListPlotsResponse> Plots, int TotalCount)> ListPlotsAsync(
            ListPlotsQuery query,
            CancellationToken cancellationToken = default)
        {
            var plotsQuery = BuildProjectionQuery();

            if (_userContext.IsAdmin && query.OwnerId is not null && query.OwnerId.HasValue && query.OwnerId.Value != Guid.Empty)
            {
                plotsQuery = plotsQuery.Where(x => x.OwnerId == query.OwnerId);
            }

            if (query.PropertyId is not null && query.PropertyId.HasValue && query.PropertyId.Value != Guid.Empty)
            {
                plotsQuery = plotsQuery.Where(p => p.PropertyId == query.PropertyId.Value);
            }

            if (query.CropTypeCatalogId.HasValue && query.CropTypeCatalogId.Value != Guid.Empty)
            {
                plotsQuery = plotsQuery.Where(p => p.CropTypeCatalogId == query.CropTypeCatalogId.Value);
            }

            // Apply optional crop type filter (using index on crop_type)
            if (!string.IsNullOrWhiteSpace(query.CropType))
            {
                plotsQuery = plotsQuery.Where(p => EF.Functions.ILike(p.CropType, $"%{query.CropType.Trim()}%"));
            }

            plotsQuery = ApplyTextFilter(plotsQuery, query.Filter);

            var totalCount = await plotsQuery
                .CountAsync(cancellationToken)
                .ConfigureAwait(false);

            plotsQuery = ApplySorting(plotsQuery, query.SortBy, query.SortDirection);

            var plots = await plotsQuery
                .ApplyPagination(query.PageNumber, query.PageSize)
                .Select(p => new ListPlotsResponse(
                    p.Id,
                    p.PropertyId,
                    p.OwnerId,
                    p.OwnerName,
                    p.PropertyName,
                    p.Name,
                    p.CropType,
                    p.AreaHectares,
                    p.Latitude,
                    p.Longitude,
                    p.IsActive,
                    p.SensorCount,
                    p.CreatedAt,
                    p.PlantingDate,
                    p.ExpectedHarvestDate,
                    p.IrrigationType,
                    p.AdditionalNotes,
                    p.CropTypeCatalogId,
                    p.SelectedCropTypeSuggestionId))
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            return ([.. plots], totalCount);
        }

        private sealed record PlotReadProjection(
            Guid Id,
            Guid PropertyId,
            Guid OwnerId,
            string OwnerName,
            string PropertyName,
            string Name,
            string CropType,
            double AreaHectares,
            double? Latitude,
            double? Longitude,
            string? BoundaryGeoJson,
            bool IsActive,
            int SensorCount,
            DateTimeOffset CreatedAt,
            DateTimeOffset? UpdatedAt,
            DateTimeOffset PlantingDate,
            DateTimeOffset ExpectedHarvestDate,
            string IrrigationType,
            string? AdditionalNotes,
            Guid CropTypeCatalogId,
            Guid? SelectedCropTypeSuggestionId);
    }
}
