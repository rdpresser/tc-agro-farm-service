using Ardalis.Result;
using FakeItEasy;
using Microsoft.Extensions.Logging;
using TC.Agro.Contracts.Events.Farm;
using TC.Agro.Farm.Application.Abstractions.Ports;
using TC.Agro.Farm.Application.UseCases.CropTypes.Regenerate;
using TC.Agro.Farm.Application.UseCases.Properties.Create;
using TC.Agro.Farm.Domain.Aggregates;
using TC.Agro.Farm.Tests.TestHelpers;
using TC.Agro.SharedKernel.Application.Ports;
using TC.Agro.SharedKernel.Infrastructure.Messaging;
using TC.Agro.SharedKernel.Infrastructure.UserClaims;

namespace TC.Agro.Farm.Tests.Application.UseCases.Properties.Create;

public sealed class CreatePropertyCommandHandlerTests
{
    private readonly IPropertyAggregateRepository _repository = A.Fake<IPropertyAggregateRepository>();
    private readonly ITransactionalOutbox _outbox = A.Fake<ITransactionalOutbox>();
    private readonly ILogger<CreatePropertyCommandHandler> _logger = A.Fake<ILogger<CreatePropertyCommandHandler>>();

    public CreatePropertyCommandHandlerTests()
    {
        FastEndpointsTestBootstrap.EnsureInitialized();
    }

    [Fact]
    public async Task ExecuteAsync_WhenProducerCommandIsValid_ShouldPersistAndPublishIntegrationEvent()
    {
        var producerId = Guid.NewGuid();
        var userContext = TestUserContextFactory.CreateProducer(producerId);

        var command = new CreatePropertyCommand(
            Name: "Farm Alpha",
            Address: "Road 1",
            City: "Ribeirao Preto",
            State: "SP",
            Country: "Brazil",
            AreaHectares: 150,
            Latitude: -21.1775,
            Longitude: -47.8103,
            OwnerId: null);

        PropertyAggregate? addedAggregate = null;
        EventContext<PropertyCreatedIntegrationEvent>? publishedEvent = null;
        GeneratePropertyCropTypeSuggestionsMessage? queuedGenerationMessage = null;

        A.CallTo(() => _repository.NameExistsForOwnerAsync(command.Name, producerId, A<CancellationToken>._))
            .Returns(false);
        A.CallTo(() => _repository.Add(A<PropertyAggregate>._))
            .Invokes(call => addedAggregate = call.GetArgument<PropertyAggregate>(0));
        A.CallTo(() => _outbox.EnqueueAsync(A<EventContext<PropertyCreatedIntegrationEvent>>._, A<CancellationToken>._))
            .Invokes(call => publishedEvent = call.GetArgument<EventContext<PropertyCreatedIntegrationEvent>>(0));
        A.CallTo(() => _outbox.EnqueueAsync(A<GeneratePropertyCropTypeSuggestionsMessage>._, A<CancellationToken>._))
            .Invokes(call => queuedGenerationMessage = call.GetArgument<GeneratePropertyCropTypeSuggestionsMessage>(0));
        A.CallTo(() => _outbox.SaveChangesAsync(A<CancellationToken>._))
            .Returns(Task.FromResult(1));

        var sut = CreateHandler(userContext);

        var result = await sut.ExecuteAsync(command, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Name.ShouldBe("Farm Alpha");
        result.Value.OwnerId.ShouldBe(producerId);

        addedAggregate.ShouldNotBeNull();
        addedAggregate!.OwnerId.ShouldBe(producerId);

        publishedEvent.ShouldNotBeNull();
        publishedEvent!.EventData.OwnerId.ShouldBe(producerId);
        publishedEvent.EventData.Name.ShouldBe("Farm Alpha");

        queuedGenerationMessage.ShouldNotBeNull();
        queuedGenerationMessage!.PropertyId.ShouldBe(addedAggregate.Id);
        queuedGenerationMessage.OwnerId.ShouldBe(producerId);
        queuedGenerationMessage.TriggerReason.ShouldBe("property-created");

        A.CallTo(() => _repository.Add(A<PropertyAggregate>._)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _outbox.EnqueueAsync(A<EventContext<PropertyCreatedIntegrationEvent>>._, A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => _outbox.EnqueueAsync(A<GeneratePropertyCropTypeSuggestionsMessage>._, A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => _outbox.SaveChangesAsync(A<CancellationToken>._)).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task ExecuteAsync_WhenCoordinatesAreMissing_ShouldNotQueueCropSuggestionGeneration()
    {
        var producerId = Guid.NewGuid();
        var userContext = TestUserContextFactory.CreateProducer(producerId);

        var command = new CreatePropertyCommand(
            Name: "Farm Delta",
            Address: "Road 8",
            City: "Jaboticabal",
            State: "SP",
            Country: "Brazil",
            AreaHectares: 60,
            Latitude: null,
            Longitude: null,
            OwnerId: null);

        A.CallTo(() => _repository.NameExistsForOwnerAsync(command.Name, producerId, A<CancellationToken>._))
            .Returns(false);
        A.CallTo(() => _outbox.SaveChangesAsync(A<CancellationToken>._))
            .Returns(Task.FromResult(1));

        var sut = CreateHandler(userContext);

        var result = await sut.ExecuteAsync(command, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();

        A.CallTo(() => _outbox.EnqueueAsync(A<GeneratePropertyCropTypeSuggestionsMessage>._, A<CancellationToken>._))
            .MustNotHaveHappened();
    }

    [Fact]
    public async Task ExecuteAsync_WhenAdminOmitsOwnerId_ShouldReturnInvalidWithoutRepositoryCalls()
    {
        var userContext = TestUserContextFactory.CreateAdmin();

        var command = new CreatePropertyCommand(
            Name: "Farm Beta",
            Address: "Road 2",
            City: "Franca",
            State: "SP",
            Country: "Brazil",
            AreaHectares: 80,
            OwnerId: null);

        var sut = CreateHandler(userContext);

        var result = await sut.ExecuteAsync(command, CancellationToken.None);

        result.Status.ShouldBe(ResultStatus.Invalid);
        result.ValidationErrors.ShouldContain(error =>
            error.ErrorMessage.Contains("OwnerId is required", StringComparison.OrdinalIgnoreCase));

        A.CallTo(() => _repository.NameExistsForOwnerAsync(A<string>._, A<Guid>._, A<CancellationToken>._))
            .MustNotHaveHappened();
        A.CallTo(() => _repository.Add(A<PropertyAggregate>._)).MustNotHaveHappened();
        A.CallTo(() => _outbox.SaveChangesAsync(A<CancellationToken>._)).MustNotHaveHappened();
    }

    [Fact]
    public async Task ExecuteAsync_WhenNameAlreadyExistsForOwner_ShouldReturnInvalid()
    {
        var producerId = Guid.NewGuid();
        var userContext = TestUserContextFactory.CreateProducer(producerId);

        var command = new CreatePropertyCommand(
            Name: "Farm Gamma",
            Address: "Road 3",
            City: "Campinas",
            State: "SP",
            Country: "Brazil",
            AreaHectares: 42,
            OwnerId: null);

        A.CallTo(() => _repository.NameExistsForOwnerAsync(command.Name, producerId, A<CancellationToken>._))
            .Returns(true);

        var sut = CreateHandler(userContext);

        var result = await sut.ExecuteAsync(command, CancellationToken.None);

        result.Status.ShouldBe(ResultStatus.Invalid);
        result.ValidationErrors.ShouldContain(error =>
            error.ErrorMessage.Contains("already exists", StringComparison.OrdinalIgnoreCase));

        A.CallTo(() => _repository.Add(A<PropertyAggregate>._)).MustNotHaveHappened();
        A.CallTo(() => _outbox.EnqueueAsync(A<EventContext<PropertyCreatedIntegrationEvent>>._, A<CancellationToken>._))
            .MustNotHaveHappened();
        A.CallTo(() => _outbox.SaveChangesAsync(A<CancellationToken>._)).MustNotHaveHappened();
    }

    private CreatePropertyCommandHandler CreateHandler(IUserContext userContext)
        => new(_repository, userContext, _outbox, _logger);
}
