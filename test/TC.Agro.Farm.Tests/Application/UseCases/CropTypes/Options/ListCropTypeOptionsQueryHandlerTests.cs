using FakeItEasy;
using Microsoft.Extensions.Logging;
using TC.Agro.Farm.Application.Abstractions.Ports;
using TC.Agro.Farm.Application.UseCases.CropTypes.List;
using TC.Agro.Farm.Application.UseCases.CropTypes.Options;
using TC.Agro.Farm.Tests.TestHelpers;

namespace TC.Agro.Farm.Tests.Application.UseCases.CropTypes.Options;

public sealed class ListCropTypeOptionsQueryHandlerTests
{
    private readonly ICropTypeCatalogReadStore _catalogReadStore = A.Fake<ICropTypeCatalogReadStore>();
    private readonly ICropTypeSuggestionReadStore _suggestionReadStore = A.Fake<ICropTypeSuggestionReadStore>();
    private readonly ILogger<ListCropTypeOptionsQueryHandler> _logger = A.Fake<ILogger<ListCropTypeOptionsQueryHandler>>();

    public ListCropTypeOptionsQueryHandlerTests()
    {
        FastEndpointsTestBootstrap.EnsureInitialized();
    }

    [Fact]
    public async Task ExecuteAsync_WhenReadStoreReturnsRows_ShouldMapToOptionsResponse()
    {
        var query = new ListCropTypeOptionsQuery
        {
            IncludeInactive = false,
            IncludeStale = false,
            Limit = 100
        };

        var propertyId = Guid.NewGuid();

        var cropTypeCatalogId = Guid.NewGuid();
        var suggestionId = Guid.NewGuid();

        var rows = new List<ListCropTypesResponse>
        {
            new(
                Id: suggestionId,
                PropertyId: propertyId,
                OwnerId: Guid.NewGuid(),
                PropertyName: "Farm A",
                OwnerName: "Owner A",
                CropType: "Soy",
                SuggestedImage: "soy-icon",
                Source: "AI",
                IsOverride: false,
                IsStale: false,
                ConfidenceScore: 88,
                PlantingWindow: "Sep to Nov",
                HarvestCycleMonths: 5,
                SuggestedIrrigationType: "Center Pivot",
                MinSoilMoisture: 30,
                MaxTemperature: 35,
                MinHumidity: 45,
                Notes: "AI recommendation",
                Model: "mock-openai",
                GeneratedAt: DateTimeOffset.UtcNow,
                IsActive: true,
                CreatedAt: DateTimeOffset.UtcNow,
                UpdatedAt: null,
                CropTypeCatalogId: cropTypeCatalogId,
                SelectedCropTypeSuggestionId: suggestionId)
        };

        A.CallTo(() => _catalogReadStore.ListAsync(
                A<ListCropTypesQuery>.That.Matches(x =>
                    x.PageNumber == 1 &&
                    x.PageSize == query.Limit &&
                    x.SortBy == "cropType" &&
                    x.SortDirection == "asc"),
                A<CancellationToken>._))
            .Returns((rows, rows.Count));

        var sut = new ListCropTypeOptionsQueryHandler(
            _catalogReadStore,
            _suggestionReadStore,
            TestUserContextFactory.CreateProducer(),
            _logger);

        var result = await sut.ExecuteAsync(query, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Count.ShouldBe(1);
        result.Value[0].CropType.ShouldBe("Soy");
        result.Value[0].CropTypeCatalogId.ShouldBe(cropTypeCatalogId);
        result.Value[0].SelectedCropTypeSuggestionId.ShouldBe(suggestionId);
        result.Value[0].SuggestedIrrigationType.ShouldBe("Center Pivot");
    }

    [Fact]
    public async Task ExecuteAsync_WhenLimitExceedsMaximum_ShouldClampTo500BeforeQueryingReadStore()
    {
        var query = new ListCropTypeOptionsQuery
        {
            Limit = 999
        };

        A.CallTo(() => _catalogReadStore.ListAsync(
                A<ListCropTypesQuery>.That.Matches(x => x.PageSize == 500),
                A<CancellationToken>._))
            .Returns((Array.Empty<ListCropTypesResponse>(), 0));

        var sut = new ListCropTypeOptionsQueryHandler(
            _catalogReadStore,
            _suggestionReadStore,
            TestUserContextFactory.CreateAdmin(),
            _logger);

        var result = await sut.ExecuteAsync(query, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Count.ShouldBe(0);
    }

    [Fact]
    public async Task ExecuteAsync_WhenIncludeSuggestionsIsTrue_ShouldMergeCatalogAndSuggestionRows()
    {
        var query = new ListCropTypeOptionsQuery
        {
            IncludeSuggestions = true,
            Limit = 10
        };

        var catalogId = Guid.NewGuid();
        var suggestionId = Guid.NewGuid();

        var catalogRows = new List<ListCropTypesResponse>
        {
            new(
                Id: catalogId,
                PropertyId: Guid.Empty,
                OwnerId: Guid.NewGuid(),
                PropertyName: string.Empty,
                OwnerName: "System",
                CropType: "Corn",
                SuggestedImage: "corn",
                Source: "Catalog",
                IsOverride: false,
                IsStale: false,
                ConfidenceScore: null,
                PlantingWindow: null,
                HarvestCycleMonths: 4,
                SuggestedIrrigationType: "Drip",
                MinSoilMoisture: 20,
                MaxTemperature: 34,
                MinHumidity: 40,
                Notes: null,
                Model: null,
                GeneratedAt: null,
                IsActive: true,
                CreatedAt: DateTimeOffset.UtcNow,
                UpdatedAt: null,
                CropTypeCatalogId: catalogId,
                SelectedCropTypeSuggestionId: null)
        };

        var suggestionRows = new List<ListCropTypesResponse>
        {
            new(
                Id: suggestionId,
                PropertyId: Guid.NewGuid(),
                OwnerId: Guid.NewGuid(),
                PropertyName: "Farm 01",
                OwnerName: "Owner",
                CropType: "Soy",
                SuggestedImage: "soy",
                Source: "AI",
                IsOverride: false,
                IsStale: false,
                ConfidenceScore: 91,
                PlantingWindow: "Sep to Nov",
                HarvestCycleMonths: 5,
                SuggestedIrrigationType: "Center Pivot",
                MinSoilMoisture: 30,
                MaxTemperature: 35,
                MinHumidity: 45,
                Notes: "AI recommendation",
                Model: "mock-openai",
                GeneratedAt: DateTimeOffset.UtcNow,
                IsActive: true,
                CreatedAt: DateTimeOffset.UtcNow,
                UpdatedAt: null,
                CropTypeCatalogId: Guid.Empty,
                SelectedCropTypeSuggestionId: suggestionId)
        };

        A.CallTo(() => _catalogReadStore.ListAsync(A<ListCropTypesQuery>._, A<CancellationToken>._))
            .Returns((catalogRows, catalogRows.Count));

        A.CallTo(() => _suggestionReadStore.ListAsync(A<ListCropTypesQuery>._, A<CancellationToken>._))
            .Returns((suggestionRows, suggestionRows.Count));

        var sut = new ListCropTypeOptionsQueryHandler(
            _catalogReadStore,
            _suggestionReadStore,
            TestUserContextFactory.CreateProducer(),
            _logger);

        var result = await sut.ExecuteAsync(query, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Count.ShouldBe(2);
        result.Value.Any(x => x.Id == catalogId && x.Source == "Catalog").ShouldBeTrue();
        result.Value.Any(x => x.Id == suggestionId && x.Source == "AI").ShouldBeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_WhenIncludeSuggestionsIsFalse_ShouldQueryCatalogStoreOnly()
    {
        var query = new ListCropTypeOptionsQuery
        {
            IncludeSuggestions = false,
            Limit = 50
        };

        var catalogRows = new List<ListCropTypesResponse>
        {
            new(
                Id: Guid.NewGuid(),
                PropertyId: Guid.Empty,
                OwnerId: Guid.NewGuid(),
                PropertyName: string.Empty,
                OwnerName: "System",
                CropType: "Wheat",
                SuggestedImage: null,
                Source: "Catalog",
                IsOverride: false,
                IsStale: false,
                ConfidenceScore: null,
                PlantingWindow: "Oct to Dec",
                HarvestCycleMonths: 4,
                SuggestedIrrigationType: "Sprinkler",
                MinSoilMoisture: 24,
                MaxTemperature: 33,
                MinHumidity: 38,
                Notes: null,
                Model: null,
                GeneratedAt: null,
                IsActive: true,
                CreatedAt: DateTimeOffset.UtcNow,
                UpdatedAt: null,
                CropTypeCatalogId: Guid.NewGuid(),
                SelectedCropTypeSuggestionId: null)
        };

        A.CallTo(() => _catalogReadStore.ListAsync(A<ListCropTypesQuery>._, A<CancellationToken>._))
            .Returns((catalogRows, catalogRows.Count));

        var sut = new ListCropTypeOptionsQueryHandler(
            _catalogReadStore,
            _suggestionReadStore,
            TestUserContextFactory.CreateProducer(),
            _logger);

        var result = await sut.ExecuteAsync(query, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Count.ShouldBe(1);
        result.Value[0].CropType.ShouldBe("Wheat");
        result.Value[0].Source.ShouldBe("Catalog");
        A.CallTo(() => _suggestionReadStore.ListAsync(A<ListCropTypesQuery>._, A<CancellationToken>._)).MustNotHaveHappened();
    }
}
