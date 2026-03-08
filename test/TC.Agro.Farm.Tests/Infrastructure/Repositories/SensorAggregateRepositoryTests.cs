using Microsoft.EntityFrameworkCore;
using TC.Agro.Farm.Domain.Aggregates;
using TC.Agro.Farm.Domain.Snapshots;
using TC.Agro.Farm.Infrastructure;
using TC.Agro.Farm.Infrastructure.Repositories;
using TC.Agro.Farm.Tests.TestHelpers;

namespace TC.Agro.Farm.Tests.Infrastructure.Repositories;

public sealed class SensorAggregateRepositoryTests
{
    [Fact]
    public async Task LabelExistsForPlotAsync_WhenLabelIsWhitespace_ShouldReturnFalse()
    {
        await using var dbContext = CreateDbContext();
        var sut = new SensorAggregateRepository(dbContext, TestUserContextFactory.CreateAdmin());

        var exists = await sut.LabelExistsForPlotAsync("   ", Guid.NewGuid(), CancellationToken.None);

        exists.ShouldBeFalse();
    }

    [Fact]
    public async Task LabelExistsForPlotExcludingAsync_WhenLabelIsWhitespace_ShouldReturnFalse()
    {
        await using var dbContext = CreateDbContext();
        var sut = new SensorAggregateRepository(dbContext, TestUserContextFactory.CreateAdmin());

        var exists = await sut.LabelExistsForPlotExcludingAsync(" ", Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);

        exists.ShouldBeFalse();
    }

    [Fact]
    public async Task GetByIdAsync_WhenCallerIsProducerFromDifferentOwner_ShouldReturnNull()
    {
        await using var dbContext = CreateDbContext();
        var seed = await SeedDataAsync(dbContext);

        var sut = new SensorAggregateRepository(dbContext, TestUserContextFactory.CreateProducer(seed.OwnerAId));

        var sensor = await sut.GetByIdAsync(seed.OwnerBSensorId, CancellationToken.None);

        sensor.ShouldBeNull();
    }

    [Fact]
    public async Task GetByIdAsync_WhenCallerIsAdmin_ShouldIncludePlotAndProperty()
    {
        await using var dbContext = CreateDbContext();
        var seed = await SeedDataAsync(dbContext);

        var sut = new SensorAggregateRepository(dbContext, TestUserContextFactory.CreateAdmin());

        var sensor = await sut.GetByIdAsync(seed.OwnerBSensorId, CancellationToken.None);

        sensor.ShouldNotBeNull();
        sensor!.Plot.ShouldNotBeNull();
        sensor.Plot.Property.ShouldNotBeNull();
        sensor.Plot.Property.Id.ShouldBe(seed.OwnerBPropertyId);
    }

    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"sensor-aggregate-repository-tests-{Guid.NewGuid()}")
            .Options;

        return new ApplicationDbContext(options);
    }

    private static async Task<(Guid OwnerAId, Guid OwnerBSensorId, Guid OwnerBPropertyId)> SeedDataAsync(ApplicationDbContext dbContext)
    {
        var ownerAId = Guid.NewGuid();
        var ownerBId = Guid.NewGuid();

        dbContext.OwnerSnapshots.Add(OwnerSnapshot.Create(ownerAId, "Producer A", "producer.a@tcagro.com"));
        dbContext.OwnerSnapshots.Add(OwnerSnapshot.Create(ownerBId, "Producer B", "producer.b@tcagro.com"));

        var propertyA = CreateProperty("Farm A", ownerAId);
        var propertyB = CreateProperty("Farm B", ownerBId);
        dbContext.Properties.Add(propertyA);
        dbContext.Properties.Add(propertyB);

        var plotA = CreatePlot(propertyA.Id, ownerAId, "Plot A");
        var plotB = CreatePlot(propertyB.Id, ownerBId, "Plot B");
        dbContext.Plots.Add(plotA);
        dbContext.Plots.Add(plotB);

        var sensorA = CreateSensor(ownerAId, propertyA.Id, plotA.Id, "Temperature", "A-T-01", "Farm A", "Plot A");
        var sensorB = CreateSensor(ownerBId, propertyB.Id, plotB.Id, "Humidity", "B-H-01", "Farm B", "Plot B");
        dbContext.Sensors.Add(sensorA);
        dbContext.Sensors.Add(sensorB);

        await dbContext.SaveChangesAsync(CancellationToken.None);

        return (ownerAId, sensorB.Id, propertyB.Id);
    }

    private static PropertyAggregate CreateProperty(string name, Guid ownerId)
    {
        var result = PropertyAggregate.Create(
            name: name,
            address: "Rural Road",
            city: "Franca",
            state: "SP",
            country: "Brazil",
            areaHectares: 100,
            ownerId: ownerId,
            latitude: -20.5397,
            longitude: -47.4009);

        result.IsSuccess.ShouldBeTrue();
        return result.Value;
    }

    private static PlotAggregate CreatePlot(Guid propertyId, Guid ownerId, string name)
    {
        var result = PlotAggregate.Create(
            propertyId: propertyId,
            ownerId: ownerId,
            name: name,
            cropType: "Soy",
            areaHectares: 45,
            plantingDate: DateTimeOffset.UtcNow.AddDays(-12),
            expectedHarvestDate: DateTimeOffset.UtcNow.AddDays(100),
            irrigationType: "Center Pivot",
            additionalNotes: null,
            latitude: -20.5397,
            longitude: -47.4009,
            boundaryGeoJson: null);

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
            plotLatitude: -20.5397,
            plotLongitude: -47.4009,
            plotBoundaryGeoJson: null);

        result.IsSuccess.ShouldBeTrue();
        return result.Value;
    }
}
