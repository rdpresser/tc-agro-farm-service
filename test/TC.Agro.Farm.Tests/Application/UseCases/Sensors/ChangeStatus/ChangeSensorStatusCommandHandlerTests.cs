using Ardalis.Result;
using FakeItEasy;
using Microsoft.Extensions.Logging;
using TC.Agro.Farm.Application.Abstractions.Ports;
using TC.Agro.Farm.Application.UseCases.Sensors.ChangeStatus;
using TC.Agro.Farm.Domain.Aggregates;
using TC.Agro.Farm.Tests.TestHelpers;
using TC.Agro.SharedKernel.Application.Ports;

namespace TC.Agro.Farm.Tests.Application.UseCases.Sensors.ChangeStatus;

public sealed class ChangeSensorStatusCommandHandlerTests
{
    private readonly ISensorAggregateRepository _repository = A.Fake<ISensorAggregateRepository>();
    private readonly ITransactionalOutbox _outbox = A.Fake<ITransactionalOutbox>();
    private readonly ILogger<ChangeSensorStatusCommandHandler> _logger = A.Fake<ILogger<ChangeSensorStatusCommandHandler>>();

    public ChangeSensorStatusCommandHandlerTests()
    {
        FastEndpointsTestBootstrap.EnsureInitialized();
    }

    [Fact]
    public async Task ExecuteAsync_WhenSensorDoesNotExist_ShouldReturnInvalid()
    {
        var command = new ChangeSensorStatusCommand(
            SensorId: Guid.NewGuid(),
            NewStatus: "Maintenance",
            Reason: "Scheduled maintenance");

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

        var command = new ChangeSensorStatusCommand(
            SensorId: sensor.Id,
            NewStatus: "Faulty",
            Reason: "Battery failure");

        A.CallTo(() => _repository.GetByIdAsync(sensor.Id, A<CancellationToken>._))
            .Returns(sensor);

        var sut = CreateHandler();

        var result = await sut.ExecuteAsync(command, CancellationToken.None);

        result.Status.ShouldBe(ResultStatus.Invalid);
        result.ValidationErrors.ShouldContain(error =>
            error.ErrorMessage.Contains("already deactivated", StringComparison.OrdinalIgnoreCase));

        A.CallTo(() => _outbox.SaveChangesAsync(A<CancellationToken>._)).MustNotHaveHappened();
    }

    private ChangeSensorStatusCommandHandler CreateHandler()
        => new(_repository, TestUserContextFactory.CreateProducer(), _outbox, _logger);

    private static SensorAggregate CreateSensor()
    {
        var result = SensorAggregate.Create(
            ownerId: Guid.NewGuid(),
            propertyId: Guid.NewGuid(),
            plotId: Guid.NewGuid(),
            label: "Temp-01",
            propertyName: "Farm A",
            plotName: "Plot A",
            type: "Temperature",
            plotLatitude: -21.1775,
            plotLongitude: -47.8103,
            plotBoundaryGeoJson: null);

        result.IsSuccess.ShouldBeTrue();
        return result.Value;
    }
}
