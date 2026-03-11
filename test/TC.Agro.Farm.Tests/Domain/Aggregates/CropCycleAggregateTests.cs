using TC.Agro.Farm.Domain.Aggregates;
using TC.Agro.Farm.Domain.ValueObjects;

namespace TC.Agro.Farm.Tests.Domain.Aggregates
{
    public class CropCycleAggregateTests
    {
        [Fact]
        public void Start_WithValidParameters_ShouldCreateAggregateAndStartedEvent()
        {
            var startedAt = DateTimeOffset.UtcNow.AddDays(-10);
            var expectedHarvestDate = DateTimeOffset.UtcNow.AddMonths(4);

            var result = CropCycleAggregate.Start(
                plotId: Guid.NewGuid(),
                propertyId: Guid.NewGuid(),
                ownerId: Guid.NewGuid(),
                cropTypeCatalogId: Guid.NewGuid(),
                startedAt: startedAt,
                expectedHarvestDate: expectedHarvestDate,
                selectedCropTypeSuggestionId: Guid.NewGuid(),
                status: CropCycleStatus.Planted,
                notes: "  Initial cycle  ");

            result.IsSuccess.ShouldBeTrue();
            result.Value.Status.Value.ShouldBe(CropCycleStatus.Planted);
            result.Value.StartedAt.ShouldBe(startedAt);
            result.Value.ExpectedHarvestDate.ShouldBe(expectedHarvestDate);
            result.Value.Notes.ShouldBe("Initial cycle");
            result.Value.Events.Count.ShouldBe(1);
            result.Value.Events.Single().EventType.ShouldBe(CropCycleEventAggregate.StartedEventType);
            result.Value.Events.Single().Status.ShouldBe(CropCycleStatus.Planted);
        }

        [Fact]
        public void Start_WithTerminalStatus_ShouldReturnValidationError()
        {
            var result = CropCycleAggregate.Start(
                plotId: Guid.NewGuid(),
                propertyId: Guid.NewGuid(),
                ownerId: Guid.NewGuid(),
                cropTypeCatalogId: Guid.NewGuid(),
                startedAt: DateTimeOffset.UtcNow.AddDays(-5),
                status: CropCycleStatus.Harvested);

            result.IsSuccess.ShouldBeFalse();
            result.ValidationErrors.ShouldContain(error =>
                error.ErrorMessage.Contains("active lifecycle status", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public void TransitionTo_WithValidActiveStatus_ShouldAppendStatusChangedEvent()
        {
            var aggregate = CreateActiveCycle();
            var occurredAt = DateTimeOffset.UtcNow;

            var result = aggregate.TransitionTo(CropCycleStatus.Growing, occurredAt, "Growing well");

            result.IsSuccess.ShouldBeTrue();
            aggregate.Status.Value.ShouldBe(CropCycleStatus.Growing);
            aggregate.Notes.ShouldBe("Growing well");
            aggregate.Events.Count.ShouldBe(2);
            aggregate.Events.Last().EventType.ShouldBe(CropCycleEventAggregate.StatusChangedEventType);
            aggregate.Events.Last().Status.ShouldBe(CropCycleStatus.Growing);
            aggregate.Events.Last().OccurredAt.ShouldBe(occurredAt);
        }

        [Fact]
        public void TransitionTo_WhenCycleIsAlreadyCompleted_ShouldReturnValidationError()
        {
            var aggregate = CreateCompletedCycle();

            var result = aggregate.TransitionTo(CropCycleStatus.Growing, DateTimeOffset.UtcNow, "Should fail");

            result.IsSuccess.ShouldBeFalse();
            result.ValidationErrors.ShouldContain(error =>
                error.ErrorMessage.Contains("cannot transition", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public void Complete_WithEndedAtBeforeStartedAt_ShouldReturnValidationError()
        {
            var aggregate = CreateActiveCycle();

            var result = aggregate.Complete(
                endedAt: aggregate.StartedAt.AddMinutes(-1),
                notes: "Invalid completion",
                finalStatus: CropCycleStatus.Harvested);

            result.IsSuccess.ShouldBeFalse();
            result.ValidationErrors.ShouldContain(error =>
                error.ErrorMessage.Contains("cannot be before StartedAt", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public void Complete_WithValidTerminalStatus_ShouldSetEndedAtAndAppendCompletedEvent()
        {
            var aggregate = CreateActiveCycle();
            var endedAt = DateTimeOffset.UtcNow;

            var result = aggregate.Complete(endedAt, "Harvest finished", CropCycleStatus.Harvested);

            result.IsSuccess.ShouldBeTrue();
            aggregate.Status.Value.ShouldBe(CropCycleStatus.Harvested);
            aggregate.EndedAt.ShouldBe(endedAt);
            aggregate.Notes.ShouldBe("Harvest finished");
            aggregate.Events.Count.ShouldBe(2);
            aggregate.Events.Last().EventType.ShouldBe(CropCycleEventAggregate.CompletedEventType);
            aggregate.Events.Last().Status.ShouldBe(CropCycleStatus.Harvested);
            aggregate.Events.Last().OccurredAt.ShouldBe(endedAt);
        }

        private static CropCycleAggregate CreateActiveCycle()
        {
            var result = CropCycleAggregate.Start(
                plotId: Guid.NewGuid(),
                propertyId: Guid.NewGuid(),
                ownerId: Guid.NewGuid(),
                cropTypeCatalogId: Guid.NewGuid(),
                startedAt: DateTimeOffset.UtcNow.AddDays(-7),
                expectedHarvestDate: DateTimeOffset.UtcNow.AddMonths(3),
                status: CropCycleStatus.Planted,
                notes: "Started");

            result.IsSuccess.ShouldBeTrue();
            return result.Value;
        }

        private static CropCycleAggregate CreateCompletedCycle()
        {
            var aggregate = CreateActiveCycle();
            aggregate.Complete(DateTimeOffset.UtcNow.AddDays(-1), "Completed", CropCycleStatus.Harvested).IsSuccess.ShouldBeTrue();
            return aggregate;
        }
    }
}
