using Microsoft.EntityFrameworkCore;
using TC.Agro.Farm.Domain.Aggregates;
using TC.Agro.Farm.Domain.Snapshots;
using TC.Agro.Farm.Infrastructure;
using TC.Agro.Farm.Infrastructure.Repositories;
using TC.Agro.Farm.Tests.TestHelpers;

namespace TC.Agro.Farm.Tests.Infrastructure.Repositories;

public sealed class CropCycleAggregateRepositoryTests
{
    [Fact]
    public async Task HasActiveCyclesByPropertyAsync_WhenPropertyHasActiveCycle_ShouldReturnTrue()
    {
        await using var dbContext = CreateDbContext();
        var seed = await SeedDataAsync(dbContext, completeCycle: false);

        var sut = new CropCycleAggregateRepository(dbContext, TestUserContextFactory.CreateProducer(seed.OwnerId));

        var result = await sut.HasActiveCyclesByPropertyAsync(seed.PropertyId, CancellationToken.None);

        result.ShouldBeTrue();
    }

    [Fact]
    public async Task HasActiveCyclesByPropertyAsync_WhenOnlyCompletedCyclesExist_ShouldReturnFalse()
    {
        await using var dbContext = CreateDbContext();
        var seed = await SeedDataAsync(dbContext, completeCycle: true);

        var sut = new CropCycleAggregateRepository(dbContext, TestUserContextFactory.CreateProducer(seed.OwnerId));

        var result = await sut.HasActiveCyclesByPropertyAsync(seed.PropertyId, CancellationToken.None);

        result.ShouldBeFalse();
    }

    [Fact]
    public async Task HasActiveCyclesByPlotAsync_WhenPlotHasActiveCycle_ShouldReturnTrue()
    {
        await using var dbContext = CreateDbContext();
        var seed = await SeedDataAsync(dbContext, completeCycle: false);

        var sut = new CropCycleAggregateRepository(dbContext, TestUserContextFactory.CreateProducer(seed.OwnerId));

        var result = await sut.HasActiveCyclesByPlotAsync(seed.PlotId, cancellationToken: CancellationToken.None);

        result.ShouldBeTrue();
    }

    [Fact]
    public async Task HasActiveCyclesByPlotAsync_WhenExcludingCurrentActiveCycle_ShouldReturnFalse()
    {
        await using var dbContext = CreateDbContext();
        var seed = await SeedDataAsync(dbContext, completeCycle: false);

        var sut = new CropCycleAggregateRepository(dbContext, TestUserContextFactory.CreateProducer(seed.OwnerId));

        var result = await sut.HasActiveCyclesByPlotAsync(
            seed.PlotId,
            seed.CropCycleId,
            CancellationToken.None);

        result.ShouldBeFalse();
    }

    [Fact]
    public async Task HasActiveCyclesByPlotAsync_WhenOnlyCompletedCyclesExist_ShouldReturnFalse()
    {
        await using var dbContext = CreateDbContext();
        var seed = await SeedDataAsync(dbContext, completeCycle: true);

        var sut = new CropCycleAggregateRepository(dbContext, TestUserContextFactory.CreateProducer(seed.OwnerId));

        var result = await sut.HasActiveCyclesByPlotAsync(seed.PlotId, cancellationToken: CancellationToken.None);

        result.ShouldBeFalse();
    }

    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"crop-cycle-repository-tests-{Guid.NewGuid()}")
            .Options;

        return new ApplicationDbContext(options);
    }

    private static async Task<(Guid OwnerId, Guid PropertyId, Guid PlotId, Guid CropCycleId)> SeedDataAsync(
        ApplicationDbContext dbContext,
        bool completeCycle)
    {
        var ownerId = Guid.NewGuid();
        dbContext.OwnerSnapshots.Add(OwnerSnapshot.Create(ownerId, "Owner A", "owner.a@tcagro.com"));

        var propertyResult = PropertyAggregate.Create(
            name: "Farm A",
            address: "Road 10",
            city: "Ribeirao Preto",
            state: "SP",
            country: "Brazil",
            areaHectares: 100,
            ownerId: ownerId,
            latitude: -21.1775,
            longitude: -47.8103);
        propertyResult.IsSuccess.ShouldBeTrue();
        var property = propertyResult.Value;
        dbContext.Properties.Add(property);

        var catalogResult = CropTypeCatalogAggregate.Create(cropTypeName: "Soy", isSystemDefined: true);
        catalogResult.IsSuccess.ShouldBeTrue();
        var catalog = catalogResult.Value;
        dbContext.CropTypeCatalogs.Add(catalog);

        var plotResult = PlotAggregate.Create(
            propertyId: property.Id,
            ownerId: ownerId,
            name: "Plot A",
            cropType: "Soy",
            areaHectares: 20,
            plantingDate: DateTimeOffset.UtcNow.AddDays(-30),
            expectedHarvestDate: DateTimeOffset.UtcNow.AddDays(90),
            irrigationType: "Center Pivot",
            additionalNotes: null,
            latitude: -21.1775,
            longitude: -47.8103,
            boundaryGeoJson: null,
            cropTypeCatalogId: catalog.Id);
        plotResult.IsSuccess.ShouldBeTrue();
        var plot = plotResult.Value;
        dbContext.Plots.Add(plot);

        var cropCycleResult = CropCycleAggregate.Start(
            plot.Id,
            property.Id,
            ownerId,
            catalog.Id,
            DateTimeOffset.UtcNow.AddDays(-15),
            expectedHarvestDate: DateTimeOffset.UtcNow.AddDays(75));
        cropCycleResult.IsSuccess.ShouldBeTrue();
        var cropCycle = cropCycleResult.Value;

        if (completeCycle)
        {
            cropCycle.Complete(DateTimeOffset.UtcNow.AddDays(-1)).IsSuccess.ShouldBeTrue();
        }

        // These repository tests validate active-cycle filtering; lifecycle event persistence is covered separately.
        cropCycle.Events.Clear();
        dbContext.CropCycles.Add(cropCycle);

        await dbContext.SaveChangesAsync(CancellationToken.None);

        return (ownerId, property.Id, plot.Id, cropCycle.Id);
    }
}
