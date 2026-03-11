using Ardalis.Result;
using FakeItEasy;
using Microsoft.Extensions.Logging;
using TC.Agro.Farm.Application.Abstractions.Ports;
using TC.Agro.Farm.Application.UseCases.CropCycles.Complete;
using TC.Agro.Farm.Domain.Aggregates;
using TC.Agro.Farm.Tests.TestHelpers;
using TC.Agro.SharedKernel.Application.Ports;
using TC.Agro.SharedKernel.Infrastructure.UserClaims;

namespace TC.Agro.Farm.Tests.Application.UseCases.CropCycles.Complete;

public sealed class CompleteCropCycleCommandHandlerTests
{
    private readonly ICropCycleAggregateRepository _repository = A.Fake<ICropCycleAggregateRepository>();
    private readonly ITransactionalOutbox _outbox = A.Fake<ITransactionalOutbox>();
    private readonly ILogger<CompleteCropCycleCommandHandler> _logger = A.Fake<ILogger<CompleteCropCycleCommandHandler>>();

    public CompleteCropCycleCommandHandlerTests()
    {
        FastEndpointsTestBootstrap.EnsureInitialized();
    }

    [Fact]
    public async Task ExecuteAsync_WhenCropCycleDoesNotExist_ShouldReturnInvalid()
    {
        var userContext = TestUserContextFactory.CreateProducer();
        var command = new CompleteCropCycleCommand(Guid.NewGuid(), DateTimeOffset.UtcNow, "Harvested", "Completed");

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
        var command = new CompleteCropCycleCommand(cropCycle.Id, DateTimeOffset.UtcNow, "Harvested", "Completed");

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
        var command = new CompleteCropCycleCommand(cropCycle.Id, DateTimeOffset.UtcNow, "Harvested", "Completed again");

        A.CallTo(() => _repository.GetByIdAsync(command.CropCycleId, A<CancellationToken>._))
            .Returns(cropCycle);

        var sut = CreateHandler(userContext);

        var result = await sut.ExecuteAsync(command, CancellationToken.None);

        result.Status.ShouldBe(ResultStatus.Invalid);
        result.ValidationErrors.ShouldContain(error =>
            error.ErrorMessage.Contains("already completed", StringComparison.OrdinalIgnoreCase));

        A.CallTo(() => _outbox.SaveChangesAsync(A<CancellationToken>._)).MustNotHaveHappened();
    }

    [Fact]
    public async Task ExecuteAsync_WhenCommandIsValid_ShouldCompleteCropCycle()
    {
        var ownerId = Guid.NewGuid();
        var userContext = TestUserContextFactory.CreateProducer(ownerId);
        var cropCycle = CreateCropCycle(ownerId);
        var endedAt = DateTimeOffset.UtcNow;
        var command = new CompleteCropCycleCommand(cropCycle.Id, endedAt, "Harvested", "Completed");

        A.CallTo(() => _repository.GetByIdAsync(command.CropCycleId, A<CancellationToken>._))
            .Returns(cropCycle);

        var sut = CreateHandler(userContext);

        var result = await sut.ExecuteAsync(command, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Id.ShouldBe(cropCycle.Id);
        result.Value.Status.ShouldBe("Harvested");
        result.Value.EndedAt.ShouldBe(endedAt);
        result.Value.Notes.ShouldBe("Completed");

        A.CallTo(() => _repository.Add(A<CropCycleAggregate>._)).MustNotHaveHappened();
        A.CallTo(() => _outbox.SaveChangesAsync(A<CancellationToken>._)).MustHaveHappenedOnceExactly();
    }

    private CompleteCropCycleCommandHandler CreateHandler(IUserContext userContext)
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
