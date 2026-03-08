using Ardalis.Result;
using FakeItEasy;
using Microsoft.Extensions.Logging;
using TC.Agro.Farm.Application.Abstractions.Ports;
using TC.Agro.Farm.Application.UseCases.Sensors.Deactivate;
using TC.Agro.Farm.Domain.Aggregates;
using TC.Agro.Farm.Tests.TestHelpers;
using TC.Agro.SharedKernel.Application.Ports;

namespace TC.Agro.Farm.Tests.Application.UseCases.Sensors.Deactivate;

public sealed class DeactivateSensorCommandHandlerTests
{
    private readonly ISensorAggregateRepository _repository = A.Fake<ISensorAggregateRepository>();
    private readonly ITransactionalOutbox _outbox = A.Fake<ITransactionalOutbox>();
    private readonly ILogger<DeactivateSensorCommandHandler> _logger = A.Fake<ILogger<DeactivateSensorCommandHandler>>();

    public DeactivateSensorCommandHandlerTests()
    {
        FastEndpointsTestBootstrap.EnsureInitialized();
    }

    [Fact]
    public async Task ExecuteAsync_WhenSensorDoesNotExist_ShouldReturnInvalid()
    {
        var command = new DeactivateSensorCommand(
            SensorId: Guid.NewGuid(),
            Reason: "Device removed");

        A.CallTo(() => _repository.GetByIdAsync(command.SensorId, A<CancellationToken>._))
            .Returns(Task.FromResult<SensorAggregate?>(null));

        var sut = CreateHandler();

        var result = await sut.ExecuteAsync(command, CancellationToken.None);

        result.Status.ShouldBe(ResultStatus.Invalid);
        result.ValidationErrors.ShouldContain(error =>
            error.ErrorMessage.Contains("Sensor not found", StringComparison.OrdinalIgnoreCase));

        A.CallTo(() => _outbox.SaveChangesAsync(A<CancellationToken>._)).MustNotHaveHappened();
    }

    [Fact]
    public async Task ExecuteAsync_WhenSensorAlreadyDeactivated_ShouldReturnInvalid()
    {
        var sensor = CreateSensor();
        sensor.Deactivate().IsSuccess.ShouldBeTrue();

        var command = new DeactivateSensorCommand(
            SensorId: sensor.Id,
            Reason: "Already removed");

        A.CallTo(() => _repository.GetByIdAsync(sensor.Id, A<CancellationToken>._))
            .Returns(sensor);

        var sut = CreateHandler();

        var result = await sut.ExecuteAsync(command, CancellationToken.None);

        result.Status.ShouldBe(ResultStatus.Invalid);
        result.ValidationErrors.ShouldContain(error =>
            error.ErrorMessage.Contains("already deactivated", StringComparison.OrdinalIgnoreCase));

        A.CallTo(() => _outbox.SaveChangesAsync(A<CancellationToken>._)).MustNotHaveHappened();
    }

    private DeactivateSensorCommandHandler CreateHandler()
        => new(_repository, TestUserContextFactory.CreateProducer(), _outbox, _logger);

    private static SensorAggregate CreateSensor()
    {
        var result = SensorAggregate.Create(
            ownerId: Guid.NewGuid(),
            propertyId: Guid.NewGuid(),
            plotId: Guid.NewGuid(),
            label: "Hum-01",
            propertyName: "Farm B",
            plotName: "Plot B",
            type: "Humidity",
            plotLatitude: -20.5397,
            plotLongitude: -47.4009,
            plotBoundaryGeoJson: null);

        result.IsSuccess.ShouldBeTrue();
        return result.Value;
    }
}
