namespace TC.Agro.Farm.Application.UseCases.Properties.GetPropertyById
{
    internal sealed class GetPropertyByIdQueryHandler : BaseQueryHandler<GetPropertyByIdQuery, PropertyByIdResponse>
    {
        private readonly IPropertyReadStore _propertyReadStore;
        private readonly IUserContext _userContext;
        private readonly ILogger<GetPropertyByIdQueryHandler> _logger;

        public GetPropertyByIdQueryHandler(
            IPropertyReadStore propertyReadStore,
            IUserContext userContext,
            ILogger<GetPropertyByIdQueryHandler> logger)
        {
            _propertyReadStore = propertyReadStore ?? throw new ArgumentNullException(nameof(propertyReadStore));
            _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public override async Task<Result<PropertyByIdResponse>> ExecuteAsync(
            GetPropertyByIdQuery query,
            CancellationToken ct = default)
        {
            _logger.LogDebug("Getting property {PropertyId}", query.Id);

            var propertyResponse = await _propertyReadStore
                .GetByIdAsync(query.Id, ct)
                .ConfigureAwait(false);

            if (propertyResponse is null)
            {
                _logger.LogWarning("Property {PropertyId} not found", query.Id);
                AddError(x => x.Id, "Property not found.", FarmDomainErrors.PropertyNotFound.ErrorCode);
                return BuildNotFoundResult();
            }

            // Authorization: User can only see their own properties unless they're Admin
            if (_userContext.Role == AppConstants.UserRole
                && propertyResponse.OwnerId != _userContext.Id)
            {
                _logger.LogWarning(
                    "User {UserId} attempted to access property {PropertyId} owned by {OwnerId}",
                    _userContext.Id,
                    query.Id,
                    propertyResponse.OwnerId);
                AddError(x => x.Id, "You are not authorized to access this property.", "Property.NotAuthorized");
                return BuildNotAuthorizedResult();
            }

            return propertyResponse;
        }
    }
}
