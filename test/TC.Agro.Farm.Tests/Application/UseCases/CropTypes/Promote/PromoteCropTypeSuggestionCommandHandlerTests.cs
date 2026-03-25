using Ardalis.Result;
using FakeItEasy;
using Microsoft.Extensions.Logging;
using TC.Agro.Farm.Application.Abstractions.Ports;
using TC.Agro.Farm.Application.UseCases.CropTypes.Promote;
using TC.Agro.Farm.Domain.Aggregates;
using TC.Agro.Farm.Tests.TestHelpers;
using TC.Agro.SharedKernel.Application.Ports;
using TC.Agro.SharedKernel.Infrastructure.UserClaims;

namespace TC.Agro.Farm.Tests.Application.UseCases.CropTypes.Promote;

public sealed class PromoteCropTypeSuggestionCommandHandlerTests
{
    private readonly ICropTypeSuggestionRepository _suggestionRepository = A.Fake<ICropTypeSuggestionRepository>();
    private readonly ICropTypeCatalogRepository _catalogRepository = A.Fake<ICropTypeCatalogRepository>();
    private readonly ITransactionalOutbox _outbox = A.Fake<ITransactionalOutbox>();
    private readonly ILogger<PromoteCropTypeSuggestionCommandHandler> _logger = A.Fake<ILogger<PromoteCropTypeSuggestionCommandHandler>>();

    public PromoteCropTypeSuggestionCommandHandlerTests()
    {
        FastEndpointsTestBootstrap.EnsureInitialized();
    }

    [Fact]
    public async Task ExecuteAsync_WhenSuggestionDoesNotExist_ShouldReturnNotFound()
    {
        var userContext = TestUserContextFactory.CreateProducer();
        var command = new PromoteCropTypeSuggestionCommand(Guid.NewGuid());

        A.CallTo(() => _suggestionRepository.GetByIdAsync(command.SuggestionId, A<CancellationToken>._))
            .Returns(Task.FromResult<CropTypeSuggestionAggregate?>(null));

        var sut = CreateHandler(userContext);

        var result = await sut.ExecuteAsync(command, CancellationToken.None);

        result.Status.ShouldBe(ResultStatus.NotFound);
        A.CallTo(() => _catalogRepository.Add(A<CropTypeCatalogAggregate>._)).MustNotHaveHappened();
        A.CallTo(() => _outbox.SaveChangesAsync(A<CancellationToken>._)).MustNotHaveHappened();
    }

    [Fact]
    public async Task ExecuteAsync_WhenCallerIsNotOwnerAndNotAdmin_ShouldReturnUnauthorized()
    {
        var ownerId = Guid.NewGuid();
        var userContext = TestUserContextFactory.CreateProducer(Guid.NewGuid());
        var suggestion = CreateSuggestion(ownerId);
        var command = new PromoteCropTypeSuggestionCommand(suggestion.Id);

        A.CallTo(() => _suggestionRepository.GetByIdAsync(command.SuggestionId, A<CancellationToken>._))
            .Returns(suggestion);

        var sut = CreateHandler(userContext);

        var result = await sut.ExecuteAsync(command, CancellationToken.None);

        result.Status.ShouldBe(ResultStatus.Unauthorized);
        A.CallTo(() => _catalogRepository.Add(A<CropTypeCatalogAggregate>._)).MustNotHaveHappened();
    }

    [Fact]
    public async Task ExecuteAsync_WhenCatalogAlreadyExists_ShouldReturnExistingCatalogAndMarkSuggestionStale()
    {
        var ownerId = Guid.NewGuid();
        var userContext = TestUserContextFactory.CreateProducer(ownerId);
        var suggestion = CreateSuggestion(ownerId);
        var catalog = CreateCatalog(suggestion.CropName.Value, ownerId);
        var command = new PromoteCropTypeSuggestionCommand(suggestion.Id);

        A.CallTo(() => _suggestionRepository.GetByIdAsync(command.SuggestionId, A<CancellationToken>._))
            .Returns(suggestion);
        A.CallTo(() => _catalogRepository.GetByNameAsync(suggestion.CropName.Value, ownerId, A<CancellationToken>._))
            .Returns(catalog);

        var sut = CreateHandler(userContext);

        var result = await sut.ExecuteAsync(command, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.AlreadyPromoted.ShouldBeTrue();
        result.Value.CropTypeCatalogId.ShouldBe(catalog.Id);
        suggestion.IsStale.ShouldBeTrue();
        A.CallTo(() => _catalogRepository.Add(A<CropTypeCatalogAggregate>._)).MustNotHaveHappened();
        A.CallTo(() => _outbox.SaveChangesAsync(A<CancellationToken>._)).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task ExecuteAsync_WhenSuggestionIsEligible_ShouldCreateCatalogAndMarkSuggestionStale()
    {
        var ownerId = Guid.NewGuid();
        var userContext = TestUserContextFactory.CreateProducer(ownerId);
        var suggestion = CreateSuggestion(ownerId);
        var command = new PromoteCropTypeSuggestionCommand(suggestion.Id);
        CropTypeCatalogAggregate? persistedCatalog = null;

        A.CallTo(() => _suggestionRepository.GetByIdAsync(command.SuggestionId, A<CancellationToken>._))
            .Returns(suggestion);
        A.CallTo(() => _catalogRepository.GetByNameAsync(suggestion.CropName.Value, ownerId, A<CancellationToken>._))
            .Returns(Task.FromResult<CropTypeCatalogAggregate?>(null));
        A.CallTo(() => _catalogRepository.Add(A<CropTypeCatalogAggregate>._))
            .Invokes(call => persistedCatalog = call.GetArgument<CropTypeCatalogAggregate>(0));

        var sut = CreateHandler(userContext);

        var result = await sut.ExecuteAsync(command, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.AlreadyPromoted.ShouldBeFalse();
        persistedCatalog.ShouldNotBeNull();
        persistedCatalog!.OwnerId.ShouldBe(ownerId);
        persistedCatalog.CropTypeName.Value.ShouldBe(suggestion.CropName.Value);
        suggestion.IsStale.ShouldBeTrue();
        A.CallTo(() => _catalogRepository.Add(A<CropTypeCatalogAggregate>._)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _outbox.SaveChangesAsync(A<CancellationToken>._)).MustHaveHappenedOnceExactly();
    }

    private PromoteCropTypeSuggestionCommandHandler CreateHandler(IUserContext userContext)
        => new(_suggestionRepository, _catalogRepository, userContext, _outbox, _logger);

    private static CropTypeSuggestionAggregate CreateSuggestion(Guid ownerId)
    {
        var result = CropTypeSuggestionAggregate.CreateAi(
            propertyId: Guid.NewGuid(),
            ownerId: ownerId,
            cropType: "Soy",
            confidenceScore: 92,
            plantingWindow: "September to November",
            harvestCycleMonths: 5,
            suggestedIrrigationType: "Center Pivot",
            minSoilMoisture: 30,
            maxTemperature: 35,
            minHumidity: 45,
            notes: "AI recommendation",
            model: "mock-openai",
            generatedAt: DateTimeOffset.UtcNow,
            suggestedImage: "soy-icon");

        result.IsSuccess.ShouldBeTrue();
        return result.Value;
    }

    private static CropTypeCatalogAggregate CreateCatalog(string cropType, Guid ownerId)
    {
        var result = CropTypeCatalogAggregate.Create(
            cropTypeName: cropType,
            isSystemDefined: false,
            ownerId: ownerId,
            description: "Catalog metadata",
            recommendedIrrigationType: "Center Pivot",
            typicalHarvestCycleMonths: 5,
            typicalPlantingStartMonth: 9,
            typicalPlantingEndMonth: 11,
            minSoilMoisture: 30,
            maxTemperature: 35,
            minHumidity: 45,
            suggestedImage: "soy-icon");

        result.IsSuccess.ShouldBeTrue();
        return result.Value;
    }
}