using TC.Agro.Farm.Application.UseCases.CropTypes.GetById;
using TC.Agro.Farm.Application.UseCases.CropTypes.List;

namespace TC.Agro.Farm.Infrastructure.Repositories
{
    public sealed class CropTypeSuggestionReadStore : ICropTypeSuggestionReadStore
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IUserContext _userContext;

        public CropTypeSuggestionReadStore(ApplicationDbContext dbContext, IUserContext userContext)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
        }

        public async Task<GetCropTypeByIdResponse?> GetByIdAsync(
            Guid id,
            bool includeInactive = false,
            CancellationToken cancellationToken = default)
        {
            var query = BuildBaseQuery(includeInactive)
                .AsNoTracking()
                .Where(x => x.Id == id);

            return await query
                .Select(x => new GetCropTypeByIdResponse(
                    x.Id,
                    x.PropertyId,
                    x.OwnerId,
                    x.Property.Name.Value,
                    x.Owner.Name,
                    x.CropName.Value,
                    x.SuggestedImage,
                    x.Source,
                    x.IsOverride,
                    x.IsStale,
                    x.ConfidenceScore,
                    x.PlantingWindow,
                    x.HarvestCycleMonths,
                    x.SuggestedIrrigationType,
                    x.MinSoilMoisture,
                    x.MaxTemperature,
                    x.MinHumidity,
                    x.Notes,
                    x.Model,
                    x.GeneratedAt,
                    x.IsActive,
                    x.CreatedAt,
                    x.UpdatedAt,
                    Guid.Empty,
                    x.Id))
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);
        }

        public async Task<(IReadOnlyList<ListCropTypesResponse> CropTypes, int TotalCount)> ListAsync(
            ListCropTypesQuery query,
            CancellationToken cancellationToken = default)
        {
            var cropTypesQuery = BuildBaseQuery(query.IncludeInactive)
                .AsNoTracking();

            if (_userContext.IsAdmin && query.OwnerId.HasValue && query.OwnerId.Value != Guid.Empty)
            {
                cropTypesQuery = cropTypesQuery.Where(x => x.OwnerId == query.OwnerId.Value);
            }

            if (query.PropertyId.HasValue && query.PropertyId.Value != Guid.Empty)
            {
                cropTypesQuery = cropTypesQuery.Where(x => x.PropertyId == query.PropertyId.Value);
            }

            if (!query.IncludeStale)
            {
                cropTypesQuery = cropTypesQuery.Where(x => !x.IsStale);
            }

            if (!string.IsNullOrWhiteSpace(query.Source))
            {
                cropTypesQuery = cropTypesQuery.Where(x => EF.Functions.ILike(x.Source, query.Source));
            }

            cropTypesQuery = cropTypesQuery.ApplyTextFilter(query.Filter);

            var totalCount = await cropTypesQuery
                .CountAsync(cancellationToken)
                .ConfigureAwait(false);

            var rows = await cropTypesQuery
                .ApplySorting(query.SortBy, query.SortDirection)
                .ApplyPagination(query.PageNumber, query.PageSize)
                .Select(x => new ListCropTypesResponse(
                    x.Id,
                    x.PropertyId,
                    x.OwnerId,
                    x.Property.Name.Value,
                    x.Owner.Name,
                    x.CropName.Value,
                    x.SuggestedImage,
                    x.Source,
                    x.IsOverride,
                    x.IsStale,
                    x.ConfidenceScore,
                    x.PlantingWindow,
                    x.HarvestCycleMonths,
                    x.SuggestedIrrigationType,
                    x.MinSoilMoisture,
                    x.MaxTemperature,
                    x.MinHumidity,
                    x.Notes,
                    x.Model,
                    x.GeneratedAt,
                    x.IsActive,
                    x.CreatedAt,
                    x.UpdatedAt,
                    Guid.Empty,
                    x.Id))
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            return ([.. rows], totalCount);
        }

        private IQueryable<CropTypeSuggestionAggregate> BuildBaseQuery(bool includeInactive)
        {
            var query = includeInactive
                ? _dbContext.CropTypeSuggestions.IgnoreQueryFilters()
                : _dbContext.CropTypeSuggestions;

            return _userContext.IsAdmin
                ? query
                : query.Where(x => x.OwnerId == _userContext.Id);
        }
    }
}
