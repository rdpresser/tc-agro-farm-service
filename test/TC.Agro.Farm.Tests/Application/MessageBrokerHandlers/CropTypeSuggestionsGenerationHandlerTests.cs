using FakeItEasy;
using Microsoft.Extensions.Logging;
using TC.Agro.Farm.Application.Abstractions.Ports;
using TC.Agro.Farm.Application.MessageBrokerHandlers;
using TC.Agro.Farm.Application.UseCases.CropTypes.Regenerate;
using TC.Agro.Farm.Domain.Aggregates;
using TC.Agro.Farm.Tests.TestHelpers;
using TC.Agro.SharedKernel.Application.Ports;

namespace TC.Agro.Farm.Tests.Application.MessageBrokerHandlers;

public sealed class CropTypeSuggestionsGenerationHandlerTests
{
    private readonly IPropertyAggregateRepository _propertyRepository = A.Fake<IPropertyAggregateRepository>();
    private readonly ICropTypeSuggestionRepository _cropTypeRepository = A.Fake<ICropTypeSuggestionRepository>();
    private readonly ICropTypeSuggestionAiProvider _aiProvider = A.Fake<ICropTypeSuggestionAiProvider>();
    private readonly IUnitOfWork _unitOfWork = A.Fake<IUnitOfWork>();
    private readonly ILogger<CropTypeSuggestionsGenerationHandler> _logger =
        A.Fake<ILogger<CropTypeSuggestionsGenerationHandler>>();

    public CropTypeSuggestionsGenerationHandlerTests()
    {
        FastEndpointsTestBootstrap.EnsureInitialized();
    }

    // ──────────────────────────────────────────
    // Property not found
    // ──────────────────────────────────────────

    [Fact]
    public async Task HandleAsync_WhenPropertyNotFound_ShouldReturnEarlyAndNotCallAiProvider()
    {
        var message = new GeneratePropertyCropTypeSuggestionsMessage(
            PropertyId: Guid.NewGuid(),
            OwnerId: Guid.NewGuid(),
            TriggeredByUserId: Guid.NewGuid(),
            TriggerReason: "Manual",
            RequestedAt: DateTimeOffset.UtcNow);

        A.CallTo(() => _propertyRepository.GetByIdAsync(message.PropertyId, A<CancellationToken>._))
            .Returns(Task.FromResult<PropertyAggregate?>(null));

        var handler = CreateHandler();

        await handler.HandleAsync(message, CancellationToken.None);

        A.CallTo(() => _aiProvider.GenerateSuggestionsAsync(A<CropTypeSuggestionAiRequest>._, A<CancellationToken>._))
            .MustNotHaveHappened();

        A.CallTo(() => _unitOfWork.SaveChangesAsync(A<CancellationToken>._))
            .MustNotHaveHappened();
    }

    // ──────────────────────────────────────────
    // Property missing coordinates
    // ──────────────────────────────────────────

    [Fact]
    public async Task HandleAsync_WhenPropertyHasNoLatitude_ShouldReturnEarlyAndNotCallAiProvider()
    {
        var ownerId = Guid.NewGuid();
        var property = CreateProperty(ownerId, latitude: null, longitude: -47.8103);
        var message = new GeneratePropertyCropTypeSuggestionsMessage(
            PropertyId: property.Id,
            OwnerId: ownerId,
            TriggeredByUserId: ownerId,
            TriggerReason: "Manual",
            RequestedAt: DateTimeOffset.UtcNow);

        A.CallTo(() => _propertyRepository.GetByIdAsync(message.PropertyId, A<CancellationToken>._))
            .Returns(property);

        var handler = CreateHandler();

        await handler.HandleAsync(message, CancellationToken.None);

        A.CallTo(() => _aiProvider.GenerateSuggestionsAsync(A<CropTypeSuggestionAiRequest>._, A<CancellationToken>._))
            .MustNotHaveHappened();
    }

    [Fact]
    public async Task HandleAsync_WhenPropertyHasNoLongitude_ShouldReturnEarlyAndNotCallAiProvider()
    {
        var ownerId = Guid.NewGuid();
        var property = CreateProperty(ownerId, latitude: -21.1775, longitude: null);
        var message = new GeneratePropertyCropTypeSuggestionsMessage(
            PropertyId: property.Id,
            OwnerId: ownerId,
            TriggeredByUserId: ownerId,
            TriggerReason: "Manual",
            RequestedAt: DateTimeOffset.UtcNow);

        A.CallTo(() => _propertyRepository.GetByIdAsync(message.PropertyId, A<CancellationToken>._))
            .Returns(property);

        var handler = CreateHandler();

        await handler.HandleAsync(message, CancellationToken.None);

        A.CallTo(() => _aiProvider.GenerateSuggestionsAsync(A<CropTypeSuggestionAiRequest>._, A<CancellationToken>._))
            .MustNotHaveHappened();
    }

    // ──────────────────────────────────────────
    // AI provider failure — should swallow exception and not persist
    // ──────────────────────────────────────────

    [Fact]
    public async Task HandleAsync_WhenAiProviderThrows_ShouldSwallowExceptionAndNotPersist()
    {
        var ownerId = Guid.NewGuid();
        var property = CreateProperty(ownerId, latitude: -21.1775, longitude: -47.8103);
        var message = new GeneratePropertyCropTypeSuggestionsMessage(
            PropertyId: property.Id,
            OwnerId: ownerId,
            TriggeredByUserId: ownerId,
            TriggerReason: "Manual",
            RequestedAt: DateTimeOffset.UtcNow);

        A.CallTo(() => _propertyRepository.GetByIdAsync(message.PropertyId, A<CancellationToken>._))
            .Returns(property);

        A.CallTo(() => _aiProvider.GenerateSuggestionsAsync(A<CropTypeSuggestionAiRequest>._, A<CancellationToken>._))
            .ThrowsAsync(new HttpRequestException("OpenAI timeout"));

        var handler = CreateHandler();

        await handler.HandleAsync(message, CancellationToken.None);

        A.CallTo(() => _cropTypeRepository.AddRange(A<IEnumerable<CropTypeSuggestionAggregate>>._))
            .MustNotHaveHappened();

        A.CallTo(() => _unitOfWork.SaveChangesAsync(A<CancellationToken>._))
            .MustNotHaveHappened();
    }

    [Fact]
    public async Task HandleAsync_WhenAiProviderThrowsTaskCancelledAndTokenCancelled_ShouldRethrow()
    {
        var ownerId = Guid.NewGuid();
        var property = CreateProperty(ownerId, latitude: -21.1775, longitude: -47.8103);
        var message = new GeneratePropertyCropTypeSuggestionsMessage(
            PropertyId: property.Id,
            OwnerId: ownerId,
            TriggeredByUserId: ownerId,
            TriggerReason: "Manual",
            RequestedAt: DateTimeOffset.UtcNow);

        A.CallTo(() => _propertyRepository.GetByIdAsync(message.PropertyId, A<CancellationToken>._))
            .Returns(property);

        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        A.CallTo(() => _aiProvider.GenerateSuggestionsAsync(A<CropTypeSuggestionAiRequest>._, A<CancellationToken>._))
            .ThrowsAsync(new TaskCanceledException("cancelled by token"));

        var handler = CreateHandler();

        await Should.ThrowAsync<TaskCanceledException>(
            () => handler.HandleAsync(message, cts.Token));
    }

    // ──────────────────────────────────────────
    // AI provider returns empty list
    // ──────────────────────────────────────────

    [Fact]
    public async Task HandleAsync_WhenAiProviderReturnsNoDrafts_ShouldNotPersist()
    {
        var ownerId = Guid.NewGuid();
        var property = CreateProperty(ownerId, latitude: -21.1775, longitude: -47.8103);
        var message = new GeneratePropertyCropTypeSuggestionsMessage(
            PropertyId: property.Id,
            OwnerId: ownerId,
            TriggeredByUserId: ownerId,
            TriggerReason: "Manual",
            RequestedAt: DateTimeOffset.UtcNow);

        A.CallTo(() => _propertyRepository.GetByIdAsync(message.PropertyId, A<CancellationToken>._))
            .Returns(property);

        A.CallTo(() => _aiProvider.GenerateSuggestionsAsync(A<CropTypeSuggestionAiRequest>._, A<CancellationToken>._))
            .Returns(Task.FromResult<IReadOnlyList<CropTypeSuggestionDraft>>([]));

        var handler = CreateHandler();

        await handler.HandleAsync(message, CancellationToken.None);

        A.CallTo(() => _cropTypeRepository.AddRange(A<IEnumerable<CropTypeSuggestionAggregate>>._))
            .MustNotHaveHappened();

        A.CallTo(() => _unitOfWork.SaveChangesAsync(A<CancellationToken>._))
            .MustNotHaveHappened();
    }

    // ──────────────────────────────────────────
    // Successful generation
    // ──────────────────────────────────────────

    [Fact]
    public async Task HandleAsync_WithValidDrafts_ShouldDeactivatePreviousAndPersistNewSuggestions()
    {
        var ownerId = Guid.NewGuid();
        var property = CreateProperty(ownerId, latitude: -21.1775, longitude: -47.8103);
        var message = new GeneratePropertyCropTypeSuggestionsMessage(
            PropertyId: property.Id,
            OwnerId: ownerId,
            TriggeredByUserId: ownerId,
            TriggerReason: "Manual",
            RequestedAt: DateTimeOffset.UtcNow);

        IEnumerable<CropTypeSuggestionAggregate>? captured = null;

        A.CallTo(() => _propertyRepository.GetByIdAsync(message.PropertyId, A<CancellationToken>._))
            .Returns(property);

        A.CallTo(() => _aiProvider.GenerateSuggestionsAsync(A<CropTypeSuggestionAiRequest>._, A<CancellationToken>._))
            .Returns(BuildDrafts("Corn", "Soy", "Wheat"));

        A.CallTo(() => _cropTypeRepository.AddRange(A<IEnumerable<CropTypeSuggestionAggregate>>._))
            .Invokes(call => captured = call.GetArgument<IEnumerable<CropTypeSuggestionAggregate>>(0));

        var handler = CreateHandler();

        await handler.HandleAsync(message, CancellationToken.None);

        A.CallTo(() => _cropTypeRepository.DeactivateAiSuggestionsByPropertyAsync(property.Id, A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();

        A.CallTo(() => _unitOfWork.SaveChangesAsync(A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();

        captured.ShouldNotBeNull();
        captured!.Count().ShouldBe(3);
    }

    [Fact]
    public async Task HandleAsync_WithDuplicateCropTypeNames_ShouldDeduplicateAndPersistUnique()
    {
        var ownerId = Guid.NewGuid();
        var property = CreateProperty(ownerId, latitude: -21.1775, longitude: -47.8103);
        var message = new GeneratePropertyCropTypeSuggestionsMessage(
            PropertyId: property.Id,
            OwnerId: ownerId,
            TriggeredByUserId: ownerId,
            TriggerReason: "Manual",
            RequestedAt: DateTimeOffset.UtcNow);

        IEnumerable<CropTypeSuggestionAggregate>? captured = null;

        A.CallTo(() => _propertyRepository.GetByIdAsync(message.PropertyId, A<CancellationToken>._))
            .Returns(property);

        // "Corn" duplicated (case-insensitive), "Soy" once → should persist only 2
        A.CallTo(() => _aiProvider.GenerateSuggestionsAsync(A<CropTypeSuggestionAiRequest>._, A<CancellationToken>._))
            .Returns(BuildDrafts("Corn", "corn", "CORN", "Soy"));

        A.CallTo(() => _cropTypeRepository.AddRange(A<IEnumerable<CropTypeSuggestionAggregate>>._))
            .Invokes(call => captured = call.GetArgument<IEnumerable<CropTypeSuggestionAggregate>>(0));

        var handler = CreateHandler();

        await handler.HandleAsync(message, CancellationToken.None);

        captured.ShouldNotBeNull();
        captured!.Count().ShouldBe(2);
    }

    [Fact]
    public async Task HandleAsync_WithDraftsThatHaveEmptyName_ShouldSkipInvalidDrafts()
    {
        var ownerId = Guid.NewGuid();
        var property = CreateProperty(ownerId, latitude: -21.1775, longitude: -47.8103);
        var message = new GeneratePropertyCropTypeSuggestionsMessage(
            PropertyId: property.Id,
            OwnerId: ownerId,
            TriggeredByUserId: ownerId,
            TriggerReason: "Manual",
            RequestedAt: DateTimeOffset.UtcNow);

        IEnumerable<CropTypeSuggestionAggregate>? captured = null;

        A.CallTo(() => _propertyRepository.GetByIdAsync(message.PropertyId, A<CancellationToken>._))
            .Returns(property);

        // Two empty names, one valid
        var drafts = new List<CropTypeSuggestionDraft>
        {
            new("", null, null, null, null, null, null, null, null, null),
            new("   ", null, null, null, null, null, null, null, null, null),
            new("Soy", 90, "October-December", 5, "Drip", 30, 35, 45, "Grows well", "soy"),
        };

        A.CallTo(() => _aiProvider.GenerateSuggestionsAsync(A<CropTypeSuggestionAiRequest>._, A<CancellationToken>._))
            .Returns(Task.FromResult<IReadOnlyList<CropTypeSuggestionDraft>>(drafts));

        A.CallTo(() => _cropTypeRepository.AddRange(A<IEnumerable<CropTypeSuggestionAggregate>>._))
            .Invokes(call => captured = call.GetArgument<IEnumerable<CropTypeSuggestionAggregate>>(0));

        var handler = CreateHandler();

        await handler.HandleAsync(message, CancellationToken.None);

        captured.ShouldNotBeNull();
        captured!.Count().ShouldBe(1);
    }

    [Fact]
    public async Task HandleAsync_WithManyUniqueDrafts_ShouldPersistAllBelowMaxSuggestionsLimit()
    {
        var ownerId = Guid.NewGuid();
        var property = CreateProperty(ownerId, latitude: -21.1775, longitude: -47.8103);
        var message = new GeneratePropertyCropTypeSuggestionsMessage(
            PropertyId: property.Id,
            OwnerId: ownerId,
            TriggeredByUserId: ownerId,
            TriggerReason: "Manual",
            RequestedAt: DateTimeOffset.UtcNow);

        IEnumerable<CropTypeSuggestionAggregate>? captured = null;

        A.CallTo(() => _propertyRepository.GetByIdAsync(message.PropertyId, A<CancellationToken>._))
            .Returns(property);

        // 8 unique valid drafts, MaxSuggestions = 15, so all 8 should be persisted
        A.CallTo(() => _aiProvider.GenerateSuggestionsAsync(A<CropTypeSuggestionAiRequest>._, A<CancellationToken>._))
            .Returns(BuildDrafts("Corn", "Soy", "Wheat", "Rice", "Coffee", "Sugarcane", "Cotton", "Tobacco"));

        A.CallTo(() => _cropTypeRepository.AddRange(A<IEnumerable<CropTypeSuggestionAggregate>>._))
            .Invokes(call => captured = call.GetArgument<IEnumerable<CropTypeSuggestionAggregate>>(0));

        var handler = CreateHandler();

        await handler.HandleAsync(message, CancellationToken.None);

        captured.ShouldNotBeNull();
        captured!.Count().ShouldBe(8);
    }

    // ──────────────────────────────────────────
    // Helpers
    // ──────────────────────────────────────────

    private CropTypeSuggestionsGenerationHandler CreateHandler()
        => new(_propertyRepository, _cropTypeRepository, _aiProvider, _unitOfWork, _logger);

    private static PropertyAggregate CreateProperty(Guid ownerId, double? latitude, double? longitude)
    {
        var result = PropertyAggregate.Create(
            name: "Test Property",
            address: "Rural Road, km 1",
            city: "Ribeirao Preto",
            state: "SP",
            country: "Brazil",
            areaHectares: 100,
            ownerId: ownerId,
            latitude: latitude,
            longitude: longitude);

        result.IsSuccess.ShouldBeTrue();
        return result.Value;
    }

    private static Task<IReadOnlyList<CropTypeSuggestionDraft>> BuildDrafts(params string[] cropTypes)
    {
        var drafts = cropTypes.Select(ct =>
            new CropTypeSuggestionDraft(
                CropType: ct,
                ConfidenceScore: 85,
                PlantingWindow: "October-December",
                HarvestCycleMonths: 5,
                SuggestedIrrigationType: "Drip",
                MinSoilMoisture: 30,
                MaxTemperature: 35,
                MinHumidity: 45,
                Notes: $"Good for {ct}",
                SuggestedImage: ct[..Math.Min(ct.Length, 5)].ToLowerInvariant()))
            .ToList();

        return Task.FromResult<IReadOnlyList<CropTypeSuggestionDraft>>(drafts);
    }
}
