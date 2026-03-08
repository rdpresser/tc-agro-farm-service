using Ardalis.Result;
using FakeItEasy;
using Microsoft.Extensions.Logging;
using TC.Agro.Farm.Application.Abstractions.Ports;
using TC.Agro.Farm.Application.UseCases.Sensors.GetSensorById;
using TC.Agro.Farm.Tests.TestHelpers;

namespace TC.Agro.Farm.Tests.Application.UseCases.Sensors.GetSensorById;

public sealed class GetSensorByIdQueryHandlerTests
{
    private readonly ISensorReadStore _readStore = A.Fake<ISensorReadStore>();
    private readonly ILogger<GetSensorByIdQueryHandler> _logger = A.Fake<ILogger<GetSensorByIdQueryHandler>>();

    public GetSensorByIdQueryHandlerTests()
    {
        FastEndpointsTestBootstrap.EnsureInitialized();
    }

    [Fact]
    public async Task ExecuteAsync_WhenSensorDoesNotExist_ShouldReturnNotFound()
    {
        var sensorId = Guid.NewGuid();

        A.CallTo(() => _readStore.GetByIdAsync(sensorId, A<CancellationToken>._))
            .Returns((SensorByIdResponse?)null);

        var sut = new GetSensorByIdQueryHandler(_readStore, _logger);

        var result = await sut.ExecuteAsync(new GetSensorByIdQuery { Id = sensorId }, CancellationToken.None);

        result.Status.ShouldBe(ResultStatus.NotFound);
    }

    [Fact]
    public async Task ExecuteAsync_WhenSensorExists_ShouldReturnResponse()
    {
        var response = new SensorByIdResponse(
            Id: Guid.NewGuid(),
            PlotId: Guid.NewGuid(),
            PlotName: "Plot A",
            PropertyId: Guid.NewGuid(),
            PropertyName: "Farm A",
            OwnerId: Guid.NewGuid(),
            OwnerName: "Producer A",
            Type: "Temperature",
            Status: "Active",
            Label: "Temp-01",
            InstalledAt: DateTimeOffset.UtcNow.AddDays(-90),
            LastMaintenanceAt: DateTimeOffset.UtcNow.AddDays(-5));

        A.CallTo(() => _readStore.GetByIdAsync(response.Id, A<CancellationToken>._))
            .Returns(response);

        var sut = new GetSensorByIdQueryHandler(_readStore, _logger);

        var result = await sut.ExecuteAsync(new GetSensorByIdQuery { Id = response.Id }, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Id.ShouldBe(response.Id);
        result.Value.Type.ShouldBe("Temperature");
        result.Value.Status.ShouldBe("Active");
    }
}
