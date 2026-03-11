using Ardalis.Result;
using FakeItEasy;
using Microsoft.Extensions.Logging;
using TC.Agro.Farm.Application.Abstractions.Ports;
using TC.Agro.Farm.Application.UseCases.CropTypes.Create;
using TC.Agro.Farm.Domain.Aggregates;
using TC.Agro.Farm.Tests.TestHelpers;
using TC.Agro.SharedKernel.Application.Ports;
using TC.Agro.SharedKernel.Infrastructure.UserClaims;

namespace TC.Agro.Farm.Tests.Application.UseCases.CropTypes.Create;

public sealed class CreateCropTypeCommandHandlerTests
{
    private readonly ICropTypeCatalogRepository _repository = A.Fake<ICropTypeCatalogRepository>();
    private readonly IPropertyAggregateRepository _propertyRepository = A.Fake<IPropertyAggregateRepository>();
    private readonly ITransactionalOutbox _outbox = A.Fake<ITransactionalOutbox>();
    private readonly ILogger<CreateCropTypeCommandHandler> _logger = A.Fake<ILogger<CreateCropTypeCommandHandler>>();

    public CreateCropTypeCommandHandlerTests()
    {
        FastEndpointsTestBootstrap.EnsureInitialized();
    }

    [Fact]
    public async Task ExecuteAsync_WhenUserContextHasNoOwnerId_ShouldReturnUnauthorized()
    {
        var userContext = TestUserContextFactory.CreateProducer(Guid.Empty);
        var command = CreateCommand();

        var sut = CreateHandler(userContext);

        var result = await sut.ExecuteAsync(command, CancellationToken.None);

        result.Status.ShouldBe(ResultStatus.Unauthorized);
        result.Errors.ShouldContain(error => error.Contains("not authorized", StringComparison.OrdinalIgnoreCase));

        A.CallTo(() => _repository.Add(A<CropTypeCatalogAggregate>._)).MustNotHaveHappened();
        A.CallTo(() => _outbox.SaveChangesAsync(A<CancellationToken>._)).MustNotHaveHappened();
    }

    [Fact]
    public async Task ExecuteAsync_WhenOwnerAlreadyHasCatalogEntryWithSameName_ShouldReturnInvalid()
    {
        var ownerId = Guid.NewGuid();
        var userContext = TestUserContextFactory.CreateProducer(ownerId);
        var command = CreateCommand();
        var existingCatalog = CropTypeCatalogAggregate.Create(command.CropType, isSystemDefined: false, ownerId: ownerId).Value;

        A.CallTo(() => _repository.GetByNameAsync(command.CropType, ownerId, A<CancellationToken>._))
            .Returns(existingCatalog);

        var sut = CreateHandler(userContext);

        var result = await sut.ExecuteAsync(command, CancellationToken.None);

        result.Status.ShouldBe(ResultStatus.Invalid);
        result.ValidationErrors.ShouldContain(error =>
            error.ErrorMessage.Contains("already exists", StringComparison.OrdinalIgnoreCase));

        A.CallTo(() => _repository.Add(A<CropTypeCatalogAggregate>._)).MustNotHaveHappened();
        A.CallTo(() => _outbox.SaveChangesAsync(A<CancellationToken>._)).MustNotHaveHappened();
    }

    [Fact]
    public async Task ExecuteAsync_WhenCommandIsValid_ShouldCreateTenantCatalogEntry()
    {
        var ownerId = Guid.NewGuid();
        var userContext = TestUserContextFactory.CreateProducer(ownerId);
        var property = CreateProperty(ownerId);
        var command = CreateCommand();
        CropTypeCatalogAggregate? persistedAggregate = null;

        A.CallTo(() => _repository.GetByNameAsync(command.CropType, ownerId, A<CancellationToken>._))
            .Returns(Task.FromResult<CropTypeCatalogAggregate?>(null));
        A.CallTo(() => _repository.Add(A<CropTypeCatalogAggregate>._))
            .Invokes(call => persistedAggregate = call.GetArgument<CropTypeCatalogAggregate>(0));
        A.CallTo(() => _propertyRepository.GetAnyByOwnerAsync(ownerId, A<CancellationToken>._))
            .Returns(property);

        var sut = CreateHandler(userContext);

        var result = await sut.ExecuteAsync(command, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Source.ShouldBe("Catalog");
        result.Value.PropertyId.ShouldBe(property.Id);
        result.Value.OwnerId.ShouldBe(ownerId);
        result.Value.CropType.ShouldBe(command.CropType);
        result.Value.CropTypeCatalogId.ShouldBe(result.Value.Id);

        persistedAggregate.ShouldNotBeNull();
        persistedAggregate!.OwnerId.ShouldBe(ownerId);
        persistedAggregate.IsSystemDefined.ShouldBeFalse();
        persistedAggregate.CropTypeName.Value.ShouldBe(command.CropType);

        A.CallTo(() => _outbox.SaveChangesAsync(A<CancellationToken>._)).MustHaveHappenedOnceExactly();
    }

    private CreateCropTypeCommandHandler CreateHandler(IUserContext userContext)
        => new(_repository, _propertyRepository, userContext, _outbox, _logger);

    private static CreateCropTypeCommand CreateCommand()
        => new(
            CropType: "Soy",
            PlantingWindow: "September to November",
            HarvestCycleMonths: 5,
            SuggestedIrrigationType: "Center Pivot",
            MinSoilMoisture: 30,
            MaxTemperature: 35,
            MinHumidity: 45,
            Notes: "Crop type metadata",
            SuggestedImage: "soy-icon");

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
