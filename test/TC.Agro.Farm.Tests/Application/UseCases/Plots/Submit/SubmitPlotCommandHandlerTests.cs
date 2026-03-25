using Ardalis.Result;
using FakeItEasy;
using Microsoft.Extensions.Logging;
using TC.Agro.Farm.Application.Abstractions.Ports;
using TC.Agro.Farm.Application.UseCases.Plots.Submit;
using TC.Agro.Farm.Domain.Aggregates;
using TC.Agro.Farm.Tests.TestHelpers;
using TC.Agro.SharedKernel.Application.Ports;
using TC.Agro.SharedKernel.Infrastructure.UserClaims;

namespace TC.Agro.Farm.Tests.Application.UseCases.Plots.Submit;

public sealed class SubmitPlotCommandHandlerTests
{
    private readonly IPlotAggregateRepository _plotRepository = A.Fake<IPlotAggregateRepository>();
    private readonly IPropertyAggregateRepository _propertyRepository = A.Fake<IPropertyAggregateRepository>();
    private readonly ICropCycleAggregateRepository _cropCycleRepository = A.Fake<ICropCycleAggregateRepository>();
    private readonly ICropTypeCatalogRepository _cropTypeCatalogRepository = A.Fake<ICropTypeCatalogRepository>();
    private readonly ICropTypeSuggestionRepository _cropTypeSuggestionRepository = A.Fake<ICropTypeSuggestionRepository>();
    private readonly ITransactionalOutbox _outbox = A.Fake<ITransactionalOutbox>();
    private readonly ILogger<SubmitPlotCommandHandler> _logger = A.Fake<ILogger<SubmitPlotCommandHandler>>();

    public SubmitPlotCommandHandlerTests()
    {
        FastEndpointsTestBootstrap.EnsureInitialized();
    }

    [Fact]
    public async Task ExecuteAsync_WhenCreateSubmitUsesSuggestionOnly_ShouldPromoteAndCreatePlotInSingleFlow()
    {
        var ownerId = Guid.NewGuid();
        var userContext = TestUserContextFactory.CreateProducer(ownerId);
        var property = CreateProperty(ownerId);
        var propertyId = property.Id;
        var suggestion = CreateSuggestion(propertyId, ownerId, "Soy");
        CropTypeCatalogAggregate? persistedCatalog = null;
        PlotAggregate? persistedPlot = null;

        var command = CreateValidCommand() with
        {
            PropertyId = propertyId,
            CropType = "Soy",
            CropTypeCatalogId = null,
            SelectedCropTypeSuggestionId = suggestion.Id
        };

        A.CallTo(() => _propertyRepository.GetByIdAsync(propertyId, A<CancellationToken>._)).Returns(property);
        A.CallTo(() => _plotRepository.NameExistsForPropertyAsync(command.Name, propertyId, A<CancellationToken>._)).Returns(false);
        A.CallTo(() => _cropTypeSuggestionRepository.GetByIdAsync(suggestion.Id, A<CancellationToken>._)).Returns(suggestion);
        A.CallTo(() => _cropTypeCatalogRepository.GetByNameAsync("Soy", ownerId, A<CancellationToken>._))
            .Returns(Task.FromResult<CropTypeCatalogAggregate?>(null));
        A.CallTo(() => _cropTypeCatalogRepository.Add(A<CropTypeCatalogAggregate>._))
            .Invokes(call => persistedCatalog = call.GetArgument<CropTypeCatalogAggregate>(0));
        A.CallTo(() => _plotRepository.Add(A<PlotAggregate>._))
            .Invokes(call => persistedPlot = call.GetArgument<PlotAggregate>(0));

        var sut = CreateHandler(userContext);

        var result = await sut.ExecuteAsync(command, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.IsCreated.ShouldBeTrue();
        result.Value.CropType.ShouldBe("Soy");
        result.Value.SelectedCropTypeSuggestionId.ShouldBe(suggestion.Id);
        persistedCatalog.ShouldNotBeNull();
        persistedPlot.ShouldNotBeNull();
        persistedPlot!.CropTypeCatalogId.ShouldBe(persistedCatalog!.Id);
        suggestion.IsStale.ShouldBeTrue();
        A.CallTo(() => _outbox.SaveChangesAsync(A<CancellationToken>._)).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task ExecuteAsync_WhenUpdateSubmitUsesSuggestionOnly_ShouldReuseExistingCatalogAndUpdatePlot()
    {
        var ownerId = Guid.NewGuid();
        var propertyId = Guid.NewGuid();
        var userContext = TestUserContextFactory.CreateProducer(ownerId);
        var plot = CreatePlot(propertyId, ownerId, "Soy");
        var suggestion = CreateSuggestion(propertyId, ownerId, "Corn");
        var existingCatalog = CreateCatalog(ownerId, "Corn");

        var command = CreateValidCommand() with
        {
            PlotId = plot.Id,
            PropertyId = Guid.Empty,
            CropType = "Corn",
            CropTypeCatalogId = null,
            SelectedCropTypeSuggestionId = suggestion.Id
        };

        A.CallTo(() => _plotRepository.GetByIdAsync(plot.Id, A<CancellationToken>._)).Returns(plot);
        A.CallTo(() => _plotRepository.NameExistsForPropertyExcludingAsync(command.Name, propertyId, plot.Id, A<CancellationToken>._)).Returns(false);
        A.CallTo(() => _cropTypeSuggestionRepository.GetByIdAsync(suggestion.Id, A<CancellationToken>._)).Returns(suggestion);
        A.CallTo(() => _cropTypeCatalogRepository.GetByNameAsync("Corn", ownerId, A<CancellationToken>._)).Returns(existingCatalog);

        var sut = CreateHandler(userContext);

        var result = await sut.ExecuteAsync(command, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.IsCreated.ShouldBeFalse();
        result.Value.CropType.ShouldBe("Corn");
        result.Value.CropTypeCatalogId.ShouldBe(existingCatalog.Id);
        result.Value.SelectedCropTypeSuggestionId.ShouldBe(suggestion.Id);
        suggestion.IsStale.ShouldBeTrue();
        A.CallTo(() => _cropTypeCatalogRepository.Add(A<CropTypeCatalogAggregate>._)).MustNotHaveHappened();
        A.CallTo(() => _outbox.SaveChangesAsync(A<CancellationToken>._)).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task ExecuteAsync_WhenSuggestionBelongsToAnotherOwner_ShouldReturnValidationError()
    {
        // existing test body preserved below
        var ownerId = Guid.NewGuid();
        var foreignOwnerId = Guid.NewGuid();
        var userContext = TestUserContextFactory.CreateProducer(ownerId);
        var property = CreateProperty(ownerId);
        var propertyId = property.Id;
        var suggestion = CreateSuggestion(propertyId, foreignOwnerId, "Soy");

        var command = CreateValidCommand() with
        {
            PropertyId = propertyId,
            SelectedCropTypeSuggestionId = suggestion.Id,
            CropTypeCatalogId = null
        };

        A.CallTo(() => _propertyRepository.GetByIdAsync(propertyId, A<CancellationToken>._)).Returns(property);
        A.CallTo(() => _cropTypeSuggestionRepository.GetByIdAsync(suggestion.Id, A<CancellationToken>._)).Returns(suggestion);
        A.CallTo(() => _plotRepository.NameExistsForPropertyAsync(command.Name, propertyId, A<CancellationToken>._)).Returns(false);

        var sut = CreateHandler(userContext);

        var result = await sut.ExecuteAsync(command, CancellationToken.None);

        result.Status.ShouldBe(ResultStatus.Invalid);
        result.ValidationErrors.ShouldContain(x => x.Identifier == nameof(SubmitPlotCommand.SelectedCropTypeSuggestionId));
        A.CallTo(() => _outbox.SaveChangesAsync(A<CancellationToken>._)).MustNotHaveHappened();
    }

    [Fact]
    public async Task ExecuteAsync_WhenCreateSubmitUsesCatalogIdOnly_ShouldCreatePlotWithoutPromotion()
    {
        var ownerId = Guid.NewGuid();
        var userContext = TestUserContextFactory.CreateProducer(ownerId);
        var property = CreateProperty(ownerId);
        var propertyId = property.Id;
        var catalog = CreateCatalog(ownerId, "Corn");
        PlotAggregate? persistedPlot = null;

        var command = CreateValidCommand() with
        {
            PropertyId = propertyId,
            CropType = "Corn",
            CropTypeCatalogId = catalog.Id,
            SelectedCropTypeSuggestionId = null
        };

        A.CallTo(() => _propertyRepository.GetByIdAsync(propertyId, A<CancellationToken>._)).Returns(property);
        A.CallTo(() => _plotRepository.NameExistsForPropertyAsync(command.Name, propertyId, A<CancellationToken>._)).Returns(false);
        A.CallTo(() => _cropTypeCatalogRepository.GetByIdScopedAsync(catalog.Id, ownerId, A<bool>._, A<CancellationToken>._)).Returns(catalog);
        A.CallTo(() => _plotRepository.Add(A<PlotAggregate>._))
            .Invokes(call => persistedPlot = call.GetArgument<PlotAggregate>(0));

        var sut = CreateHandler(userContext);

        var result = await sut.ExecuteAsync(command, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.IsCreated.ShouldBeTrue();
        result.Value.CropType.ShouldBe("Corn");
        result.Value.CropTypeCatalogId.ShouldBe(catalog.Id);
        persistedPlot.ShouldNotBeNull();
        persistedPlot!.CropTypeCatalogId.ShouldBe(catalog.Id);
        A.CallTo(() => _cropTypeSuggestionRepository.GetByIdAsync(A<Guid>._, A<CancellationToken>._)).MustNotHaveHappened();
        A.CallTo(() => _cropTypeCatalogRepository.Add(A<CropTypeCatalogAggregate>._)).MustNotHaveHappened();
        A.CallTo(() => _outbox.SaveChangesAsync(A<CancellationToken>._)).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task ExecuteAsync_WhenCreateSubmitUsesCatalogAndSuggestion_ShouldKeepCatalogAndSuggestionReferences()
    {
        var ownerId = Guid.NewGuid();
        var userContext = TestUserContextFactory.CreateProducer(ownerId);
        var property = CreateProperty(ownerId);
        var propertyId = property.Id;
        var catalog = CreateCatalog(ownerId, "Corn");
        var suggestion = CreateSuggestion(propertyId, ownerId, "Corn");
        PlotAggregate? persistedPlot = null;

        var command = CreateValidCommand() with
        {
            PropertyId = propertyId,
            CropType = "Corn",
            CropTypeCatalogId = catalog.Id,
            SelectedCropTypeSuggestionId = suggestion.Id
        };

        A.CallTo(() => _propertyRepository.GetByIdAsync(propertyId, A<CancellationToken>._)).Returns(property);
        A.CallTo(() => _plotRepository.NameExistsForPropertyAsync(command.Name, propertyId, A<CancellationToken>._)).Returns(false);
        A.CallTo(() => _cropTypeCatalogRepository.GetByIdScopedAsync(catalog.Id, ownerId, A<bool>._, A<CancellationToken>._)).Returns(catalog);
        A.CallTo(() => _cropTypeSuggestionRepository.GetByIdAsync(suggestion.Id, A<CancellationToken>._)).Returns(suggestion);
        A.CallTo(() => _plotRepository.Add(A<PlotAggregate>._))
            .Invokes(call => persistedPlot = call.GetArgument<PlotAggregate>(0));

        var sut = CreateHandler(userContext);

        var result = await sut.ExecuteAsync(command, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.CropType.ShouldBe("Corn");
        result.Value.CropTypeCatalogId.ShouldBe(catalog.Id);
        result.Value.SelectedCropTypeSuggestionId.ShouldBe(suggestion.Id);
        persistedPlot.ShouldNotBeNull();
        persistedPlot!.CropTypeCatalogId.ShouldBe(catalog.Id);
        persistedPlot.SelectedCropTypeSuggestionId.ShouldBe(suggestion.Id);
        A.CallTo(() => _cropTypeCatalogRepository.Add(A<CropTypeCatalogAggregate>._)).MustNotHaveHappened();
    }

    private SubmitPlotCommandHandler CreateHandler(IUserContext userContext)
        => new(
            _plotRepository,
            _propertyRepository,
            _cropCycleRepository,
            _cropTypeCatalogRepository,
            _cropTypeSuggestionRepository,
            userContext,
            _outbox,
            _logger);

    private static SubmitPlotCommand CreateValidCommand()
        => new()
        {
            PropertyId = Guid.NewGuid(),
            Name = "North Plot",
            CropType = "Soy",
            AreaHectares = 50,
            Latitude = -21.1775,
            Longitude = -47.8103,
            BoundaryGeoJson = "{\"type\":\"Polygon\"}",
            PlantingDate = DateTimeOffset.UtcNow.AddDays(-10),
            ExpectedHarvestDate = DateTimeOffset.UtcNow.AddDays(120),
            IrrigationType = "Center Pivot",
            AdditionalNotes = "Notes"
        };

    private static PropertyAggregate CreateProperty(Guid ownerId)
    {
        var result = PropertyAggregate.Create(
            "Farm A",
            "Road 1",
            "Ribeirao Preto",
            "SP",
            "Brazil",
            200,
            ownerId,
            -21.1767,
            -47.8208);

        result.IsSuccess.ShouldBeTrue();
        return result.Value;
    }

    private static PlotAggregate CreatePlot(Guid propertyId, Guid ownerId, string cropType)
    {
        var catalog = CreateCatalog(ownerId, cropType);
        var result = PlotAggregate.Create(
            propertyId: propertyId,
            ownerId: ownerId,
            name: "North Plot",
            cropType: cropType,
            areaHectares: 50,
            plantingDate: DateTimeOffset.UtcNow.AddDays(-60),
            expectedHarvestDate: DateTimeOffset.UtcNow.AddDays(120),
            irrigationType: "Center Pivot",
            additionalNotes: "Initial notes",
            latitude: -21.1775,
            longitude: -47.8103,
            boundaryGeoJson: null,
            cropTypeCatalogId: catalog.Id);

        result.IsSuccess.ShouldBeTrue();
        return result.Value;
    }

    private static CropTypeSuggestionAggregate CreateSuggestion(Guid propertyId, Guid ownerId, string cropType)
    {
        var result = CropTypeSuggestionAggregate.CreateAi(
            propertyId: propertyId,
            ownerId: ownerId,
            cropType: cropType,
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

    private static CropTypeCatalogAggregate CreateCatalog(Guid ownerId, string cropType)
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