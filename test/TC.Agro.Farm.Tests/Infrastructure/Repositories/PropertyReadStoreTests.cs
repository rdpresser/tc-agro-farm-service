using Microsoft.EntityFrameworkCore;
using TC.Agro.Farm.Application.UseCases.Properties.List;
using TC.Agro.Farm.Domain.Aggregates;
using TC.Agro.Farm.Domain.Snapshots;
using TC.Agro.Farm.Infrastructure;
using TC.Agro.Farm.Infrastructure.Repositories;
using TC.Agro.Farm.Tests.TestHelpers;

namespace TC.Agro.Farm.Tests.Infrastructure.Repositories;

public sealed class PropertyReadStoreTests
{
    [Fact]
    public async Task GetByIdAsync_WhenCallerIsAdmin_ShouldReturnPropertyFromAnyOwner()
    {
        await using var dbContext = CreateDbContext();
        var seed = await SeedDataAsync(dbContext);

        var sut = new PropertyReadStore(dbContext, TestUserContextFactory.CreateAdmin());

        var property = await sut.GetByIdAsync(seed.OwnerBPropertyId, CancellationToken.None);

        property.ShouldNotBeNull();
        property!.Id.ShouldBe(seed.OwnerBPropertyId);
        property.OwnerId.ShouldBe(seed.OwnerBId);
        property.Name.ShouldBe("Farm B");
    }

    [Fact]
    public async Task GetByIdAsync_WhenCallerIsProducerFromDifferentOwner_ShouldReturnNull()
    {
        await using var dbContext = CreateDbContext();
        var seed = await SeedDataAsync(dbContext);

        var sut = new PropertyReadStore(dbContext, TestUserContextFactory.CreateProducer(seed.OwnerAId));

        var property = await sut.GetByIdAsync(seed.OwnerBPropertyId, CancellationToken.None);

        property.ShouldBeNull();
    }

    [Fact]
    public async Task GetPropertyListAsync_WhenCallerIsProducer_ShouldReturnOnlyOwnActiveProperties()
    {
        await using var dbContext = CreateDbContext();
        var seed = await SeedDataAsync(dbContext);

        var sut = new PropertyReadStore(dbContext, TestUserContextFactory.CreateProducer(seed.OwnerAId));

        var query = new ListPropertiesQuery
        {
            PageNumber = 1,
            PageSize = 20,
            SortBy = "name",
            SortDirection = "asc",
            Filter = null
        };

        var (properties, totalCount) = await sut.GetPropertyListAsync(query, CancellationToken.None);

        totalCount.ShouldBe(1);
        properties.Count.ShouldBe(1);
        properties[0].OwnerId.ShouldBe(seed.OwnerAId);
        properties[0].Name.ShouldBe("Farm A");
    }

    [Fact]
    public async Task GetPropertyListAsync_WhenCallerIsAdminWithOwnerFilter_ShouldReturnOnlyRequestedOwner()
    {
        await using var dbContext = CreateDbContext();
        var seed = await SeedDataAsync(dbContext);

        var sut = new PropertyReadStore(dbContext, TestUserContextFactory.CreateAdmin());

        var query = new ListPropertiesQuery
        {
            OwnerId = seed.OwnerBId,
            PageNumber = 1,
            PageSize = 20,
            SortBy = "name",
            SortDirection = "asc",
            Filter = null
        };

        var (properties, totalCount) = await sut.GetPropertyListAsync(query, CancellationToken.None);

        totalCount.ShouldBe(1);
        properties.Count.ShouldBe(1);
        properties[0].OwnerId.ShouldBe(seed.OwnerBId);
        properties[0].Name.ShouldBe("Farm B");
    }

    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"property-read-store-tests-{Guid.NewGuid()}")
            .Options;

        return new ApplicationDbContext(options);
    }

    private static async Task<(Guid OwnerAId, Guid OwnerBId, Guid OwnerBPropertyId)> SeedDataAsync(ApplicationDbContext dbContext)
    {
        var ownerAId = Guid.NewGuid();
        var ownerBId = Guid.NewGuid();

        dbContext.OwnerSnapshots.Add(OwnerSnapshot.Create(ownerAId, "Producer A", "producer.a@tcagro.com"));
        dbContext.OwnerSnapshots.Add(OwnerSnapshot.Create(ownerBId, "Producer B", "producer.b@tcagro.com"));

        var ownerAProperty = CreateProperty("Farm A", ownerAId);
        var ownerAInactiveProperty = CreateProperty("Farm A Inactive", ownerAId);
        ownerAInactiveProperty.Deactivate().IsSuccess.ShouldBeTrue();

        var ownerBProperty = CreateProperty("Farm B", ownerBId);

        dbContext.Properties.Add(ownerAProperty);
        dbContext.Properties.Add(ownerAInactiveProperty);
        dbContext.Properties.Add(ownerBProperty);

        await dbContext.SaveChangesAsync(CancellationToken.None);

        return (ownerAId, ownerBId, ownerBProperty.Id);
    }

    private static PropertyAggregate CreateProperty(string name, Guid ownerId)
    {
        var result = PropertyAggregate.Create(
            name: name,
            address: "Rural Road",
            city: "Ribeirao Preto",
            state: "SP",
            country: "Brazil",
            areaHectares: 100,
            ownerId: ownerId,
            latitude: -21.1775,
            longitude: -47.8103);

        result.IsSuccess.ShouldBeTrue();
        return result.Value;
    }
}
