using FakeItEasy;
using Microsoft.Extensions.Logging;
using TC.Agro.Farm.Application.Abstractions.Ports;
using TC.Agro.Farm.Application.UseCases.CropTypes.List;
using TC.Agro.Farm.Tests.TestHelpers;

namespace TC.Agro.Farm.Tests.Application.UseCases.CropTypes.List;

public sealed class ListCropTypesQueryHandlerTests
{
    private readonly ICropTypeCatalogReadStore _catalogReadStore = A.Fake<ICropTypeCatalogReadStore>();
    private readonly ICropTypeSuggestionReadStore _suggestionReadStore = A.Fake<ICropTypeSuggestionReadStore>();
    private readonly ILogger<ListCropTypesQueryHandler> _logger = A.Fake<ILogger<ListCropTypesQueryHandler>>();

    public ListCropTypesQueryHandlerTests()
    {
        FastEndpointsTestBootstrap.EnsureInitialized();
    }

    [Fact]
    public async Task ExecuteAsync_WhenIncludeSuggestionsIsFalse_ShouldUseCatalogStoreOnly()
    {
        var query = new ListCropTypesQuery
        {
            IncludeSuggestions = false,
            PageNumber = 1,
            PageSize = 10,
            SortBy = "createdAt",
            SortDirection = "desc"
        };

        var rows = new List<ListCropTypesResponse>
        {
            CreateCatalogRow("Corn")
        };

        A.CallTo(() => _catalogReadStore.ListAsync(query, A<CancellationToken>._))
            .Returns((rows, rows.Count));

        var sut = new ListCropTypesQueryHandler(
            _catalogReadStore,
            _suggestionReadStore,
            TestUserContextFactory.CreateProducer(),
            _logger);

        var result = await sut.ExecuteAsync(query, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.TotalCount.ShouldBe(1);
        result.Value.Data.Count.ShouldBe(1);
        result.Value.Data[0].CropType.ShouldBe("Corn");

        A.CallTo(() => _catalogReadStore.ListAsync(query, A<CancellationToken>._)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _suggestionReadStore.ListAsync(A<ListCropTypesQuery>._, A<CancellationToken>._)).MustNotHaveHappened();
    }

    [Fact]
    public async Task ExecuteAsync_WhenIncludeSuggestionsIsTrue_ShouldMergeCatalogAndSuggestionRowsWithoutDedup()
    {
        var query = new ListCropTypesQuery
        {
            IncludeSuggestions = true,
            PageNumber = 1,
            PageSize = 10,
            SortBy = "cropType",
            SortDirection = "asc"
        };

        var catalogRows = new List<ListCropTypesResponse>
        {
            CreateCatalogRow("Soy")
        };

        var suggestionRows = new List<ListCropTypesResponse>
        {
            CreateSuggestionRow("Soy"),
            CreateSuggestionRow("Wheat")
        };

        A.CallTo(() => _catalogReadStore.ListAsync(
                A<ListCropTypesQuery>.That.Matches(x => x.IncludeSuggestions && x.PageNumber == 1 && x.PageSize == 10),
                A<CancellationToken>._))
            .Returns((catalogRows, catalogRows.Count));

        A.CallTo(() => _suggestionReadStore.ListAsync(
                A<ListCropTypesQuery>.That.Matches(x => x.IncludeSuggestions && x.PageNumber == 1 && x.PageSize == 10),
                A<CancellationToken>._))
            .Returns((suggestionRows, suggestionRows.Count));

        var sut = new ListCropTypesQueryHandler(
            _catalogReadStore,
            _suggestionReadStore,
            TestUserContextFactory.CreateProducer(),
            _logger);

        var result = await sut.ExecuteAsync(query, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.TotalCount.ShouldBe(3);
        result.Value.Data.Count.ShouldBe(3);
        result.Value.Data.Count(x => x.CropType == "Soy").ShouldBe(2);
        result.Value.Data.Any(x => x.Source == "Catalog").ShouldBeTrue();
        result.Value.Data.Any(x => x.Source == "AI").ShouldBeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_WhenIncludeSuggestionsIsTrue_ShouldApplyPaginationAfterMerge()
    {
        var query = new ListCropTypesQuery
        {
            IncludeSuggestions = true,
            PageNumber = 2,
            PageSize = 2,
            SortBy = "cropType",
            SortDirection = "asc"
        };

        var catalogRows = new List<ListCropTypesResponse>
        {
            CreateCatalogRow("Corn"),
            CreateCatalogRow("Rice")
        };

        var suggestionRows = new List<ListCropTypesResponse>
        {
            CreateSuggestionRow("Soy"),
            CreateSuggestionRow("Wheat")
        };

        A.CallTo(() => _catalogReadStore.ListAsync(
                A<ListCropTypesQuery>.That.Matches(x => x.PageNumber == 1 && x.PageSize == 4),
                A<CancellationToken>._))
            .Returns((catalogRows, catalogRows.Count));

        A.CallTo(() => _suggestionReadStore.ListAsync(
                A<ListCropTypesQuery>.That.Matches(x => x.PageNumber == 1 && x.PageSize == 4),
                A<CancellationToken>._))
            .Returns((suggestionRows, suggestionRows.Count));

        var sut = new ListCropTypesQueryHandler(
            _catalogReadStore,
            _suggestionReadStore,
            TestUserContextFactory.CreateProducer(),
            _logger);

        var result = await sut.ExecuteAsync(query, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.TotalCount.ShouldBe(4);
        result.Value.Data.Count.ShouldBe(2);
        result.Value.Data[0].CropType.ShouldBe("Soy");
        result.Value.Data[1].CropType.ShouldBe("Wheat");
    }

    [Fact]
    public async Task ExecuteAsync_WhenIncludeSuggestionsIsTrueAndPaginationWouldOverflow_ShouldClampToMaximumPageSize()
    {
        var query = new ListCropTypesQuery
        {
            IncludeSuggestions = true,
            PageNumber = int.MaxValue,
            PageSize = int.MaxValue,
            SortBy = "cropType",
            SortDirection = "asc"
        };

        A.CallTo(() => _catalogReadStore.ListAsync(
                A<ListCropTypesQuery>.That.Matches(x => x.PageNumber == 1 && x.PageSize == 5000),
                A<CancellationToken>._))
            .Returns((Array.Empty<ListCropTypesResponse>(), 0));

        A.CallTo(() => _suggestionReadStore.ListAsync(
                A<ListCropTypesQuery>.That.Matches(x => x.PageNumber == 1 && x.PageSize == 5000),
                A<CancellationToken>._))
            .Returns((Array.Empty<ListCropTypesResponse>(), 0));

        var sut = new ListCropTypesQueryHandler(
            _catalogReadStore,
            _suggestionReadStore,
            TestUserContextFactory.CreateProducer(),
            _logger);

        var result = await sut.ExecuteAsync(query, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.TotalCount.ShouldBe(0);
        result.Value.Data.Count.ShouldBe(0);
    }

    private static ListCropTypesResponse CreateCatalogRow(string cropType)
        => new(
            Id: Guid.NewGuid(),
            PropertyId: Guid.Empty,
            OwnerId: Guid.NewGuid(),
            PropertyName: string.Empty,
            OwnerName: "System",
            CropType: cropType,
            SuggestedImage: null,
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
            CropTypeCatalogId: Guid.NewGuid(),
            SelectedCropTypeSuggestionId: null);

    private static ListCropTypesResponse CreateSuggestionRow(string cropType)
        => new(
            Id: Guid.NewGuid(),
            PropertyId: Guid.NewGuid(),
            OwnerId: Guid.NewGuid(),
            PropertyName: "Farm 01",
            OwnerName: "Owner",
            CropType: cropType,
            SuggestedImage: null,
            Source: "AI",
            IsOverride: false,
            IsStale: false,
            ConfidenceScore: 91,
            PlantingWindow: null,
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
            SelectedCropTypeSuggestionId: Guid.NewGuid());
}
