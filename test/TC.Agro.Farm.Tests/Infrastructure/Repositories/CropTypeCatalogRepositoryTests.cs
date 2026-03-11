using Microsoft.EntityFrameworkCore;
using TC.Agro.Farm.Domain.Aggregates;
using TC.Agro.Farm.Domain.Snapshots;
using TC.Agro.Farm.Infrastructure;
using TC.Agro.Farm.Infrastructure.Repositories;

namespace TC.Agro.Farm.Tests.Infrastructure.Repositories;

public sealed class CropTypeCatalogRepositoryTests
{
    [Fact]
    public async Task GetByNameAsync_WhenTenantSpecificAndSystemEntriesExist_ShouldPreferTenantSpecificEntry()
    {
        await using var dbContext = CreateDbContext();
        var seed = await SeedDataAsync(dbContext);
        var sut = new CropTypeCatalogRepository(dbContext);

        var result = await sut.GetByNameAsync("Soy", seed.OwnerAId, CancellationToken.None);

        result.ShouldNotBeNull();
        result!.Id.ShouldBe(seed.OwnerACatalogId);
        result.OwnerId.ShouldBe(seed.OwnerAId);
    }

    [Fact]
    public async Task GetByNameAsync_WhenOwnerIsNotProvided_ShouldReturnSystemEntryOnly()
    {
        await using var dbContext = CreateDbContext();
        var seed = await SeedDataAsync(dbContext);
        var sut = new CropTypeCatalogRepository(dbContext);

        var result = await sut.GetByNameAsync("Soy", cancellationToken: CancellationToken.None);

        result.ShouldNotBeNull();
        result!.Id.ShouldBe(seed.SystemCatalogId);
        result.OwnerId.ShouldBeNull();
    }

    [Fact]
    public async Task GetByIdScopedAsync_WhenCatalogBelongsToAnotherTenant_ShouldReturnNull()
    {
        await using var dbContext = CreateDbContext();
        var seed = await SeedDataAsync(dbContext);
        var sut = new CropTypeCatalogRepository(dbContext);

        var result = await sut.GetByIdScopedAsync(seed.OwnerBCatalogId, seed.OwnerAId, cancellationToken: CancellationToken.None);

        result.ShouldBeNull();
    }

    [Fact]
    public async Task GetByIdScopedAsync_WhenCatalogIsInactive_ShouldRespectIncludeInactiveFlag()
    {
        await using var dbContext = CreateDbContext();
        var seed = await SeedDataAsync(dbContext);
        var sut = new CropTypeCatalogRepository(dbContext);

        var excluded = await sut.GetByIdScopedAsync(seed.InactiveOwnerCatalogId, seed.OwnerAId, includeInactive: false, cancellationToken: CancellationToken.None);
        var included = await sut.GetByIdScopedAsync(seed.InactiveOwnerCatalogId, seed.OwnerAId, includeInactive: true, cancellationToken: CancellationToken.None);

        excluded.ShouldBeNull();
        included.ShouldNotBeNull();
        included!.Id.ShouldBe(seed.InactiveOwnerCatalogId);
    }

    [Fact]
    public async Task NameExistsAsync_WhenSystemOrTenantEntryExistsInScope_ShouldReturnTrue()
    {
        await using var dbContext = CreateDbContext();
        var seed = await SeedDataAsync(dbContext);
        var sut = new CropTypeCatalogRepository(dbContext);

        var result = await sut.NameExistsAsync("Soy", seed.OwnerAId, CancellationToken.None);

        result.ShouldBeTrue();
    }

    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"crop-type-catalog-repository-tests-{Guid.NewGuid()}")
            .Options;

        return new ApplicationDbContext(options);
    }

    private static async Task<(Guid OwnerAId, Guid OwnerBId, Guid SystemCatalogId, Guid OwnerACatalogId, Guid OwnerBCatalogId, Guid InactiveOwnerCatalogId)> SeedDataAsync(ApplicationDbContext dbContext)
    {
        var ownerAId = Guid.NewGuid();
        var ownerBId = Guid.NewGuid();

        dbContext.OwnerSnapshots.Add(OwnerSnapshot.Create(ownerAId, "Owner A", "owner.a@tcagro.com"));
        dbContext.OwnerSnapshots.Add(OwnerSnapshot.Create(ownerBId, "Owner B", "owner.b@tcagro.com"));

        var systemCatalog = CropTypeCatalogAggregate.Create("Soy", isSystemDefined: true).Value;
        var ownerACatalog = CropTypeCatalogAggregate.Create("Soy", isSystemDefined: false, ownerId: ownerAId).Value;
        var ownerBCatalog = CropTypeCatalogAggregate.Create("Barley", isSystemDefined: false, ownerId: ownerBId).Value;
        var inactiveOwnerCatalog = CropTypeCatalogAggregate.Create("Cassava", isSystemDefined: false, ownerId: ownerAId).Value;
        inactiveOwnerCatalog.Deactivate().IsSuccess.ShouldBeTrue();

        dbContext.CropTypeCatalogs.AddRange(systemCatalog, ownerACatalog, ownerBCatalog, inactiveOwnerCatalog);
        await dbContext.SaveChangesAsync(CancellationToken.None);

        return (ownerAId, ownerBId, systemCatalog.Id, ownerACatalog.Id, ownerBCatalog.Id, inactiveOwnerCatalog.Id);
    }
}
