namespace TC.Agro.Farm.Infrastructure.Repositories
{
    public sealed class CropTypeSuggestionRepository : BaseRepository<CropTypeSuggestionAggregate>, ICropTypeSuggestionRepository
    {
        private readonly IUserContext _userContext;

        public CropTypeSuggestionRepository(ApplicationDbContext dbContext, IUserContext userContext)
            : base(dbContext)
        {
            _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
        }

        private IQueryable<CropTypeSuggestionAggregate> FilteredDbSet => _userContext.IsAdmin
            ? DbSet
            : DbSet.Where(x => x.OwnerId == _userContext.Id);

        public async Task MarkAiSuggestionsAsStaleByPropertyAsync(Guid propertyId, CancellationToken cancellationToken = default)
        {
            var suggestions = await FilteredDbSet
                .Where(x => x.PropertyId == propertyId &&
                            x.Source == CropTypeSuggestionAggregate.AiSource &&
                            !x.IsStale)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            foreach (var suggestion in suggestions)
            {
                suggestion.MarkAsStale();
            }
        }

        public async Task DeactivateAiSuggestionsByPropertyAsync(Guid propertyId, CancellationToken cancellationToken = default)
        {
            var suggestions = await FilteredDbSet
                .Where(x => x.PropertyId == propertyId &&
                            x.Source == CropTypeSuggestionAggregate.AiSource)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            foreach (var suggestion in suggestions)
            {
                suggestion.Deactivate();
            }
        }
    }
}
