using Ardalis.Result;
using FakeItEasy;
using Microsoft.Extensions.Logging;
using TC.Agro.Farm.Application.Abstractions.Ports;
using TC.Agro.Farm.Application.UseCases.CropTypes.Regenerate;
using TC.Agro.Farm.Domain.Aggregates;
using TC.Agro.Farm.Tests.TestHelpers;
using TC.Agro.SharedKernel.Infrastructure.UserClaims;
using Wolverine;

namespace TC.Agro.Farm.Tests.Application.UseCases.CropTypes.Regenerate;

public sealed class RegeneratePropertyCropTypesCommandHandlerTests
{
    private readonly IPropertyAggregateRepository _propertyRepository = A.Fake<IPropertyAggregateRepository>();
    private readonly IMessageBus _messageBus = A.Fake<IMessageBus>();
    private readonly ILogger<RegeneratePropertyCropTypesCommandHandler> _logger = A.Fake<ILogger<RegeneratePropertyCropTypesCommandHandler>>();

    public RegeneratePropertyCropTypesCommandHandlerTests()
    {
        FastEndpointsTestBootstrap.EnsureInitialized();
    }

    [Fact]
    public async Task ExecuteAsync_WhenPropertyDoesNotExist_ShouldReturnNotFoundAndNotPublishMessage()
    {
        var userContext = TestUserContextFactory.CreateProducer();
        var command = new RegeneratePropertyCropTypesCommand(Guid.NewGuid());

        A.CallTo(() => _propertyRepository.GetByIdAsync(command.PropertyId, A<CancellationToken>._))
            .Returns(Task.FromResult<PropertyAggregate?>(null));

        var sut = CreateHandler(userContext);

        var result = await sut.ExecuteAsync(command, CancellationToken.None);

        result.Status.ShouldBe(ResultStatus.NotFound);
        result.Errors.ShouldContain(error => error.Contains("Property not found", StringComparison.OrdinalIgnoreCase));

        A.CallTo(() => _messageBus.PublishAsync(A<GeneratePropertyCropTypeSuggestionsMessage>._))
            .MustNotHaveHappened();
    }

    [Fact]
    public async Task ExecuteAsync_WhenCallerIsNotOwnerAndNotAdmin_ShouldReturnUnauthorizedAndNotPublishMessage()
    {
        var ownerId = Guid.NewGuid();
        var userContext = TestUserContextFactory.CreateProducer(Guid.NewGuid());
        var property = CreateProperty(ownerId, latitude: -21.1775, longitude: -47.8103);
        var command = new RegeneratePropertyCropTypesCommand(property.Id);

        A.CallTo(() => _propertyRepository.GetByIdAsync(command.PropertyId, A<CancellationToken>._))
            .Returns(property);

        var sut = CreateHandler(userContext);

        var result = await sut.ExecuteAsync(command, CancellationToken.None);

        result.Status.ShouldBe(ResultStatus.Unauthorized);
        result.Errors.ShouldContain(error => error.Contains("not authorized", StringComparison.OrdinalIgnoreCase));

        A.CallTo(() => _messageBus.PublishAsync(A<GeneratePropertyCropTypeSuggestionsMessage>._))
            .MustNotHaveHappened();
    }

    [Fact]
    public async Task ExecuteAsync_WhenPropertyHasNoCoordinates_ShouldReturnInvalidAndNotPublishMessage()
    {
        var ownerId = Guid.NewGuid();
        var userContext = TestUserContextFactory.CreateProducer(ownerId);
        var property = CreateProperty(ownerId, latitude: null, longitude: null);
        var command = new RegeneratePropertyCropTypesCommand(property.Id);

        A.CallTo(() => _propertyRepository.GetByIdAsync(command.PropertyId, A<CancellationToken>._))
            .Returns(property);

        var sut = CreateHandler(userContext);

        var result = await sut.ExecuteAsync(command, CancellationToken.None);

        result.Status.ShouldBe(ResultStatus.Invalid);
        result.ValidationErrors.ShouldContain(error =>
            error.ErrorMessage.Contains("latitude and longitude", StringComparison.OrdinalIgnoreCase));

        A.CallTo(() => _messageBus.PublishAsync(A<GeneratePropertyCropTypeSuggestionsMessage>._))
            .MustNotHaveHappened();
    }

    [Fact]
    public async Task ExecuteAsync_WhenCommandIsValidAndUserIdIsEmpty_ShouldPublishMessageUsingPropertyOwnerAsTrigger()
    {
        var ownerId = Guid.NewGuid();
        var userContext = TestUserContextFactory.CreateAdmin(Guid.Empty);
        var property = CreateProperty(ownerId, latitude: -21.1775, longitude: -47.8103);
        var command = new RegeneratePropertyCropTypesCommand(property.Id);

        GeneratePropertyCropTypeSuggestionsMessage? publishedMessage = null;
        A.CallTo(() => _propertyRepository.GetByIdAsync(command.PropertyId, A<CancellationToken>._))
            .Returns(property);
        A.CallTo(() => _messageBus.PublishAsync(A<GeneratePropertyCropTypeSuggestionsMessage>._))
            .Invokes(call => publishedMessage = call.GetArgument<GeneratePropertyCropTypeSuggestionsMessage>(0));

        var sut = CreateHandler(userContext);

        var result = await sut.ExecuteAsync(command, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.PropertyId.ShouldBe(property.Id);
        result.Value.Status.ShouldBe("Queued");

        publishedMessage.ShouldNotBeNull();
        publishedMessage!.PropertyId.ShouldBe(property.Id);
        publishedMessage.OwnerId.ShouldBe(ownerId);
        publishedMessage.TriggeredByUserId.ShouldBe(ownerId);
        publishedMessage.TriggerReason.ShouldBe("manual-regenerate");

        A.CallTo(() => _messageBus.PublishAsync(A<GeneratePropertyCropTypeSuggestionsMessage>._))
            .MustHaveHappenedOnceExactly();
    }

    private RegeneratePropertyCropTypesCommandHandler CreateHandler(IUserContext userContext)
        => new(_propertyRepository, userContext, _messageBus, _logger);

    private static PropertyAggregate CreateProperty(Guid ownerId, double? latitude, double? longitude)
    {
        var aggregateResult = PropertyAggregate.Create(
            name: "Farm A",
            address: "Road 1",
            city: "Ribeirao Preto",
            state: "SP",
            country: "Brazil",
            areaHectares: 140,
            ownerId: ownerId,
            latitude: latitude,
            longitude: longitude);

        aggregateResult.IsSuccess.ShouldBeTrue();
        return aggregateResult.Value;
    }
}
