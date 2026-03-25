using TC.Agro.Farm.Application.Abstractions.Mappers;
using TC.Agro.Farm.Application.Abstractions.Ports;
using TC.Agro.Farm.Application.UseCases.CropTypes;
using TC.Agro.Farm.Application.UseCases.Plots.Create;
using TC.Agro.Farm.Domain.Aggregates;
using TC.Agro.SharedKernel.Application.Ports;
using TC.Agro.SharedKernel.Infrastructure.UserClaims;

namespace TC.Agro.Farm.Application.UseCases.Plots.Submit
{
    internal sealed class PlotSubmissionCoordinator
    {
        private readonly IPlotAggregateRepository _plotRepository;
        private readonly IPropertyAggregateRepository _propertyRepository;
        private readonly ICropCycleAggregateRepository _cropCycleRepository;
        private readonly ICropTypeCatalogRepository _cropTypeCatalogRepository;
        private readonly ICropTypeSuggestionRepository _cropTypeSuggestionRepository;
        private readonly IUserContext _userContext;
        private readonly ITransactionalOutbox _outbox;
        private readonly ILogger _logger;

        public PlotSubmissionCoordinator(
            IPlotAggregateRepository plotRepository,
            IPropertyAggregateRepository propertyRepository,
            ICropCycleAggregateRepository cropCycleRepository,
            ICropTypeCatalogRepository cropTypeCatalogRepository,
            ICropTypeSuggestionRepository cropTypeSuggestionRepository,
            IUserContext userContext,
            ITransactionalOutbox outbox,
            ILogger logger)
        {
            _plotRepository = plotRepository ?? throw new ArgumentNullException(nameof(plotRepository));
            _propertyRepository = propertyRepository ?? throw new ArgumentNullException(nameof(propertyRepository));
            _cropCycleRepository = cropCycleRepository ?? throw new ArgumentNullException(nameof(cropCycleRepository));
            _cropTypeCatalogRepository = cropTypeCatalogRepository ?? throw new ArgumentNullException(nameof(cropTypeCatalogRepository));
            _cropTypeSuggestionRepository = cropTypeSuggestionRepository ?? throw new ArgumentNullException(nameof(cropTypeSuggestionRepository));
            _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
            _outbox = outbox ?? throw new ArgumentNullException(nameof(outbox));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Result<PlotSubmissionResult>> CreateAsync(PlotSubmissionRequest request, CancellationToken ct)
        {
            var ownerIdResult = ResolveEffectiveOwnerId(request.OwnerId);
            if (!ownerIdResult.IsSuccess)
            {
                return Result<PlotSubmissionResult>.Invalid(ownerIdResult.ValidationErrors);
            }

            var property = await _propertyRepository
                .GetByIdAsync(request.PropertyId, ct)
                .ConfigureAwait(false);

            if (property is null)
            {
                return Result<PlotSubmissionResult>.Invalid(FarmDomainErrors.PropertyNotFound);
            }

            if (property.OwnerId != ownerIdResult.Value)
            {
                return Result<PlotSubmissionResult>.Invalid(new ValidationError(
                    nameof(request.OwnerId),
                    "Selected OwnerId does not match the property owner."));
            }

            if (property.OwnerId != _userContext.Id && !_userContext.IsAdmin)
            {
                return Result<PlotSubmissionResult>.Unauthorized("You are not authorized to create plots for this property.");
            }

            var cropReferenceResult = await ResolveCropReferencesAsync(
                request.CropType,
                request.CropTypeCatalogId,
                request.SelectedCropTypeSuggestionId,
                ownerIdResult.Value,
                request.PropertyId,
                ct).ConfigureAwait(false);

            if (!cropReferenceResult.IsSuccess)
            {
                return Result<PlotSubmissionResult>.Invalid(cropReferenceResult.ValidationErrors);
            }

            var nameExists = await _plotRepository
                .NameExistsForPropertyAsync(request.Name, request.PropertyId, ct)
                .ConfigureAwait(false);

            if (nameExists)
            {
                return Result<PlotSubmissionResult>.Invalid(new ValidationError(
                    nameof(request.Name),
                    $"A plot with name '{request.Name}' already exists for this property."));
            }

            var plotResult = PlotAggregate.Create(
                propertyId: request.PropertyId,
                ownerId: ownerIdResult.Value,
                name: request.Name,
                cropType: cropReferenceResult.Value.ResolvedCropType,
                areaHectares: request.AreaHectares,
                plantingDate: request.PlantingDate,
                expectedHarvestDate: request.ExpectedHarvestDate,
                irrigationType: request.IrrigationType,
                additionalNotes: request.AdditionalNotes,
                latitude: request.Latitude,
                longitude: request.Longitude,
                boundaryGeoJson: request.BoundaryGeoJson,
                cropTypeCatalogId: cropReferenceResult.Value.CropTypeCatalogId,
                selectedCropTypeSuggestionId: cropReferenceResult.Value.SelectedCropTypeSuggestionId);

            if (!plotResult.IsSuccess)
            {
                return Result<PlotSubmissionResult>.Invalid(plotResult.ValidationErrors);
            }

            var cycleResult = CropCycleAggregate.Start(
                plotId: plotResult.Value.Id,
                propertyId: request.PropertyId,
                ownerId: ownerIdResult.Value,
                cropTypeCatalogId: cropReferenceResult.Value.CropTypeCatalogId,
                startedAt: request.PlantingDate,
                irrigationType: request.IrrigationType,
                expectedHarvestDate: request.ExpectedHarvestDate,
                selectedCropTypeSuggestionId: cropReferenceResult.Value.SelectedCropTypeSuggestionId,
                status: CropCycleStatus.Planted,
                notes: request.AdditionalNotes);

            if (!cycleResult.IsSuccess)
            {
                return Result<PlotSubmissionResult>.Invalid(cycleResult.ValidationErrors);
            }

            var plot = plotResult.Value;
            var cycle = cycleResult.Value;
            _plotRepository.Add(plot);
            _cropCycleRepository.Add(cycle);

            await PublishCreateIntegrationEventsAsync(plot, ct).ConfigureAwait(false);
            await _outbox.SaveChangesAsync(ct).ConfigureAwait(false);

            _logger.LogInformation(
                "Created plot {PlotId} and initial crop cycle {CropCycleId} for property {PropertyId}",
                plot.Id,
                cycle.Id,
                plot.PropertyId);

            return Result<PlotSubmissionResult>.Success(new PlotSubmissionResult(plot, cycle, cropReferenceResult.Value.ResolvedCropType, true));
        }

        public async Task<Result<PlotSubmissionResult>> UpdateAsync(PlotSubmissionRequest request, CancellationToken ct)
        {
            var plot = await _plotRepository.GetByIdAsync(request.PlotId, ct).ConfigureAwait(false);
            if (plot is null)
            {
                return Result<PlotSubmissionResult>.NotFound("Plot not found.");
            }

            if (plot.OwnerId != _userContext.Id && !_userContext.IsAdmin)
            {
                return Result<PlotSubmissionResult>.Unauthorized("You are not authorized to update this plot.");
            }

            var cropReferenceResult = await ResolveCropReferencesAsync(
                request.CropType,
                request.CropTypeCatalogId,
                request.SelectedCropTypeSuggestionId,
                plot.OwnerId,
                plot.PropertyId,
                ct).ConfigureAwait(false);

            if (!cropReferenceResult.IsSuccess)
            {
                return Result<PlotSubmissionResult>.Invalid(cropReferenceResult.ValidationErrors);
            }

            var nameExists = await _plotRepository
                .NameExistsForPropertyExcludingAsync(request.Name, plot.PropertyId, plot.Id, ct)
                .ConfigureAwait(false);

            if (nameExists)
            {
                return Result<PlotSubmissionResult>.Invalid(new ValidationError(
                    nameof(request.Name),
                    $"A plot with name '{request.Name}' already exists for this property."));
            }

            var plotUpdateResult = plot.Update(
                name: request.Name,
                cropType: cropReferenceResult.Value.ResolvedCropType,
                areaHectares: request.AreaHectares,
                plantingDate: request.PlantingDate,
                expectedHarvestDate: request.ExpectedHarvestDate,
                irrigationType: request.IrrigationType,
                additionalNotes: request.AdditionalNotes,
                latitude: request.Latitude,
                longitude: request.Longitude,
                boundaryGeoJson: request.BoundaryGeoJson,
                cropTypeCatalogId: cropReferenceResult.Value.CropTypeCatalogId,
                selectedCropTypeSuggestionId: cropReferenceResult.Value.SelectedCropTypeSuggestionId);

            if (!plotUpdateResult.IsSuccess)
            {
                return Result<PlotSubmissionResult>.Invalid(plotUpdateResult.ValidationErrors);
            }

            var currentCycle = await _cropCycleRepository
                .GetCurrentByPlotAsync(plot.Id, ct)
                .ConfigureAwait(false);

            if (currentCycle is null || !CanReviseCycle(currentCycle))
            {
                var startResult = CropCycleAggregate.Start(
                    plotId: plot.Id,
                    propertyId: plot.PropertyId,
                    ownerId: plot.OwnerId,
                    cropTypeCatalogId: cropReferenceResult.Value.CropTypeCatalogId,
                    startedAt: request.PlantingDate,
                    irrigationType: request.IrrigationType,
                    expectedHarvestDate: request.ExpectedHarvestDate,
                    selectedCropTypeSuggestionId: cropReferenceResult.Value.SelectedCropTypeSuggestionId,
                    status: CropCycleStatus.Planted,
                    notes: request.AdditionalNotes);

                if (!startResult.IsSuccess)
                {
                    return Result<PlotSubmissionResult>.Invalid(startResult.ValidationErrors);
                }

                currentCycle = startResult.Value;
                _cropCycleRepository.Add(currentCycle);
            }
            else
            {
                var reviseResult = currentCycle.Revise(
                    cropTypeCatalogId: cropReferenceResult.Value.CropTypeCatalogId,
                    selectedCropTypeSuggestionId: cropReferenceResult.Value.SelectedCropTypeSuggestionId,
                    startedAt: request.PlantingDate,
                    expectedHarvestDate: request.ExpectedHarvestDate,
                    irrigationType: request.IrrigationType,
                    notes: request.AdditionalNotes);

                if (!reviseResult.IsSuccess)
                {
                    return Result<PlotSubmissionResult>.Invalid(reviseResult.ValidationErrors);
                }
            }

            await _outbox.SaveChangesAsync(ct).ConfigureAwait(false);

            _logger.LogInformation(
                "Updated plot {PlotId} and synchronized crop cycle {CropCycleId}",
                plot.Id,
                currentCycle.Id);

            return Result<PlotSubmissionResult>.Success(new PlotSubmissionResult(plot, currentCycle, cropReferenceResult.Value.ResolvedCropType, false));
        }

        private async Task<Result<CropReferenceResolution>> ResolveCropReferencesAsync(
            string cropType,
            Guid? cropTypeCatalogId,
            Guid? selectedCropTypeSuggestionId,
            Guid ownerId,
            Guid propertyId,
            CancellationToken ct)
        {
            var normalizedCropType = string.IsNullOrWhiteSpace(cropType)
                ? null
                : cropType.Trim();
            var normalizedSuggestionId = NormalizeSelectedSuggestionId(cropTypeCatalogId, selectedCropTypeSuggestionId);

            if (cropTypeCatalogId.HasValue)
            {
                return await ResolveUsingCatalogAsync(
                    cropTypeCatalogId.Value,
                    normalizedCropType,
                    normalizedSuggestionId,
                    ownerId,
                    propertyId,
                    ct).ConfigureAwait(false);
            }

            if (normalizedSuggestionId.HasValue)
            {
                return await ResolveUsingSuggestionAsync(
                    normalizedSuggestionId.Value,
                    ownerId,
                    propertyId,
                    normalizedCropType,
                    ct).ConfigureAwait(false);
            }

            return await ResolveUsingCropTypeNameAsync(normalizedCropType, ownerId, ct).ConfigureAwait(false);
        }

        private async Task<Result<CropReferenceResolution>> ResolveUsingCatalogAsync(
            Guid cropTypeCatalogId,
            string? normalizedCropType,
            Guid? normalizedSuggestionId,
            Guid ownerId,
            Guid propertyId,
            CancellationToken ct)
        {
            var catalog = await _cropTypeCatalogRepository
                .GetByIdScopedAsync(cropTypeCatalogId, ownerId, cancellationToken: ct)
                .ConfigureAwait(false);

            if (catalog is null)
            {
                return Result<CropReferenceResolution>.Invalid(FarmDomainErrors.CropTypeCatalogNotFound);
            }

            if (!string.IsNullOrWhiteSpace(normalizedCropType) &&
                !string.Equals(normalizedCropType, catalog.CropTypeName.Value, StringComparison.OrdinalIgnoreCase))
            {
                return Result<CropReferenceResolution>.Invalid(new ValidationError(
                    nameof(SubmitPlotCommand.CropType),
                    "CropType must match the informed CropTypeCatalogId when both are provided."));
            }

            if (normalizedSuggestionId.HasValue)
            {
                var suggestionValidation = await ValidateSuggestionAsync(
                    normalizedSuggestionId.Value,
                    ownerId,
                    propertyId,
                    catalog.CropTypeName.Value,
                    ct).ConfigureAwait(false);

                if (!suggestionValidation.IsSuccess)
                {
                    return Result<CropReferenceResolution>.Invalid(suggestionValidation.ValidationErrors);
                }
            }

            return Result<CropReferenceResolution>.Success(new CropReferenceResolution(
                catalog.CropTypeName.Value,
                catalog.Id,
                normalizedSuggestionId));
        }

        private async Task<Result<CropReferenceResolution>> ResolveUsingSuggestionAsync(
            Guid normalizedSuggestionId,
            Guid ownerId,
            Guid propertyId,
            string? normalizedCropType,
            CancellationToken ct)
            => await PromoteSuggestionSelectionAsync(
                normalizedSuggestionId,
                ownerId,
                propertyId,
                normalizedCropType,
                ct).ConfigureAwait(false);

        private async Task<Result<CropReferenceResolution>> ResolveUsingCropTypeNameAsync(
            string? normalizedCropType,
            Guid ownerId,
            CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(normalizedCropType))
            {
                return Result<CropReferenceResolution>.Invalid(new ValidationError(
                    nameof(SubmitPlotCommand.CropType),
                    "CropType, CropTypeCatalogId or SelectedCropTypeSuggestionId is required."));
            }

            var existingCatalog = await _cropTypeCatalogRepository
                .GetByNameAsync(normalizedCropType, ownerId, ct)
                .ConfigureAwait(false);

            if (existingCatalog is null)
            {
                return Result<CropReferenceResolution>.Invalid(FarmDomainErrors.CropTypeCatalogNotFound);
            }

            return Result<CropReferenceResolution>.Success(new CropReferenceResolution(
                existingCatalog.CropTypeName.Value,
                existingCatalog.Id,
                null));
        }

        private async Task<Result<CropReferenceResolution>> PromoteSuggestionSelectionAsync(
            Guid suggestionId,
            Guid ownerId,
            Guid propertyId,
            string? requestedCropType,
            CancellationToken ct)
        {
            var suggestion = await _cropTypeSuggestionRepository
                .GetByIdAsync(suggestionId, ct)
                .ConfigureAwait(false);

            if (suggestion is null)
            {
                return Result<CropReferenceResolution>.Invalid(FarmDomainErrors.CropTypeSuggestionNotFound);
            }

            var suggestionScopeValidation = ValidateSuggestionScope(suggestion, ownerId, propertyId);
            if (!suggestionScopeValidation.IsSuccess)
            {
                return Result<CropReferenceResolution>.Invalid(suggestionScopeValidation.ValidationErrors);
            }

            if (!string.IsNullOrWhiteSpace(requestedCropType) &&
                !string.Equals(requestedCropType, suggestion.CropName.Value, StringComparison.OrdinalIgnoreCase))
            {
                return Result<CropReferenceResolution>.Invalid(new ValidationError(
                    nameof(SubmitPlotCommand.CropType),
                    "CropType must match the selected crop type suggestion when suggestion-only submit is used."));
            }

            var existingCatalog = await _cropTypeCatalogRepository
                .GetByNameAsync(suggestion.CropName.Value, ownerId, ct)
                .ConfigureAwait(false);

            if (existingCatalog is not null)
            {
                if (!suggestion.IsStale)
                {
                    var staleResult = suggestion.MarkAsStale();
                    if (!staleResult.IsSuccess)
                    {
                        return Result<CropReferenceResolution>.Invalid(staleResult.ValidationErrors);
                    }
                }

                return Result<CropReferenceResolution>.Success(new CropReferenceResolution(
                    existingCatalog.CropTypeName.Value,
                    existingCatalog.Id,
                    suggestion.Id));
            }

            var (startMonth, endMonth) = CropTypeCatalogCommandMapping.ParsePlantingWindow(suggestion.PlantingWindow);

            var aggregateResult = CropTypeCatalogAggregate.Create(
                cropTypeName: suggestion.CropName.Value,
                isSystemDefined: false,
                description: suggestion.Notes,
                recommendedIrrigationType: suggestion.SuggestedIrrigationType,
                typicalHarvestCycleMonths: suggestion.HarvestCycleMonths,
                scientificName: null,
                typicalPlantingStartMonth: startMonth,
                typicalPlantingEndMonth: endMonth,
                minTemperature: null,
                maxTemperature: suggestion.MaxTemperature,
                minHumidity: suggestion.MinHumidity,
                minSoilMoisture: suggestion.MinSoilMoisture,
                maxSoilMoisture: null,
                suggestedImage: suggestion.SuggestedImage,
                ownerId: ownerId);

            if (!aggregateResult.IsSuccess)
            {
                return Result<CropReferenceResolution>.Invalid(aggregateResult.ValidationErrors);
            }

            var staleMarkResult = suggestion.MarkAsStale();
            if (!staleMarkResult.IsSuccess)
            {
                return Result<CropReferenceResolution>.Invalid(staleMarkResult.ValidationErrors);
            }

            var catalog = aggregateResult.Value;
            _cropTypeCatalogRepository.Add(catalog);

            return Result<CropReferenceResolution>.Success(new CropReferenceResolution(
                catalog.CropTypeName.Value,
                catalog.Id,
                suggestion.Id));
        }

        private async Task<Result> ValidateSuggestionAsync(
            Guid suggestionId,
            Guid ownerId,
            Guid propertyId,
            string resolvedCropType,
            CancellationToken ct)
        {
            var suggestion = await _cropTypeSuggestionRepository
                .GetByIdAsync(suggestionId, ct)
                .ConfigureAwait(false);

            if (suggestion is null)
            {
                return Result.Invalid(FarmDomainErrors.CropTypeSuggestionNotFound);
            }

            var suggestionScopeValidation = ValidateSuggestionScope(suggestion, ownerId, propertyId);
            if (!suggestionScopeValidation.IsSuccess)
            {
                return suggestionScopeValidation;
            }

            if (!string.Equals(suggestion.CropName.Value, resolvedCropType, StringComparison.OrdinalIgnoreCase))
            {
                return Result.Invalid(new ValidationError(
                    nameof(SubmitPlotCommand.SelectedCropTypeSuggestionId),
                    "Selected crop type suggestion does not match the resolved crop type catalog."));
            }

            return Result.Success();
        }

        private static Result ValidateSuggestionScope(
            CropTypeSuggestionAggregate suggestion,
            Guid ownerId,
            Guid propertyId)
        {
            if (suggestion.PropertyId != propertyId)
            {
                return Result.Invalid(new ValidationError(
                    nameof(SubmitPlotCommand.SelectedCropTypeSuggestionId),
                    "Selected crop type suggestion does not belong to the informed property."));
            }

            if (suggestion.OwnerId != ownerId)
            {
                return Result.Invalid(new ValidationError(
                    nameof(SubmitPlotCommand.SelectedCropTypeSuggestionId),
                    "Selected crop type suggestion does not belong to the informed owner."));
            }

            return Result.Success();
        }

        private async Task PublishCreateIntegrationEventsAsync(PlotAggregate aggregate, CancellationToken ct)
        {
            var integrationEvents = aggregate.UncommittedEvents
                .MapToIntegrationEvents(
                    aggregate: aggregate,
                    userContext: _userContext,
                    requestedOwnerId: aggregate.OwnerId,
                    handlerName: nameof(PlotSubmissionCoordinator),
                    mappings: new Dictionary<Type, Func<BaseDomainEvent, PlotCreatedIntegrationEvent>>
                    {
                        { typeof(PlotAggregate.PlotCreatedDomainEvent), e => CreatePlotMapper.ToIntegrationEvent((PlotAggregate.PlotCreatedDomainEvent)e) }
                    })
                .ToList();

            foreach (var integrationEvent in integrationEvents)
            {
                await _outbox.EnqueueAsync(integrationEvent, ct).ConfigureAwait(false);
            }
        }

        private Result<Guid> ResolveEffectiveOwnerId(Guid? requestedOwnerId)
        {
            if (_userContext.IsAdmin)
            {
                if (!requestedOwnerId.HasValue || requestedOwnerId.Value == Guid.Empty)
                {
                    return Result<Guid>.Invalid(new ValidationError(
                        nameof(SubmitPlotCommand.OwnerId),
                        "OwnerId is required when creating plot on behalf as Admin."));
                }

                return Result.Success(requestedOwnerId.Value);
            }

            return Result.Success(_userContext.Id);
        }

        private static bool CanReviseCycle(CropCycleAggregate cycle)
            => cycle.Status.IsActiveCycle && !cycle.EndedAt.HasValue;

        private static Guid? NormalizeSelectedSuggestionId(Guid? cropTypeCatalogId, Guid? selectedCropTypeSuggestionId)
        {
            if (!selectedCropTypeSuggestionId.HasValue)
            {
                return null;
            }

            if (cropTypeCatalogId.HasValue && cropTypeCatalogId.Value != Guid.Empty && cropTypeCatalogId == selectedCropTypeSuggestionId)
            {
                return null;
            }

            return selectedCropTypeSuggestionId;
        }
    }

    internal sealed record PlotSubmissionRequest(
        Guid PlotId,
        Guid PropertyId,
        string Name,
        string CropType,
        double AreaHectares,
        double? Latitude,
        double? Longitude,
        string? BoundaryGeoJson,
        DateTimeOffset PlantingDate,
        DateTimeOffset ExpectedHarvestDate,
        string IrrigationType,
        string? AdditionalNotes,
        Guid? OwnerId,
        Guid? CropTypeCatalogId,
        Guid? SelectedCropTypeSuggestionId);

    internal sealed record PlotSubmissionResult(
        PlotAggregate Plot,
        CropCycleAggregate CurrentCycle,
        string ResolvedCropType,
        bool IsCreated);

    internal sealed record CropReferenceResolution(
        string ResolvedCropType,
        Guid CropTypeCatalogId,
        Guid? SelectedCropTypeSuggestionId);
}
