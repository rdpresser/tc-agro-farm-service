using Ardalis.Result;
using FakeItEasy;
using Microsoft.Extensions.Logging;
using TC.Agro.Farm.Application.Abstractions.Ports;
using TC.Agro.Farm.Application.UseCases.CropTypes.Update;
using TC.Agro.Farm.Domain.Aggregates;
using TC.Agro.Farm.Tests.TestHelpers;
using TC.Agro.SharedKernel.Application.Ports;
using TC.Agro.SharedKernel.Infrastructure.UserClaims;

namespace TC.Agro.Farm.Tests.Application.UseCases.CropTypes.Update;

public sealed class UpdateCropTypeCommandHandlerTests
{
    private readonly ICropTypeCatalogRepository _repository = A.Fake<ICropTypeCatalogRepository>();
    private readonly ITransactionalOutbox _outbox = A.Fake<ITransactionalOutbox>();
    private readonly ILogger<UpdateCropTypeCommandHandler> _logger = A.Fake<ILogger<UpdateCropTypeCommandHandler>>();

    public UpdateCropTypeCommandHandlerTests()
    {
        FastEndpointsTestBootstrap.EnsureInitialized();
    }

    [Fact]
    public async Task ExecuteAsync_WhenCatalogEntryDoesNotExist_ShouldReturnNotFound()
    {
        var userContext = TestUserContextFactory.CreateProducer();
        var command = CreateCommand();

        A.CallTo(() => _repository.GetByIdScopedAsync(command.CropTypeId, userContext.Id, false, A<CancellationToken>._))
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
        var userContext = TestUserContextFactory.CreateProducer(Guid.NewGuid());
        var ownerId = Guid.NewGuid();
        var aggregate = CreateCatalog("Soy", ownerId);
        var command = CreateCommand(aggregate.Id);

        A.CallTo(() => _repository.GetByIdScopedAsync(command.CropTypeId, userContext.Id, false, A<CancellationToken>._))
            .Returns(aggregate);

        var sut = CreateHandler(userContext);

        var result = await sut.ExecuteAsync(command, CancellationToken.None);

        result.Status.ShouldBe(ResultStatus.Unauthorized);
        result.Errors.ShouldContain(error => error.Contains("not authorized", StringComparison.OrdinalIgnoreCase));

        A.CallTo(() => _outbox.SaveChangesAsync(A<CancellationToken>._)).MustNotHaveHappened();
    }

    [Fact]
    public async Task ExecuteAsync_WhenCropTypeNameIsChanged_ShouldReturnInvalid()
    {
        var ownerId = Guid.NewGuid();
        var userContext = TestUserContextFactory.CreateProducer(ownerId);
        var aggregate = CreateCatalog("Soy", ownerId);
        var command = CreateCommand(aggregate.Id) with { CropType = "Corn" };

        A.CallTo(() => _repository.GetByIdScopedAsync(command.CropTypeId, ownerId, false, A<CancellationToken>._))
            .Returns(aggregate);

        var sut = CreateHandler(userContext);

        var result = await sut.ExecuteAsync(command, CancellationToken.None);

        result.Status.ShouldBe(ResultStatus.Invalid);
        result.ValidationErrors.ShouldContain(error =>
            error.ErrorMessage.Contains("cannot be renamed", StringComparison.OrdinalIgnoreCase));

        A.CallTo(() => _outbox.SaveChangesAsync(A<CancellationToken>._)).MustNotHaveHappened();
    }

    [Fact]
    public async Task ExecuteAsync_WhenCommandIsValid_ShouldUpdateCatalogMetadata()
    {
        var ownerId = Guid.NewGuid();
        var userContext = TestUserContextFactory.CreateProducer(ownerId);
        var aggregate = CreateCatalog("Soy", ownerId);
        var command = CreateCommand(aggregate.Id) with
        {
            PlantingWindow = "October to December",
            HarvestCycleMonths = 6,
            SuggestedIrrigationType = "Drip Irrigation",
            MinSoilMoisture = 28,
            MaxTemperature = 36,
            MinHumidity = 44,
            Notes = "Updated metadata",
            SuggestedImage = "soy-upd"
        };

        A.CallTo(() => _repository.GetByIdScopedAsync(command.CropTypeId, ownerId, false, A<CancellationToken>._))
            .Returns(aggregate);

        var sut = CreateHandler(userContext);

        var result = await sut.ExecuteAsync(command, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Source.ShouldBe("Catalog");
        result.Value.CropType.ShouldBe("Soy");
        result.Value.CropTypeCatalogId.ShouldBe(aggregate.Id);
        result.Value.SuggestedIrrigationType.ShouldBe("Drip Irrigation");
        result.Value.HarvestCycleMonths.ShouldBe(6);
        result.Value.PlantingWindow.ShouldBe("Oct to Dec");

        A.CallTo(() => _outbox.SaveChangesAsync(A<CancellationToken>._)).MustHaveHappenedOnceExactly();
    }

    private UpdateCropTypeCommandHandler CreateHandler(IUserContext userContext)
        => new(_repository, userContext, _outbox, _logger);

    private static UpdateCropTypeCommand CreateCommand(Guid? cropTypeId = null)
        => new(
            CropTypeId: cropTypeId ?? Guid.NewGuid(),
            CropType: "Soy",
            PlantingWindow: "September to November",
            HarvestCycleMonths: 5,
            SuggestedIrrigationType: "Center Pivot",
            MinSoilMoisture: 30,
            MaxTemperature: 35,
            MinHumidity: 45,
            Notes: "Catalog metadata",
            SuggestedImage: "soy-icon");

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
