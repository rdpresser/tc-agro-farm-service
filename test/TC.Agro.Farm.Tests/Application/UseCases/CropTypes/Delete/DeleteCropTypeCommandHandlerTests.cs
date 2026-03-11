using Ardalis.Result;
using FakeItEasy;
using TC.Agro.Farm.Application.Abstractions.Ports;
using TC.Agro.Farm.Application.UseCases.CropTypes.Delete;
using TC.Agro.Farm.Domain.Aggregates;
using TC.Agro.Farm.Tests.TestHelpers;
using TC.Agro.SharedKernel.Application.Ports;
using TC.Agro.SharedKernel.Infrastructure.UserClaims;

namespace TC.Agro.Farm.Tests.Application.UseCases.CropTypes.Delete;

public sealed class DeleteCropTypeCommandHandlerTests
{
    private readonly ICropTypeCatalogRepository _repository = A.Fake<ICropTypeCatalogRepository>();
    private readonly ITransactionalOutbox _outbox = A.Fake<ITransactionalOutbox>();

    public DeleteCropTypeCommandHandlerTests()
    {
        FastEndpointsTestBootstrap.EnsureInitialized();
    }

    [Fact]
    public async Task ExecuteAsync_WhenCatalogEntryDoesNotExist_ShouldReturnNotFound()
    {
        var userContext = TestUserContextFactory.CreateProducer();
        var command = new DeleteCropTypeCommand(Guid.NewGuid());

        A.CallTo(() => _repository.GetByIdScopedAsync(command.CropTypeId, userContext.Id, true, A<CancellationToken>._))
            .Returns(Task.FromResult<CropTypeCatalogAggregate?>(null));

        var sut = CreateHandler(userContext);

        var result = await sut.ExecuteAsync(command, CancellationToken.None);

        result.Status.ShouldBe(ResultStatus.NotFound);
        result.Errors.ShouldContain(error => error.Contains("catalog", StringComparison.OrdinalIgnoreCase));

        A.CallTo(() => _outbox.SaveChangesAsync(A<CancellationToken>._)).MustNotHaveHappened();
    }

    [Fact]
    public async Task ExecuteAsync_WhenCallerIsNotOwnerAndNotAdmin_ShouldReturnUnauthorized()
    {
        var ownerId = Guid.NewGuid();
        var userContext = TestUserContextFactory.CreateProducer(Guid.NewGuid());
        var aggregate = CreateCatalog("Soy", ownerId);
        var command = new DeleteCropTypeCommand(aggregate.Id);

        A.CallTo(() => _repository.GetByIdScopedAsync(command.CropTypeId, userContext.Id, true, A<CancellationToken>._))
            .Returns(aggregate);

        var sut = CreateHandler(userContext);

        var result = await sut.ExecuteAsync(command, CancellationToken.None);

        result.Status.ShouldBe(ResultStatus.Unauthorized);
        result.Errors.ShouldContain(error => error.Contains("not authorized", StringComparison.OrdinalIgnoreCase));

        A.CallTo(() => _outbox.SaveChangesAsync(A<CancellationToken>._)).MustNotHaveHappened();
    }

    [Fact]
    public async Task ExecuteAsync_WhenCommandIsValid_ShouldDeactivateCatalogEntry()
    {
        var ownerId = Guid.NewGuid();
        var userContext = TestUserContextFactory.CreateProducer(ownerId);
        var aggregate = CreateCatalog("Soy", ownerId);
        var command = new DeleteCropTypeCommand(aggregate.Id);

        A.CallTo(() => _repository.GetByIdScopedAsync(command.CropTypeId, ownerId, true, A<CancellationToken>._))
            .Returns(aggregate);

        var sut = CreateHandler(userContext);

        var result = await sut.ExecuteAsync(command, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Id.ShouldBe(aggregate.Id);
        aggregate.IsActive.ShouldBeFalse();

        A.CallTo(() => _outbox.SaveChangesAsync(A<CancellationToken>._)).MustHaveHappenedOnceExactly();
    }

    private DeleteCropTypeCommandHandler CreateHandler(IUserContext userContext)
        => new(_repository, userContext, _outbox);

    private static CropTypeCatalogAggregate CreateCatalog(string cropType, Guid ownerId)
    {
        var result = CropTypeCatalogAggregate.Create(
            cropTypeName: cropType,
            isSystemDefined: false,
            ownerId: ownerId,
            description: "Initial metadata",
            recommendedIrrigationType: "Center Pivot",
            typicalHarvestCycleMonths: 5,
            typicalPlantingStartMonth: 9,
            typicalPlantingEndMonth: 11,
            minSoilMoisture: 30,
            maxTemperature: 35,
            minHumidity: 45,
            suggestedImage: "soy-icon");

        result.IsSuccess.ShouldBeTrue();
        return result.Value;
    }
}
