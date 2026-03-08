using Ardalis.Result;
using FakeItEasy;
using Microsoft.Extensions.Logging;
using TC.Agro.Contracts.Events.Farm;
using TC.Agro.Farm.Application.Abstractions.Ports;
using TC.Agro.Farm.Application.UseCases.Plots.GetById;
using TC.Agro.Farm.Application.UseCases.Sensors.Create;
using TC.Agro.Farm.Domain.Aggregates;
using TC.Agro.Farm.Tests.TestHelpers;
using TC.Agro.SharedKernel.Application.Ports;
using TC.Agro.SharedKernel.Infrastructure.Messaging;
using TC.Agro.SharedKernel.Infrastructure.UserClaims;

namespace TC.Agro.Farm.Tests.Application.UseCases.Sensors.Create;

public sealed class CreateSensorCommandHandlerTests
{
    private readonly ISensorAggregateRepository _sensorRepository = A.Fake<ISensorAggregateRepository>();
    private readonly IPlotAggregateRepository _plotRepository = A.Fake<IPlotAggregateRepository>();
    private readonly IPlotReadStore _plotReadStore = A.Fake<IPlotReadStore>();
    private readonly IPropertyAggregateRepository _propertyRepository = A.Fake<IPropertyAggregateRepository>();
    private readonly ITransactionalOutbox _outbox = A.Fake<ITransactionalOutbox>();
    private readonly ILogger<CreateSensorCommandHandler> _logger = A.Fake<ILogger<CreateSensorCommandHandler>>();

    public CreateSensorCommandHandlerTests()
    {
        FastEndpointsTestBootstrap.EnsureInitialized();
    }

    [Fact]
    public async Task ExecuteAsync_WhenAdminOmitsOwnerId_ShouldReturnInvalid()
    {
        var userContext = TestUserContextFactory.CreateAdmin();
        var command = new CreateSensorCommand(
            PlotId: Guid.NewGuid(),
            Type: "Temperature",
            Label: "T-01",
            OwnerId: null);

        var sut = CreateHandler(userContext);

        var result = await sut.ExecuteAsync(command, CancellationToken.None);

        result.Status.ShouldBe(ResultStatus.Invalid);
        result.ValidationErrors.ShouldContain(error =>
            error.ErrorMessage.Contains("OwnerId is required", StringComparison.OrdinalIgnoreCase));

        A.CallTo(() => _plotReadStore.GetByIdAsync(A<Guid>._, A<CancellationToken>._)).MustNotHaveHappened();
        A.CallTo(() => _sensorRepository.Add(A<SensorAggregate>._)).MustNotHaveHappened();
        A.CallTo(() => _outbox.SaveChangesAsync(A<CancellationToken>._)).MustNotHaveHappened();
    }

    [Fact]
    public async Task ExecuteAsync_WhenPlotIsNotFound_ShouldReturnInvalid()
    {
        var userId = Guid.NewGuid();
        var userContext = TestUserContextFactory.CreateProducer(userId);
        var command = new CreateSensorCommand(
            PlotId: Guid.NewGuid(),
            Type: "Temperature",
            Label: "T-02",
            OwnerId: null);

        A.CallTo(() => _plotReadStore.GetByIdAsync(command.PlotId, A<CancellationToken>._))
            .Returns((GetPlotByIdResponse?)null);

        var sut = CreateHandler(userContext);

        var result = await sut.ExecuteAsync(command, CancellationToken.None);

        result.Status.ShouldBe(ResultStatus.Invalid);
        result.ValidationErrors.ShouldContain(error =>
            error.ErrorMessage.Contains("Plot not found", StringComparison.OrdinalIgnoreCase));

        A.CallTo(() => _propertyRepository.GetByIdAsync(A<Guid>._, A<CancellationToken>._)).MustNotHaveHappened();
    }

    [Fact]
    public async Task ExecuteAsync_WhenPropertyIsNotFound_ShouldReturnInvalid()
    {
        var ownerId = Guid.NewGuid();
        var propertyId = Guid.NewGuid();
        var command = new CreateSensorCommand(
            PlotId: Guid.NewGuid(),
            Type: "Humidity",
            Label: "H-01",
            OwnerId: null);

        A.CallTo(() => _plotReadStore.GetByIdAsync(command.PlotId, A<CancellationToken>._))
            .Returns(CreatePlotResponse(command.PlotId, propertyId, ownerId));
        A.CallTo(() => _propertyRepository.GetByIdAsync(propertyId, A<CancellationToken>._))
            .Returns(Task.FromResult<PropertyAggregate?>(null));

        var sut = CreateHandler(TestUserContextFactory.CreateProducer(ownerId));

        var result = await sut.ExecuteAsync(command, CancellationToken.None);

        result.Status.ShouldBe(ResultStatus.Invalid);
        result.ValidationErrors.ShouldContain(error =>
            error.ErrorMessage.Contains("Property not found", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ExecuteAsync_WhenOwnerDoesNotMatchPlotPropertyOwner_ShouldReturnInvalid()
    {
        var callerId = Guid.NewGuid();
        var propertyOwnerId = Guid.NewGuid();
        var propertyId = Guid.NewGuid();
        var command = new CreateSensorCommand(
            PlotId: Guid.NewGuid(),
            Type: "SoilMoisture",
            Label: "SM-01",
            OwnerId: null);

        A.CallTo(() => _plotReadStore.GetByIdAsync(command.PlotId, A<CancellationToken>._))
            .Returns(CreatePlotResponse(command.PlotId, propertyId, propertyOwnerId));
        A.CallTo(() => _propertyRepository.GetByIdAsync(propertyId, A<CancellationToken>._))
            .Returns(CreateProperty(propertyOwnerId));

        var sut = CreateHandler(TestUserContextFactory.CreateProducer(callerId));

        var result = await sut.ExecuteAsync(command, CancellationToken.None);

        result.Status.ShouldBe(ResultStatus.Invalid);
        result.ValidationErrors.ShouldContain(error =>
            error.ErrorMessage.Contains("does not match the property owner", StringComparison.OrdinalIgnoreCase));

        A.CallTo(() => _sensorRepository.Add(A<SensorAggregate>._)).MustNotHaveHappened();
    }

    [Fact]
    public async Task ExecuteAsync_WhenCommandIsValid_ShouldPersistAndPublishIntegrationEvent()
    {
        var ownerId = Guid.NewGuid();
        var propertyId = Guid.NewGuid();
        var plotId = Guid.NewGuid();

        var command = new CreateSensorCommand(
            PlotId: plotId,
            Type: "Temperature",
            Label: "Temp-01",
            OwnerId: null);

        SensorAggregate? addedAggregate = null;
        EventContext<SensorRegisteredIntegrationEvent>? publishedEvent = null;

        A.CallTo(() => _plotReadStore.GetByIdAsync(plotId, A<CancellationToken>._))
            .Returns(CreatePlotResponse(plotId, propertyId, ownerId));
        A.CallTo(() => _propertyRepository.GetByIdAsync(propertyId, A<CancellationToken>._))
            .Returns(CreateProperty(ownerId));
        A.CallTo(() => _plotRepository.GetByIdAsync(plotId, A<CancellationToken>._))
            .Returns(CreatePlot(propertyId, ownerId));
        A.CallTo(() => _sensorRepository.LabelExistsForPlotAsync("Temp-01", plotId, A<CancellationToken>._))
            .Returns(false);

        A.CallTo(() => _sensorRepository.Add(A<SensorAggregate>._))
            .Invokes(call => addedAggregate = call.GetArgument<SensorAggregate>(0));
        A.CallTo(() => _outbox.EnqueueAsync(A<EventContext<SensorRegisteredIntegrationEvent>>._, A<CancellationToken>._))
            .Invokes(call => publishedEvent = call.GetArgument<EventContext<SensorRegisteredIntegrationEvent>>(0));
        A.CallTo(() => _outbox.SaveChangesAsync(A<CancellationToken>._))
            .Returns(Task.FromResult(1));

        var sut = CreateHandler(TestUserContextFactory.CreateProducer(ownerId));

        var result = await sut.ExecuteAsync(command, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.PlotId.ShouldBe(plotId);
        result.Value.Type.ShouldBe("Temperature");
        result.Value.Status.ShouldBe("Active");

        addedAggregate.ShouldNotBeNull();
        addedAggregate!.OwnerId.ShouldBe(ownerId);

        publishedEvent.ShouldNotBeNull();
        publishedEvent!.EventData.OwnerId.ShouldBe(ownerId);
        publishedEvent.EventData.PlotId.ShouldBe(plotId);
        publishedEvent.EventData.Type.ShouldBe("Temperature");

        A.CallTo(() => _sensorRepository.Add(A<SensorAggregate>._)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _outbox.SaveChangesAsync(A<CancellationToken>._)).MustHaveHappenedOnceExactly();
    }

    private CreateSensorCommandHandler CreateHandler(IUserContext userContext)
        => new(
            _sensorRepository,
            _plotRepository,
            _plotReadStore,
            _propertyRepository,
            userContext,
            _outbox,
            _logger);

    private static GetPlotByIdResponse CreatePlotResponse(Guid plotId, Guid propertyId, Guid ownerId)
        => new(
            Id: plotId,
            PropertyId: propertyId,
            OwnerId: ownerId,
            OwnerName: "Producer",
            PropertyName: "Farm Sigma",
            Name: "North Plot",
            CropType: "Soy",
            AreaHectares: 50,
            Latitude: -21.1775,
            Longitude: -47.8103,
            BoundaryGeoJson: null,
            IsActive: true,
            SensorCount: 0,
            CreatedAt: DateTimeOffset.UtcNow.AddDays(-15),
            UpdatedAt: null,
            PlantingDate: DateTimeOffset.UtcNow.AddDays(-30),
            ExpectedHarvestDate: DateTimeOffset.UtcNow.AddDays(120),
            IrrigationType: "Center Pivot",
            AdditionalNotes: null);

    private static PropertyAggregate CreateProperty(Guid ownerId)
    {
        var result = PropertyAggregate.Create(
            name: "Farm Sigma",
            address: "Road 15",
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

    private static PlotAggregate CreatePlot(Guid propertyId, Guid ownerId)
    {
        var result = PlotAggregate.Create(
            propertyId: propertyId,
            ownerId: ownerId,
            name: "North Plot",
            cropType: "Soy",
            areaHectares: 50,
            plantingDate: DateTimeOffset.UtcNow.AddDays(-20),
            expectedHarvestDate: DateTimeOffset.UtcNow.AddDays(100),
            irrigationType: "Center Pivot",
            additionalNotes: null,
            latitude: -21.1775,
            longitude: -47.8103,
            boundaryGeoJson: null);

        result.IsSuccess.ShouldBeTrue();

        return result.Value;
    }
}
