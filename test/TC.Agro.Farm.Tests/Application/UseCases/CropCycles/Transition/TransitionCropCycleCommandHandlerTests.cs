using Ardalis.Result;
using FakeItEasy;
using Microsoft.Extensions.Logging;
using TC.Agro.Farm.Application.Abstractions.Ports;
using TC.Agro.Farm.Application.UseCases.CropCycles.Transition;
using TC.Agro.Farm.Domain.Aggregates;
using TC.Agro.Farm.Tests.TestHelpers;
using TC.Agro.SharedKernel.Application.Ports;
using TC.Agro.SharedKernel.Infrastructure.UserClaims;

namespace TC.Agro.Farm.Tests.Application.UseCases.CropCycles.Transition;

public sealed class TransitionCropCycleCommandHandlerTests
{
    private readonly ICropCycleAggregateRepository _repository = A.Fake<ICropCycleAggregateRepository>();
    private readonly ITransactionalOutbox _outbox = A.Fake<ITransactionalOutbox>();
    private readonly ILogger<TransitionCropCycleCommandHandler> _logger = A.Fake<ILogger<TransitionCropCycleCommandHandler>>();

    public TransitionCropCycleCommandHandlerTests()
    {
        FastEndpointsTestBootstrap.EnsureInitialized();
    }

    [Fact]
    public async Task ExecuteAsync_WhenCropCycleDoesNotExist_ShouldReturnInvalid()
    {
        var userContext = TestUserContextFactory.CreateProducer();
        var command = new TransitionCropCycleCommand(Guid.NewGuid(), "Growing", DateTimeOffset.UtcNow, "Transition notes");

        A.CallTo(() => _repository.GetByIdAsync(command.CropCycleId, A<CancellationToken>._))
            .Returns(Task.FromResult<CropCycleAggregate?>(null));

        var sut = CreateHandler(userContext);

        var result = await sut.ExecuteAsync(command, CancellationToken.None);

        result.Status.ShouldBe(ResultStatus.Invalid);
        result.ValidationErrors.ShouldContain(error =>
            error.ErrorMessage.Contains("Crop cycle not found", StringComparison.OrdinalIgnoreCase));

        A.CallTo(() => _outbox.SaveChangesAsync(A<CancellationToken>._)).MustNotHaveHappened();
    }

    [Fact]
    public async Task ExecuteAsync_WhenCallerIsNotOwnerAndNotAdmin_ShouldReturnUnauthorized()
    {
        var ownerId = Guid.NewGuid();
        var userContext = TestUserContextFactory.CreateProducer(Guid.NewGuid());
        var cropCycle = CreateCropCycle(ownerId);
        var command = new TransitionCropCycleCommand(cropCycle.Id, "Growing", DateTimeOffset.UtcNow, "Transition notes");

        A.CallTo(() => _repository.GetByIdAsync(command.CropCycleId, A<CancellationToken>._))
            .Returns(cropCycle);

        var sut = CreateHandler(userContext);

        var result = await sut.ExecuteAsync(command, CancellationToken.None);

        result.Status.ShouldBe(ResultStatus.Unauthorized);
        result.Errors.ShouldContain(error =>
            error.Contains("not authorized", StringComparison.OrdinalIgnoreCase));

        A.CallTo(() => _repository.Add(A<CropCycleAggregate>._)).MustNotHaveHappened();
        A.CallTo(() => _outbox.SaveChangesAsync(A<CancellationToken>._)).MustNotHaveHappened();
    }

    [Fact]
    public async Task ExecuteAsync_WhenCropCycleIsAlreadyCompleted_ShouldReturnInvalid()
    {
        var ownerId = Guid.NewGuid();
        var userContext = TestUserContextFactory.CreateProducer(ownerId);
        var cropCycle = CreateCompletedCropCycle(ownerId);
        var command = new TransitionCropCycleCommand(cropCycle.Id, "Growing", DateTimeOffset.UtcNow, "Transition notes");

        A.CallTo(() => _repository.GetByIdAsync(command.CropCycleId, A<CancellationToken>._))
            .Returns(cropCycle);

        var sut = CreateHandler(userContext);

        var result = await sut.ExecuteAsync(command, CancellationToken.None);

        result.Status.ShouldBe(ResultStatus.Invalid);
        result.ValidationErrors.ShouldContain(error =>
            error.ErrorMessage.Contains("cannot transition", StringComparison.OrdinalIgnoreCase));

        A.CallTo(() => _outbox.SaveChangesAsync(A<CancellationToken>._)).MustNotHaveHappened();
    }

    [Fact]
    public async Task ExecuteAsync_WhenCommandIsValid_ShouldTransitionCropCycle()
    {
        var ownerId = Guid.NewGuid();
        var userContext = TestUserContextFactory.CreateProducer(ownerId);
        var cropCycle = CreateCropCycle(ownerId);
        var occurredAt = DateTimeOffset.UtcNow;
        var command = new TransitionCropCycleCommand(cropCycle.Id, "Growing", occurredAt, "Transition notes");

        A.CallTo(() => _repository.GetByIdAsync(command.CropCycleId, A<CancellationToken>._))
            .Returns(cropCycle);

        var sut = CreateHandler(userContext);

        var result = await sut.ExecuteAsync(command, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Id.ShouldBe(cropCycle.Id);
        result.Value.Status.ShouldBe("Growing");
        result.Value.Notes.ShouldBe("Transition notes");

        A.CallTo(() => _repository.Add(A<CropCycleAggregate>._)).MustNotHaveHappened();
        A.CallTo(() => _outbox.SaveChangesAsync(A<CancellationToken>._)).MustHaveHappenedOnceExactly();
    }

    private TransitionCropCycleCommandHandler CreateHandler(IUserContext userContext)
        => new(_repository, userContext, _outbox, _logger);

    private static CropCycleAggregate CreateCropCycle(Guid ownerId)
    {
        var result = CropCycleAggregate.Start(
            plotId: Guid.NewGuid(),
            propertyId: Guid.NewGuid(),
            ownerId: ownerId,
            cropTypeCatalogId: Guid.NewGuid(),
            startedAt: DateTimeOffset.UtcNow.AddDays(-14),
            expectedHarvestDate: DateTimeOffset.UtcNow.AddMonths(4),
            status: "Planted",
            notes: "Started");

        result.IsSuccess.ShouldBeTrue();
        return result.Value;
    }

    private static CropCycleAggregate CreateCompletedCropCycle(Guid ownerId)
    {
        var cropCycle = CreateCropCycle(ownerId);
        cropCycle.Complete(DateTimeOffset.UtcNow.AddDays(-1), "Completed", "Harvested").IsSuccess.ShouldBeTrue();
        return cropCycle;
    }
}
