using FakeItEasy;
using Microsoft.Extensions.Logging;
using TC.Agro.Farm.Application.Abstractions.Ports;
using TC.Agro.Farm.Application.UseCases.CropTypes.List;
using TC.Agro.Farm.Application.UseCases.CropTypes.Options;
using TC.Agro.Farm.Tests.TestHelpers;

namespace TC.Agro.Farm.Tests.Application.UseCases.CropTypes.Options;

public sealed class ListCropTypeOptionsQueryHandlerTests
{
    private readonly ICropTypeCatalogReadStore _readStore = A.Fake<ICropTypeCatalogReadStore>();
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

        A.CallTo(() => _readStore.ListAsync(
                A<ListCropTypesQuery>.That.Matches(x =>
                    x.PageNumber == 1 &&
                    x.PageSize == query.Limit &&
                    x.SortBy == "cropType" &&
                    x.SortDirection == "asc"),
                A<CancellationToken>._))
            .Returns((rows, rows.Count));

        var sut = new ListCropTypeOptionsQueryHandler(_readStore, TestUserContextFactory.CreateProducer(), _logger);

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

        A.CallTo(() => _readStore.ListAsync(
                A<ListCropTypesQuery>.That.Matches(x => x.PageSize == 500),
                A<CancellationToken>._))
            .Returns((Array.Empty<ListCropTypesResponse>(), 0));

        var sut = new ListCropTypeOptionsQueryHandler(_readStore, TestUserContextFactory.CreateAdmin(), _logger);

        var result = await sut.ExecuteAsync(query, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Count.ShouldBe(0);
    }
}
