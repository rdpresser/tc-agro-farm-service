using Microsoft.EntityFrameworkCore;
using TC.Agro.Farm.Application.UseCases.CropTypes.List;
using TC.Agro.Farm.Domain.Aggregates;
using TC.Agro.Farm.Domain.Snapshots;
using TC.Agro.Farm.Infrastructure;
using TC.Agro.Farm.Infrastructure.Repositories;
using TC.Agro.Farm.Tests.TestHelpers;

namespace TC.Agro.Farm.Tests.Infrastructure.Repositories;

public sealed class CropTypeCatalogReadStoreTests
{
    [Fact]
    public async Task ListAsync_WhenCallerIsProducer_ShouldReturnGlobalAndOwnTenantCatalogEntries()
    {
        await using var dbContext = CreateDbContext();
        var seed = await SeedDataAsync(dbContext);

        var sut = new CropTypeCatalogReadStore(dbContext, TestUserContextFactory.CreateProducer(seed.OwnerAId));

        var query = new ListCropTypesQuery
        {
            PageNumber = 1,
            PageSize = 20,
            SortBy = "cropType",
            SortDirection = "asc"
        };

        var (cropTypes, totalCount) = await sut.ListAsync(query, CancellationToken.None);

        totalCount.ShouldBe(3);
        cropTypes.Count.ShouldBe(3);
        cropTypes.ShouldContain(item => item.CropType == "Corn" && item.CropTypeCatalogId == seed.GlobalCatalogId);
        cropTypes.ShouldContain(item => item.CropType == "Dragon Fruit" && item.CropTypeCatalogId == seed.OwnerACatalogId);
        cropTypes.ShouldContain(item => item.CropType == "Sorghum" && item.CropTypeCatalogId == seed.SuggestionCatalogId);
        cropTypes.ShouldNotContain(item => item.CropType == "Barley");
    }

    [Fact]
    public async Task ListAsync_WhenOwnerHasProperty_ShouldPopulatePropertyContextForOwnerScopedCatalogEntries()
    {
        await using var dbContext = CreateDbContext();
        var seed = await SeedDataAsync(dbContext);

        var sut = new CropTypeCatalogReadStore(dbContext, TestUserContextFactory.CreateProducer(seed.OwnerAId));

        var query = new ListCropTypesQuery
        {
            PageNumber = 1,
            PageSize = 20,
            SortBy = "createdAt",
            SortDirection = "desc"
        };

        var (cropTypes, _) = await sut.ListAsync(query, CancellationToken.None);

        var sorghum = cropTypes.Single(item => item.CropType == "Sorghum");
        sorghum.Id.ShouldBe(seed.SuggestionCatalogId);
        sorghum.CropTypeCatalogId.ShouldBe(seed.SuggestionCatalogId);
        sorghum.SelectedCropTypeSuggestionId.ShouldBeNull();
        sorghum.PropertyId.ShouldBe(seed.PropertyId);
        sorghum.Source.ShouldBe("Catalog");

        var corn = cropTypes.Single(item => item.CropType == "Corn");
        corn.CropTypeCatalogId.ShouldBe(seed.GlobalCatalogId);
        corn.PropertyId.ShouldBe(Guid.Empty);
        corn.SelectedCropTypeSuggestionId.ShouldBeNull();
    }

    [Fact]
    public async Task GetByIdAsync_WhenGivenSuggestionId_ShouldReturnCatalogEnrichedResponse()
    {
        await using var dbContext = CreateDbContext();
        var seed = await SeedDataAsync(dbContext);

        var sut = new CropTypeCatalogReadStore(dbContext, TestUserContextFactory.CreateProducer(seed.OwnerAId));

        var response = await sut.GetByIdAsync(seed.SuggestionId, cancellationToken: CancellationToken.None);

        response.ShouldNotBeNull();
        response!.Id.ShouldBe(seed.SuggestionId);
        response.CropTypeCatalogId.ShouldBe(seed.SuggestionCatalogId);
        response.SelectedCropTypeSuggestionId.ShouldBe(seed.SuggestionId);
        response.PropertyId.ShouldBe(seed.PropertyId);
        response.CropType.ShouldBe("Sorghum");
    }

    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"crop-type-catalog-read-store-tests-{Guid.NewGuid()}")
            .Options;

        return new ApplicationDbContext(options);
    }

    private static async Task<(Guid OwnerAId, Guid OwnerBId, Guid PropertyId, Guid GlobalCatalogId, Guid OwnerACatalogId, Guid SuggestionCatalogId, Guid SuggestionId)> SeedDataAsync(ApplicationDbContext dbContext)
    {
        var ownerAId = Guid.NewGuid();
        var ownerBId = Guid.NewGuid();

        dbContext.OwnerSnapshots.Add(OwnerSnapshot.Create(ownerAId, "Owner A", "owner.a@tcagro.com"));
        dbContext.OwnerSnapshots.Add(OwnerSnapshot.Create(ownerBId, "Owner B", "owner.b@tcagro.com"));

        var globalCatalog = CropTypeCatalogAggregate.Create("Corn").Value;
        var ownerCatalog = CropTypeCatalogAggregate.Create("Dragon Fruit", isSystemDefined: false, ownerId: ownerAId).Value;
        var suggestionCatalog = CropTypeCatalogAggregate.Create("Sorghum", isSystemDefined: false, ownerId: ownerAId).Value;
        var otherOwnerCatalog = CropTypeCatalogAggregate.Create("Barley", isSystemDefined: false, ownerId: ownerBId).Value;

        dbContext.CropTypeCatalogs.AddRange(globalCatalog, ownerCatalog, suggestionCatalog, otherOwnerCatalog);

        var property = PropertyAggregate.Create(
            name: "Farm A",
            address: "Road 9",
            city: "Ribeirao Preto",
            state: "SP",
            country: "Brazil",
            areaHectares: 180,
            ownerId: ownerAId,
            latitude: -21.1775,
            longitude: -47.8103).Value;

        dbContext.Properties.Add(property);

        var suggestion = CropTypeSuggestionAggregate.CreateAi(
            propertyId: property.Id,
            ownerId: ownerAId,
            cropType: "Sorghum",
            confidenceScore: 88,
            plantingWindow: "Late spring",
            harvestCycleMonths: 4,
            suggestedIrrigationType: "Sprinkler",
            minSoilMoisture: 26,
            maxTemperature: 37,
            minHumidity: 38,
            notes: "AI recommendation",
            model: "mock-openai",
            generatedAt: DateTimeOffset.UtcNow).Value;

        dbContext.CropTypeSuggestions.Add(suggestion);

        await dbContext.SaveChangesAsync(CancellationToken.None);

        return (
            ownerAId,
            ownerBId,
            property.Id,
            globalCatalog.Id,
            ownerCatalog.Id,
            suggestionCatalog.Id,
            suggestion.Id);
    }
}
