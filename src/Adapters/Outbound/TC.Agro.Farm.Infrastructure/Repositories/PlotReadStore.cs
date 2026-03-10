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

        /// <inheritdoc />
        public async Task<GetPlotByIdResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var plot = await FilteredDbSet
                .AsNoTracking()
                .Where(p => p.Id == id)
                .Select(p => new GetPlotByIdResponse(
                    p.Id,
                    p.PropertyId,
                    p.OwnerId,
                    p.Owner.Name,
                    p.Property.Name.Value,
                    p.Name.Value,
                    p.CropTypeCatalog == null ? string.Empty : p.CropTypeCatalog.CropTypeName.Value,
                    p.AreaHectares.Hectares,
                    p.Latitude ?? p.Property.Location.Latitude,
                    p.Longitude ?? p.Property.Location.Longitude,
                    p.BoundaryGeoJson,
                    p.IsActive,
                    p.Sensors.Count,
                    p.CreatedAt,
                    p.UpdatedAt,
                    p.PlantingDate,
                    p.ExpectedHarvestDate,
                    p.IrrigationType.Value,
                    p.AdditionalNotes != null ? p.AdditionalNotes.Value : null,
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
            var plotsQuery = FilteredDbSet
                .AsNoTracking()
                .Where(p => p.PropertyId == query.Id);

            if (query.CropTypeCatalogId.HasValue && query.CropTypeCatalogId.Value != Guid.Empty)
            {
                plotsQuery = plotsQuery.Where(p => p.CropTypeCatalogId == query.CropTypeCatalogId.Value);
            }

            // Apply optional crop type filter (using index on crop_type)
            if (!string.IsNullOrWhiteSpace(query.CropType))
            {
                plotsQuery = plotsQuery.Where(p => EF.Functions.ILike(
                    p.CropTypeCatalog == null ? string.Empty : p.CropTypeCatalog.CropTypeName.Value,
                    query.CropType));
            }

            // Apply text filter (searches name and crop type)
            plotsQuery = plotsQuery.ApplyTextFilter(query.Filter);

            // Get total count before pagination
            var totalCount = await plotsQuery
                .CountAsync(cancellationToken)
                .ConfigureAwait(false);

            // Apply sorting, pagination, and projection in one go
            var plots = await plotsQuery
                .ApplySorting(query.SortBy, query.SortDirection)
                .ApplyPagination(query.PageNumber, query.PageSize)
                .Select(p => new ListPlotsFromPropertyResponse(
                    p.Id,
                    p.PropertyId,
                    p.OwnerId,
                    p.Owner.Name,
                    p.Property.Name.Value,
                    p.Name.Value,
                    p.CropTypeCatalog == null ? string.Empty : p.CropTypeCatalog.CropTypeName.Value,
                    p.AreaHectares.Hectares,
                    p.Latitude ?? p.Property.Location.Latitude,
                    p.Longitude ?? p.Property.Location.Longitude,
                    p.IsActive,
                    p.Sensors.Count,
                    p.CreatedAt,
                    p.PlantingDate,
                    p.ExpectedHarvestDate,
                    p.IrrigationType.Value,
                    p.AdditionalNotes != null ? p.AdditionalNotes.Value : null,
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
            var plotsQuery = FilteredDbSet
                .AsNoTracking();

            if (_userContext.IsAdmin && query.OwnerId is not null && query.OwnerId.HasValue && query.OwnerId.Value != Guid.Empty)
            {
                //when loggedin as admin on frontend
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
                plotsQuery = plotsQuery.Where(p => EF.Functions.ILike(
                    p.CropTypeCatalog == null ? string.Empty : p.CropTypeCatalog.CropTypeName.Value,
                    query.CropType));
            }

            // Apply text filter (searches name and crop type)
            plotsQuery = plotsQuery.ApplyTextFilter(query.Filter);

            // Get total count before pagination
            var totalCount = await plotsQuery
                .CountAsync(cancellationToken)
                .ConfigureAwait(false);

            // Apply sorting, pagination, and projection in one go
            var plots = await plotsQuery
                .ApplySorting(query.SortBy, query.SortDirection)
                .ApplyPagination(query.PageNumber, query.PageSize)
                .Select(p => new ListPlotsResponse(
                    p.Id,
                    p.PropertyId,
                    p.OwnerId,
                    p.Owner.Name,
                    p.Property.Name.Value,
                    p.Name.Value,
                    p.CropTypeCatalog == null ? string.Empty : p.CropTypeCatalog.CropTypeName.Value,
                    p.AreaHectares.Hectares,
                    p.Latitude ?? p.Property.Location.Latitude,
                    p.Longitude ?? p.Property.Location.Longitude,
                    p.IsActive,
                    p.Sensors.Count,
                    p.CreatedAt,
                    p.PlantingDate,
                    p.ExpectedHarvestDate,
                    p.IrrigationType.Value,
                    p.AdditionalNotes != null ? p.AdditionalNotes.Value : null,
                    p.CropTypeCatalogId,
                    p.SelectedCropTypeSuggestionId))
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            return ([.. plots], totalCount);
        }
    }
}
