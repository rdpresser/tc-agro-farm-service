using Microsoft.EntityFrameworkCore;
using TC.Agro.Farm.Application.UseCases.Sensors.ListAll;
using TC.Agro.Farm.Domain.Aggregates;
using TC.Agro.Farm.Domain.Snapshots;
using TC.Agro.Farm.Infrastructure;
using TC.Agro.Farm.Infrastructure.Repositories;
using TC.Agro.Farm.Tests.TestHelpers;

namespace TC.Agro.Farm.Tests.Infrastructure.Repositories;

public sealed class SensorReadStoreTests
{
    [Fact]
    public async Task GetByIdAsync_WhenCallerIsAdmin_ShouldReturnSensorFromAnyOwner()
    {
        await using var dbContext = CreateDbContext();
        var seed = await SeedDataAsync(dbContext);

        var sut = new SensorReadStore(dbContext, TestUserContextFactory.CreateAdmin());

        var sensor = await sut.GetByIdAsync(seed.OwnerBSensorId, CancellationToken.None);

        sensor.ShouldNotBeNull();
        sensor!.Id.ShouldBe(seed.OwnerBSensorId);
        sensor.OwnerId.ShouldBe(seed.OwnerBId);
        sensor.PropertyName.ShouldBe("Farm B");
        sensor.PlotName.ShouldBe("Plot B");
    }

    [Fact]
    public async Task GetByIdAsync_WhenCallerIsProducerFromDifferentOwner_ShouldReturnNull()
    {
        await using var dbContext = CreateDbContext();
        var seed = await SeedDataAsync(dbContext);

        var sut = new SensorReadStore(dbContext, TestUserContextFactory.CreateProducer(seed.OwnerAId));

        var sensor = await sut.GetByIdAsync(seed.OwnerBSensorId, CancellationToken.None);

        sensor.ShouldBeNull();
    }

    [Fact]
    public async Task ListSensorsAsync_WhenCallerIsProducer_ShouldReturnOnlyOwnActiveSensors()
    {
        await using var dbContext = CreateDbContext();
        var seed = await SeedDataAsync(dbContext);

        var sut = new SensorReadStore(dbContext, TestUserContextFactory.CreateProducer(seed.OwnerAId));

        var query = new ListSensorsQuery
        {
            PageNumber = 1,
            PageSize = 20,
            SortBy = "installedAt",
            SortDirection = "desc",
            Filter = null,
            Type = null,
            Status = null
        };

        var (sensors, totalCount) = await sut.ListSensorsAsync(query, CancellationToken.None);

        totalCount.ShouldBe(1);
        sensors.Count.ShouldBe(1);
        sensors[0].OwnerId.ShouldBe(seed.OwnerAId);
        sensors[0].PropertyName.ShouldBe("Farm A");
    }

    [Fact]
    public async Task ListSensorsAsync_WhenCallerIsAdminWithOwnerFilter_ShouldReturnOnlyRequestedOwner()
    {
        await using var dbContext = CreateDbContext();
        var seed = await SeedDataAsync(dbContext);

        var sut = new SensorReadStore(dbContext, TestUserContextFactory.CreateAdmin());

        var query = new ListSensorsQuery
        {
            OwnerId = seed.OwnerBId,
            PageNumber = 1,
            PageSize = 20,
            SortBy = "installedAt",
            SortDirection = "desc"
        };

        var (sensors, totalCount) = await sut.ListSensorsAsync(query, CancellationToken.None);

        totalCount.ShouldBe(1);
        sensors.Count.ShouldBe(1);
        sensors[0].OwnerId.ShouldBe(seed.OwnerBId);
        sensors[0].PropertyName.ShouldBe("Farm B");
    }

    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"sensor-read-store-tests-{Guid.NewGuid()}")
            .Options;

        return new ApplicationDbContext(options);
    }

    private static async Task<(Guid OwnerAId, Guid OwnerBId, Guid OwnerBSensorId)> SeedDataAsync(ApplicationDbContext dbContext)
    {
        var ownerAId = Guid.NewGuid();
        var ownerBId = Guid.NewGuid();

        dbContext.OwnerSnapshots.Add(OwnerSnapshot.Create(ownerAId, "Producer A", "producer.a@tcagro.com"));
        dbContext.OwnerSnapshots.Add(OwnerSnapshot.Create(ownerBId, "Producer B", "producer.b@tcagro.com"));

        var propertyA = CreateProperty("Farm A", ownerAId);
        var propertyB = CreateProperty("Farm B", ownerBId);
        dbContext.Properties.Add(propertyA);
        dbContext.Properties.Add(propertyB);

        var cropCatalogA = CreateCropCatalog("Soy");
        var cropCatalogB = CreateCropCatalog("Corn");
        dbContext.CropTypeCatalogs.Add(cropCatalogA);
        dbContext.CropTypeCatalogs.Add(cropCatalogB);

        var plotA = CreatePlot(propertyA.Id, ownerAId, "Plot A", cropCatalogA.Id);
        var plotB = CreatePlot(propertyB.Id, ownerBId, "Plot B", cropCatalogB.Id);
        dbContext.Plots.Add(plotA);
        dbContext.Plots.Add(plotB);

        var ownerAActiveSensor = CreateSensor(ownerAId, propertyA.Id, plotA.Id, "Temperature", "A-T-01", "Farm A", "Plot A");
        var ownerAInactiveSensor = CreateSensor(ownerAId, propertyA.Id, plotA.Id, "Humidity", "A-H-02", "Farm A", "Plot A");
        ownerAInactiveSensor.Deactivate().IsSuccess.ShouldBeTrue();

        var ownerBSensor = CreateSensor(ownerBId, propertyB.Id, plotB.Id, "SoilMoisture", "B-SM-01", "Farm B", "Plot B");

        dbContext.Sensors.Add(ownerAActiveSensor);
        dbContext.Sensors.Add(ownerAInactiveSensor);
        dbContext.Sensors.Add(ownerBSensor);

        await dbContext.SaveChangesAsync(CancellationToken.None);

        return (ownerAId, ownerBId, ownerBSensor.Id);
    }

    private static PropertyAggregate CreateProperty(string name, Guid ownerId)
    {
        var result = PropertyAggregate.Create(
            name: name,
            address: "Rural Road",
            city: "Ribeirao Preto",
            state: "SP",
            country: "Brazil",
            areaHectares: 120,
            ownerId: ownerId,
            latitude: -21.1775,
            longitude: -47.8103);

        result.IsSuccess.ShouldBeTrue();
        return result.Value;
    }

    private static PlotAggregate CreatePlot(Guid propertyId, Guid ownerId, string name, Guid cropTypeCatalogId)
    {
        var result = PlotAggregate.Create(
            propertyId: propertyId,
            ownerId: ownerId,
            name: name,
            cropType: "Soy",
            areaHectares: 60,
            plantingDate: DateTimeOffset.UtcNow.AddDays(-20),
            expectedHarvestDate: DateTimeOffset.UtcNow.AddDays(120),
            irrigationType: "Center Pivot",
            additionalNotes: null,
            latitude: -21.1775,
            longitude: -47.8103,
            boundaryGeoJson: null,
            cropTypeCatalogId: cropTypeCatalogId);

        result.IsSuccess.ShouldBeTrue();
        return result.Value;
    }

    private static CropTypeCatalogAggregate CreateCropCatalog(string cropType)
    {
        var result = CropTypeCatalogAggregate.Create(cropTypeName: cropType, isSystemDefined: true);
        result.IsSuccess.ShouldBeTrue();
        return result.Value;
    }

    private static SensorAggregate CreateSensor(
        Guid ownerId,
        Guid propertyId,
        Guid plotId,
        string type,
        string label,
        string propertyName,
        string plotName)
    {
        var result = SensorAggregate.Create(
            ownerId: ownerId,
            propertyId: propertyId,
            plotId: plotId,
            label: label,
            propertyName: propertyName,
            plotName: plotName,
            type: type,
            plotLatitude: -21.1775,
            plotLongitude: -47.8103,
            plotBoundaryGeoJson: null);

        result.IsSuccess.ShouldBeTrue();
        return result.Value;
    }
}
