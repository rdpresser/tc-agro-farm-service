using Ardalis.Result;
using FakeItEasy;
using Microsoft.Extensions.Logging;
using TC.Agro.Contracts.Events.Farm;
using TC.Agro.Farm.Application.Abstractions.Ports;
using TC.Agro.Farm.Application.UseCases.Properties.Update;
using TC.Agro.Farm.Domain.Aggregates;
using TC.Agro.Farm.Tests.TestHelpers;
using TC.Agro.SharedKernel.Application.Ports;
using TC.Agro.SharedKernel.Infrastructure.Messaging;
using TC.Agro.SharedKernel.Infrastructure.UserClaims;

namespace TC.Agro.Farm.Tests.Application.UseCases.Properties.Update;

public sealed class UpdatePropertyCommandHandlerTests
{
    private readonly IPropertyAggregateRepository _repository = A.Fake<IPropertyAggregateRepository>();
    private readonly ICropTypeSuggestionRepository _cropTypeRepository = A.Fake<ICropTypeSuggestionRepository>();
    private readonly ITransactionalOutbox _outbox = A.Fake<ITransactionalOutbox>();
    private readonly ILogger<UpdatePropertyCommandHandler> _logger = A.Fake<ILogger<UpdatePropertyCommandHandler>>();

    public UpdatePropertyCommandHandlerTests()
    {
        FastEndpointsTestBootstrap.EnsureInitialized();
    }

    [Fact]
    public async Task ExecuteAsync_WhenPropertyDoesNotExist_ShouldReturnNotFound()
    {
        var userContext = TestUserContextFactory.CreateProducer();
        var command = CreateCommand();

        A.CallTo(() => _repository.GetByIdAsync(command.Id, A<CancellationToken>._))
            .Returns(Task.FromResult<PropertyAggregate?>(null));

        var sut = CreateHandler(userContext);

        var result = await sut.ExecuteAsync(command, CancellationToken.None);

        result.Status.ShouldBe(ResultStatus.NotFound);
        result.Errors.ShouldContain(error => error.Contains("Property not found", StringComparison.OrdinalIgnoreCase));

        A.CallTo(() => _repository.NameExistsForOwnerExcludingAsync(A<string>._, A<Guid>._, A<Guid>._, A<CancellationToken>._))
            .MustNotHaveHappened();
        A.CallTo(() => _outbox.SaveChangesAsync(A<CancellationToken>._)).MustNotHaveHappened();
    }

    [Fact]
    public async Task ExecuteAsync_WhenCallerIsNotOwnerAndNotAdmin_ShouldReturnUnauthorized()
    {
        var ownerId = Guid.NewGuid();
        var userContext = TestUserContextFactory.CreateProducer(Guid.NewGuid());
        var aggregate = CreateProperty(ownerId);
        var command = CreateCommand(aggregate.Id);

        A.CallTo(() => _repository.GetByIdAsync(command.Id, A<CancellationToken>._))
            .Returns(aggregate);

        var sut = CreateHandler(userContext);

        var result = await sut.ExecuteAsync(command, CancellationToken.None);

        result.Status.ShouldBe(ResultStatus.Unauthorized);
        result.Errors.ShouldContain(error => error.Contains("not authorized", StringComparison.OrdinalIgnoreCase));

        A.CallTo(() => _repository.NameExistsForOwnerExcludingAsync(A<string>._, A<Guid>._, A<Guid>._, A<CancellationToken>._))
            .MustNotHaveHappened();
        A.CallTo(() => _outbox.SaveChangesAsync(A<CancellationToken>._)).MustNotHaveHappened();
    }

    [Fact]
    public async Task ExecuteAsync_WhenNameAlreadyExistsForOwner_ShouldReturnInvalid()
    {
        var ownerId = Guid.NewGuid();
        var userContext = TestUserContextFactory.CreateProducer(ownerId);
        var aggregate = CreateProperty(ownerId);
        var command = CreateCommand(aggregate.Id);

        A.CallTo(() => _repository.GetByIdAsync(command.Id, A<CancellationToken>._))
            .Returns(aggregate);
        A.CallTo(() => _repository.NameExistsForOwnerExcludingAsync(command.Name, ownerId, aggregate.Id, A<CancellationToken>._))
            .Returns(true);

        var sut = CreateHandler(userContext);

        var result = await sut.ExecuteAsync(command, CancellationToken.None);

        result.Status.ShouldBe(ResultStatus.Invalid);
        result.ValidationErrors.ShouldContain(error =>
            error.ErrorMessage.Contains("already exists", StringComparison.OrdinalIgnoreCase));

        A.CallTo(() => _outbox.EnqueueAsync(A<EventContext<PropertyUpdatedIntegrationEvent>>._, A<CancellationToken>._))
            .MustNotHaveHappened();
        A.CallTo(() => _outbox.SaveChangesAsync(A<CancellationToken>._)).MustNotHaveHappened();
    }

    [Fact]
    public async Task ExecuteAsync_WhenCommandIsValid_ShouldUpdateAndPublishIntegrationEvent()
    {
        var ownerId = Guid.NewGuid();
        var userContext = TestUserContextFactory.CreateProducer(ownerId);
        var aggregate = CreateProperty(ownerId);
        var command = CreateCommand(aggregate.Id) with
        {
            Name = "Farm Updated",
            City = "Araraquara",
            AreaHectares = 210
        };

        EventContext<PropertyUpdatedIntegrationEvent>? publishedEvent = null;

        A.CallTo(() => _repository.GetByIdAsync(command.Id, A<CancellationToken>._))
            .Returns(aggregate);
        A.CallTo(() => _repository.NameExistsForOwnerExcludingAsync(command.Name, ownerId, aggregate.Id, A<CancellationToken>._))
            .Returns(false);
        A.CallTo(() => _outbox.EnqueueAsync(A<EventContext<PropertyUpdatedIntegrationEvent>>._, A<CancellationToken>._))
            .Invokes(call => publishedEvent = call.GetArgument<EventContext<PropertyUpdatedIntegrationEvent>>(0));
        A.CallTo(() => _outbox.SaveChangesAsync(A<CancellationToken>._))
            .Returns(Task.FromResult(1));

        var sut = CreateHandler(userContext);

        var result = await sut.ExecuteAsync(command, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Name.ShouldBe("Farm Updated");
        result.Value.City.ShouldBe("Araraquara");
        result.Value.AreaHectares.ShouldBe(210);

        publishedEvent.ShouldNotBeNull();
        publishedEvent!.EventData.OwnerId.ShouldBe(ownerId);
        publishedEvent.EventData.Name.ShouldBe("Farm Updated");

        A.CallTo(() => _cropTypeRepository.MarkAiSuggestionsAsStaleByPropertyAsync(A<Guid>._, A<CancellationToken>._))
            .MustNotHaveHappened();
        A.CallTo(() => _outbox.SaveChangesAsync(A<CancellationToken>._)).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task ExecuteAsync_WhenLocationChanges_ShouldMarkAiSuggestionsAsStale()
    {
        var ownerId = Guid.NewGuid();
        var userContext = TestUserContextFactory.CreateProducer(ownerId);
        var aggregate = CreateProperty(ownerId);
        var command = CreateCommand(aggregate.Id) with
        {
            Latitude = -20.1234,
            Longitude = -47.9999
        };

        A.CallTo(() => _repository.GetByIdAsync(command.Id, A<CancellationToken>._))
            .Returns(aggregate);
        A.CallTo(() => _repository.NameExistsForOwnerExcludingAsync(command.Name, ownerId, aggregate.Id, A<CancellationToken>._))
            .Returns(false);
        A.CallTo(() => _outbox.SaveChangesAsync(A<CancellationToken>._))
            .Returns(Task.FromResult(1));

        var sut = CreateHandler(userContext);

        var result = await sut.ExecuteAsync(command, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();

        A.CallTo(() => _cropTypeRepository.MarkAiSuggestionsAsStaleByPropertyAsync(aggregate.Id, A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
    }

    private UpdatePropertyCommandHandler CreateHandler(IUserContext userContext)
        => new(_repository, _cropTypeRepository, userContext, _outbox, _logger);

    private static UpdatePropertyCommand CreateCommand(Guid? id = null)
        => new(
            Id: id ?? Guid.NewGuid(),
            Name: "Farm One",
            Address: "Road 9",
            City: "Ribeirao Preto",
            State: "SP",
            Country: "Brazil",
            AreaHectares: 180,
            Latitude: -21.1775,
            Longitude: -47.8103);

    private static PropertyAggregate CreateProperty(Guid ownerId)
    {
        var result = PropertyAggregate.Create(
            name: "Farm One",
            address: "Road 9",
            city: "Ribeirao Preto",
            state: "SP",
            country: "Brazil",
            areaHectares: 180,
            ownerId: ownerId,
            latitude: -21.1775,
            longitude: -47.8103);

        result.IsSuccess.ShouldBeTrue();
        return result.Value;
    }
}
