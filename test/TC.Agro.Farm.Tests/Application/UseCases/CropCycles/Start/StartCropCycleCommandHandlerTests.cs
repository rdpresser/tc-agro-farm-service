using Ardalis.Result;
using FakeItEasy;
using Microsoft.Extensions.Logging;
using TC.Agro.Farm.Application.Abstractions.Ports;
using TC.Agro.Farm.Application.UseCases.CropCycles.Start;
using TC.Agro.Farm.Domain.Aggregates;
using TC.Agro.Farm.Tests.TestHelpers;
using TC.Agro.SharedKernel.Application.Ports;
using TC.Agro.SharedKernel.Infrastructure.UserClaims;

namespace TC.Agro.Farm.Tests.Application.UseCases.CropCycles.Start;

public sealed class StartCropCycleCommandHandlerTests
{
    private readonly ICropCycleAggregateRepository _repository = A.Fake<ICropCycleAggregateRepository>();
    private readonly IPlotAggregateRepository _plotRepository = A.Fake<IPlotAggregateRepository>();
    private readonly ITransactionalOutbox _outbox = A.Fake<ITransactionalOutbox>();
    private readonly ILogger<StartCropCycleCommandHandler> _logger = A.Fake<ILogger<StartCropCycleCommandHandler>>();

    public StartCropCycleCommandHandlerTests()
    {
        FastEndpointsTestBootstrap.EnsureInitialized();
    }

    [Fact]
    public async Task ExecuteAsync_WhenPlotDoesNotExist_ShouldReturnInvalid()
    {
        var userContext = TestUserContextFactory.CreateProducer();
        var command = CreateCommand();

        A.CallTo(() => _plotRepository.GetByIdAsync(command.PlotId, A<CancellationToken>._))
            .Returns(Task.FromResult<PlotAggregate?>(null));

        var sut = CreateHandler(userContext);

        var result = await sut.ExecuteAsync(command, CancellationToken.None);

        result.Status.ShouldBe(ResultStatus.Invalid);
        result.ValidationErrors.ShouldContain(error =>
            error.ErrorMessage.Contains("Plot not found", StringComparison.OrdinalIgnoreCase));

        A.CallTo(() => _repository.HasActiveCyclesByPlotAsync(A<Guid>._, A<Guid?>._, A<CancellationToken>._))
            .MustNotHaveHappened();
        A.CallTo(() => _repository.Add(A<CropCycleAggregate>._)).MustNotHaveHappened();
        A.CallTo(() => _outbox.SaveChangesAsync(A<CancellationToken>._)).MustNotHaveHappened();
    }

    [Fact]
    public async Task ExecuteAsync_WhenCallerIsNotOwnerAndNotAdmin_ShouldReturnUnauthorized()
    {
        var ownerId = Guid.NewGuid();
        var userContext = TestUserContextFactory.CreateProducer(Guid.NewGuid());
        var plot = CreatePlot(ownerId);
        var command = CreateCommand(plot.Id);

        A.CallTo(() => _plotRepository.GetByIdAsync(command.PlotId, A<CancellationToken>._))
            .Returns(plot);

        var sut = CreateHandler(userContext);

        var result = await sut.ExecuteAsync(command, CancellationToken.None);

        result.Status.ShouldBe(ResultStatus.Unauthorized);
        result.Errors.ShouldContain(error =>
            error.Contains("not authorized", StringComparison.OrdinalIgnoreCase));

        A.CallTo(() => _repository.HasActiveCyclesByPlotAsync(A<Guid>._, A<Guid?>._, A<CancellationToken>._))
            .MustNotHaveHappened();
        A.CallTo(() => _repository.Add(A<CropCycleAggregate>._)).MustNotHaveHappened();
        A.CallTo(() => _outbox.SaveChangesAsync(A<CancellationToken>._)).MustNotHaveHappened();
    }

    [Fact]
    public async Task ExecuteAsync_WhenPlotAlreadyHasActiveCycle_ShouldReturnConflict()
    {
        var ownerId = Guid.NewGuid();
        var userContext = TestUserContextFactory.CreateProducer(ownerId);
        var plot = CreatePlot(ownerId);
        var command = CreateCommand(plot.Id);

        A.CallTo(() => _plotRepository.GetByIdAsync(command.PlotId, A<CancellationToken>._))
            .Returns(plot);
        A.CallTo(() => _repository.HasActiveCyclesByPlotAsync(command.PlotId, A<Guid?>._, A<CancellationToken>._))
            .Returns(true);

        var sut = CreateHandler(userContext);

        var result = await sut.ExecuteAsync(command, CancellationToken.None);

        result.Status.ShouldBe(ResultStatus.Conflict);
        result.Errors.ShouldContain(error =>
            error.Contains("active crop cycle", StringComparison.OrdinalIgnoreCase));

        A.CallTo(() => _repository.Add(A<CropCycleAggregate>._)).MustNotHaveHappened();
        A.CallTo(() => _outbox.SaveChangesAsync(A<CancellationToken>._)).MustNotHaveHappened();
    }

    [Fact]
    public async Task ExecuteAsync_WhenCommandIsValid_ShouldPersistCropCycle()
    {
        var ownerId = Guid.NewGuid();
        var userContext = TestUserContextFactory.CreateProducer(ownerId);
        var plot = CreatePlot(ownerId);
        var command = CreateCommand(plot.Id);
        CropCycleAggregate? persistedAggregate = null;

        A.CallTo(() => _plotRepository.GetByIdAsync(command.PlotId, A<CancellationToken>._))
            .Returns(plot);
        A.CallTo(() => _repository.HasActiveCyclesByPlotAsync(command.PlotId, A<Guid?>._, A<CancellationToken>._))
            .Returns(false);
        A.CallTo(() => _repository.Add(A<CropCycleAggregate>._))
            .Invokes(call => persistedAggregate = call.GetArgument<CropCycleAggregate>(0));

        var sut = CreateHandler(userContext);

        var result = await sut.ExecuteAsync(command, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.PlotId.ShouldBe(command.PlotId);
        result.Value.PropertyId.ShouldBe(plot.PropertyId);
        result.Value.CropTypeCatalogId.ShouldBe(command.CropTypeCatalogId);
        result.Value.Status.ShouldBe(command.Status);

        persistedAggregate.ShouldNotBeNull();
        persistedAggregate!.OwnerId.ShouldBe(ownerId);
        persistedAggregate.PlotId.ShouldBe(command.PlotId);

        A.CallTo(() => _repository.Add(A<CropCycleAggregate>._)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _outbox.SaveChangesAsync(A<CancellationToken>._)).MustHaveHappenedOnceExactly();
    }

    private StartCropCycleCommandHandler CreateHandler(IUserContext userContext)
        => new(_repository, _plotRepository, userContext, _outbox, _logger);

    private static StartCropCycleCommand CreateCommand(Guid? plotId = null)
        => new(
            PlotId: plotId ?? Guid.NewGuid(),
            CropTypeCatalogId: Guid.NewGuid(),
            StartedAt: DateTimeOffset.UtcNow.AddDays(-3),
            ExpectedHarvestDate: DateTimeOffset.UtcNow.AddMonths(4),
            Status: "Planted",
            Notes: "Field prepared and planted.");

    private static PlotAggregate CreatePlot(Guid ownerId)
    {
        var result = PlotAggregate.Create(
            propertyId: Guid.NewGuid(),
            ownerId: ownerId,
            name: "North Plot",
            cropType: "Soy",
            areaHectares: 32.5,
            plantingDate: DateTimeOffset.UtcNow.AddDays(-10),
            expectedHarvestDate: DateTimeOffset.UtcNow.AddMonths(5),
            irrigationType: "Drip Irrigation",
            additionalNotes: "Plot notes",
            latitude: -21.1775,
            longitude: -47.8103,
            boundaryGeoJson: null,
            cropTypeCatalogId: Guid.NewGuid());

        result.IsSuccess.ShouldBeTrue();
        return result.Value;
    }
}
