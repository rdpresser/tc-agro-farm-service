namespace TC.Agro.Farm.Application.UseCases.Plots.Create
{
    internal sealed class CreatePlotCommandHandler
        : BaseCommandHandler<CreatePlotCommand, CreatePlotResponse, PlotAggregate, IPlotAggregateRepository>
    {
        private readonly IPropertyAggregateRepository _propertyRepository;
        private readonly ICropTypeCatalogRepository _cropTypeCatalogRepository;
        private readonly ICropTypeSuggestionRepository _cropTypeSuggestionRepository;
        private readonly ILogger<CreatePlotCommandHandler> _logger;
        private string? _resolvedCropType;

        public CreatePlotCommandHandler(
            IPlotAggregateRepository repository,
            IPropertyAggregateRepository propertyRepository,
            ICropTypeCatalogRepository cropTypeCatalogRepository,
            ICropTypeSuggestionRepository cropTypeSuggestionRepository,
            IUserContext userContext,
            ITransactionalOutbox outbox,
            ILogger<CreatePlotCommandHandler> logger)
            : base(repository, userContext, outbox, logger)
        {
            _propertyRepository = propertyRepository ?? throw new ArgumentNullException(nameof(propertyRepository));
            _cropTypeCatalogRepository = cropTypeCatalogRepository ?? throw new ArgumentNullException(nameof(cropTypeCatalogRepository));
            _cropTypeSuggestionRepository = cropTypeSuggestionRepository ?? throw new ArgumentNullException(nameof(cropTypeSuggestionRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        protected override async Task<Result<PlotAggregate>> MapAsync(CreatePlotCommand command, CancellationToken ct)
        {
            var ownerIdResult = ResolveEffectiveOwnerId(command.OwnerId);
            if (!ownerIdResult.IsSuccess)
            {
                return Result<PlotAggregate>.Invalid(ownerIdResult.ValidationErrors);
            }

            var cropReferenceResult = await ResolveCropReferencesAsync(
                command,
                ownerIdResult.Value,
                command.PropertyId,
                ct).ConfigureAwait(false);

            if (!cropReferenceResult.IsSuccess)
            {
                return Result<PlotAggregate>.Invalid(cropReferenceResult.ValidationErrors);
            }

            _resolvedCropType = cropReferenceResult.Value.ResolvedCropType;

            return CreatePlotMapper.ToAggregate(
                command,
                ownerIdResult.Value,
                cropReferenceResult.Value.ResolvedCropType,
                cropReferenceResult.Value.CropTypeCatalogId,
                cropReferenceResult.Value.SelectedCropTypeSuggestionId);
        }

        protected override async Task<Result> ValidateAsync(PlotAggregate aggregate, CancellationToken ct)
        {
            // 1. Check if property exists
            var property = await _propertyRepository
                .GetByIdAsync(aggregate.PropertyId, ct)
                .ConfigureAwait(false);

            if (property is null)
            {
                return Result.Invalid(FarmDomainErrors.PropertyNotFound);
            }

            if (property.OwnerId != aggregate.OwnerId)
            {
                return Result.Invalid(new ValidationError(
                    nameof(CreatePlotCommand.OwnerId),
                    "Selected OwnerId does not match the property owner."));
            }

            // 2. Authorization: only property owner or admin can create plots
            if (property.OwnerId != UserContext.Id && !UserContext.IsAdmin)
            {
                _logger.LogWarning(
                    "User {UserId} attempted to create plot in property {PropertyId} owned by {OwnerId}",
                    UserContext.Id,
                    aggregate.PropertyId,
                    property.OwnerId);
                return Result.Forbidden();
            }

            // 3. Check if plot name already exists for this property
            var nameExists = await Repository
                .NameExistsForPropertyAsync(aggregate.Name.Value, aggregate.PropertyId, ct)
                .ConfigureAwait(false);

            if (nameExists)
            {
                return Result.Invalid(new ValidationError(
                    "Name",
                    $"A plot with name '{aggregate.Name.Value}' already exists for this property."));
            }

            return Result.Success();
        }

        private Result<Guid> ResolveEffectiveOwnerId(Guid? requestedOwnerId)
        {
            var isAdmin = UserContext.IsAdmin;

            if (isAdmin)
            {
                if (!requestedOwnerId.HasValue || requestedOwnerId.Value == Guid.Empty)
                {
                    return Result<Guid>.Invalid(new ValidationError(
                        nameof(CreatePlotCommand.OwnerId),
                        "OwnerId is required when creating plot on behalf as Admin."));
                }

                return Result.Success(requestedOwnerId.Value);
            }

            return Result.Success(UserContext.Id);
        }

        private async Task<Result<CropReferenceResolution>> ResolveCropReferencesAsync(
            CreatePlotCommand command,
            Guid ownerId,
            Guid propertyId,
            CancellationToken ct)
        {
            var normalizedCropType = string.IsNullOrWhiteSpace(command.CropType)
                ? null
                : command.CropType.Trim();

            CropTypeCatalogAggregate? catalogAggregate = null;

            if (command.CropTypeCatalogId.HasValue)
            {
                if (command.CropTypeCatalogId.Value == Guid.Empty)
                {
                    return Result<CropReferenceResolution>.Invalid(
                        new ValidationError(nameof(command.CropTypeCatalogId), "CropTypeCatalogId cannot be empty when informed."));
                }

                catalogAggregate = await _cropTypeCatalogRepository
                    .GetByIdAsync(command.CropTypeCatalogId.Value, ct)
                    .ConfigureAwait(false);
            }
            else
            {
                if (string.IsNullOrWhiteSpace(normalizedCropType))
                {
                    return Result<CropReferenceResolution>.Invalid(
                        new ValidationError(nameof(command.CropType), "CropTypeCatalogId is required, or an existing CropType name must be informed."));
                }

                catalogAggregate = await _cropTypeCatalogRepository
                    .GetByNameAsync(normalizedCropType, ct)
                    .ConfigureAwait(false);
            }

            if (catalogAggregate is null)
            {
                return Result<CropReferenceResolution>.Invalid(FarmDomainErrors.CropTypeCatalogNotFound);
            }

            if (!string.IsNullOrWhiteSpace(normalizedCropType) &&
                !string.Equals(normalizedCropType, catalogAggregate.CropTypeName.Value, StringComparison.OrdinalIgnoreCase))
            {
                return Result<CropReferenceResolution>.Invalid(
                    new ValidationError(
                        nameof(command.CropType),
                        "CropType must match the informed CropTypeCatalogId when both are provided."));
            }

            var resolvedCropType = catalogAggregate.CropTypeName.Value;

            if (command.SelectedCropTypeSuggestionId.HasValue)
            {
                if (command.SelectedCropTypeSuggestionId.Value == Guid.Empty)
                {
                    return Result<CropReferenceResolution>.Invalid(
                        new ValidationError(
                            nameof(command.SelectedCropTypeSuggestionId),
                            "SelectedCropTypeSuggestionId cannot be empty when informed."));
                }

                var selectedSuggestion = await _cropTypeSuggestionRepository
                    .GetByIdAsync(command.SelectedCropTypeSuggestionId.Value, ct)
                    .ConfigureAwait(false);

                if (selectedSuggestion is null)
                {
                    return Result<CropReferenceResolution>.Invalid(FarmDomainErrors.CropTypeSuggestionNotFound);
                }

                if (selectedSuggestion.PropertyId != propertyId)
                {
                    return Result<CropReferenceResolution>.Invalid(
                        new ValidationError(
                            nameof(command.SelectedCropTypeSuggestionId),
                            "Selected crop type suggestion does not belong to the informed property."));
                }

                if (selectedSuggestion.OwnerId != ownerId)
                {
                    return Result<CropReferenceResolution>.Invalid(
                        new ValidationError(
                            nameof(command.SelectedCropTypeSuggestionId),
                            "Selected crop type suggestion does not belong to the informed owner."));
                }

                if (!string.Equals(selectedSuggestion.CropName.Value, resolvedCropType, StringComparison.OrdinalIgnoreCase))
                {
                    return Result<CropReferenceResolution>.Invalid(
                        new ValidationError(
                            nameof(command.SelectedCropTypeSuggestionId),
                            "Selected crop type suggestion does not match the resolved crop type catalog."));
                }
            }

            return Result<CropReferenceResolution>.Success(
                new CropReferenceResolution(
                    resolvedCropType,
                    catalogAggregate.Id,
                    command.SelectedCropTypeSuggestionId));
        }

        private sealed record CropReferenceResolution(
            string ResolvedCropType,
            Guid CropTypeCatalogId,
            Guid? SelectedCropTypeSuggestionId);

        protected override async Task PublishIntegrationEventsAsync(PlotAggregate aggregate, CancellationToken ct)
        {
            var integrationEvents = aggregate.UncommittedEvents
                .MapToIntegrationEvents(
                    aggregate: aggregate,
                    userContext: UserContext,
                    requestedOwnerId: aggregate.OwnerId,
                    handlerName: nameof(CreatePlotCommandHandler),
                    mappings: new Dictionary<Type, Func<BaseDomainEvent, PlotCreatedIntegrationEvent>>
                    {
                        { typeof(PlotCreatedDomainEvent), e => CreatePlotMapper.ToIntegrationEvent((PlotCreatedDomainEvent)e) }
                    })
                .ToList();

            if (integrationEvents.Count > 0)
            {
                foreach (var evt in integrationEvents)
                {
                    await Outbox.EnqueueAsync(evt, ct).ConfigureAwait(false);
                }
            }

            _logger.LogInformation(
                "Enqueued {Count} integration events for plot {PlotId}",
                integrationEvents.Count,
                aggregate.Id);
        }

        protected override Task<CreatePlotResponse> BuildResponseAsync(PlotAggregate aggregate, CancellationToken ct)
            => Task.FromResult(CreatePlotMapper.FromAggregate(aggregate, _resolvedCropType!));
    }
}
