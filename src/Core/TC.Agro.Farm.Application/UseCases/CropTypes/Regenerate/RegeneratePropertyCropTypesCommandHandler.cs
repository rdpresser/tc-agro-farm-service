using Wolverine;

namespace TC.Agro.Farm.Application.UseCases.CropTypes.Regenerate
{
    internal sealed class RegeneratePropertyCropTypesCommandHandler
        : BaseHandler<RegeneratePropertyCropTypesCommand, RegeneratePropertyCropTypesResponse>
    {
        private readonly IPropertyAggregateRepository _propertyRepository;
        private readonly IUserContext _userContext;
        private readonly IMessageBus _messageBus;
        private readonly ILogger<RegeneratePropertyCropTypesCommandHandler> _logger;

        public RegeneratePropertyCropTypesCommandHandler(
            IPropertyAggregateRepository propertyRepository,
            IUserContext userContext,
            IMessageBus messageBus,
            ILogger<RegeneratePropertyCropTypesCommandHandler> logger)
        {
            _propertyRepository = propertyRepository ?? throw new ArgumentNullException(nameof(propertyRepository));
            _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
            _messageBus = messageBus ?? throw new ArgumentNullException(nameof(messageBus));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public override async Task<Result<RegeneratePropertyCropTypesResponse>> ExecuteAsync(
            RegeneratePropertyCropTypesCommand command,
            CancellationToken ct = default)
        {
            var property = await _propertyRepository
                .GetByIdAsync(command.PropertyId, ct)
                .ConfigureAwait(false);

            if (property is null)
            {
                AddError(x => x.PropertyId, "Property not found.", FarmDomainErrors.PropertyNotFound.ErrorCode);
                return BuildNotFoundResult();
            }

            if (property.OwnerId != _userContext.Id && !_userContext.IsAdmin)
            {
                AddError(x => x.PropertyId, "You are not authorized to regenerate crop suggestions for this property.", "CropTypeSuggestion.NotAuthorized");
                return BuildNotAuthorizedResult();
            }

            if (!property.Location.Latitude.HasValue || !property.Location.Longitude.HasValue)
            {
                AddError(x => x.PropertyId, "Property must have latitude and longitude to generate location-based crop suggestions.", "CropTypeSuggestion.MissingCoordinates");
                return BuildValidationErrorResult();
            }

            var queuedAt = DateTimeOffset.UtcNow;
            var effectiveUserId = _userContext.Id == Guid.Empty ? property.OwnerId : _userContext.Id;
            var message = new GeneratePropertyCropTypeSuggestionsMessage(
                PropertyId: property.Id,
                OwnerId: property.OwnerId,
                TriggeredByUserId: effectiveUserId,
                TriggerReason: "manual-regenerate",
                RequestedAt: queuedAt);

            ct.ThrowIfCancellationRequested();
            await _messageBus.PublishAsync(message).ConfigureAwait(false);

            _logger.LogInformation(
                "Queued crop type suggestion regeneration for property {PropertyId} by user {UserId}",
                property.Id,
                _userContext.Id);

            return new RegeneratePropertyCropTypesResponse(
                PropertyId: property.Id,
                Status: "Queued",
                QueuedAt: queuedAt);
        }
    }
}
