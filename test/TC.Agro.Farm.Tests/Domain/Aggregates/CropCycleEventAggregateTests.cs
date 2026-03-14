using TC.Agro.Farm.Domain.Aggregates;
using TC.Agro.Farm.Domain.ValueObjects;

namespace TC.Agro.Farm.Tests.Domain.Aggregates
{
    public class CropCycleEventAggregateTests
    {
        [Fact]
        public void Create_WithValidParameters_ShouldSucceed()
        {
            var occurredAt = DateTimeOffset.UtcNow;

            var result = CropCycleEventAggregate.Create(
                cropCycleId: Guid.NewGuid(),
                plotId: Guid.NewGuid(),
                propertyId: Guid.NewGuid(),
                ownerId: Guid.NewGuid(),
                eventType: CropCycleEventAggregate.StatusChangedEventType,
                status: CropCycleStatus.Growing,
                occurredAt: occurredAt,
                notes: "  Crop is growing  ");

            result.IsSuccess.ShouldBeTrue();
            result.Value.EventType.ShouldBe(CropCycleEventAggregate.StatusChangedEventType);
            result.Value.Status.ShouldBe(CropCycleStatus.Growing);
            result.Value.OccurredAt.ShouldBe(occurredAt);
            result.Value.Notes.ShouldBe("Crop is growing");
            result.Value.CreatedAt.ShouldNotBe(default);
        }

        [Fact]
        public void Create_WithInvalidStatus_ShouldReturnValidationError()
        {
            var result = CropCycleEventAggregate.Create(
                cropCycleId: Guid.NewGuid(),
                plotId: Guid.NewGuid(),
                propertyId: Guid.NewGuid(),
                ownerId: Guid.NewGuid(),
                eventType: CropCycleEventAggregate.StartedEventType,
                status: "Unknown",
                occurredAt: DateTimeOffset.UtcNow);

            result.IsSuccess.ShouldBeFalse();
            result.ValidationErrors.ShouldContain(error => error.Identifier == CropCycleStatus.InvalidValue.Identifier);
        }

        [Fact]
        public void Create_WithNotesLongerThanOneThousandCharacters_ShouldReturnValidationError()
        {
            var notes = new string('n', 1001);

            var result = CropCycleEventAggregate.Create(
                cropCycleId: Guid.NewGuid(),
                plotId: Guid.NewGuid(),
                propertyId: Guid.NewGuid(),
                ownerId: Guid.NewGuid(),
                eventType: CropCycleEventAggregate.CompletedEventType,
                status: CropCycleStatus.Harvested,
                occurredAt: DateTimeOffset.UtcNow,
                notes: notes);

            result.IsSuccess.ShouldBeFalse();
            result.ValidationErrors.ShouldContain(error => error.Identifier == "CropCycleEvent.Notes");
        }

        [Fact]
        public void BindToCycle_WithValidCycle_ShouldAssignNavigation()
        {
            var cycle = CropCycleAggregate.Start(
                plotId: Guid.NewGuid(),
                propertyId: Guid.NewGuid(),
                ownerId: Guid.NewGuid(),
                cropTypeCatalogId: Guid.NewGuid(),
                startedAt: DateTimeOffset.UtcNow.AddDays(-3),
                status: CropCycleStatus.Planted).Value;

            var cropCycleEvent = CropCycleEventAggregate.Create(
                cropCycleId: cycle.Id,
                plotId: cycle.PlotId,
                propertyId: cycle.PropertyId,
                ownerId: cycle.OwnerId,
                eventType: CropCycleEventAggregate.StatusChangedEventType,
                status: CropCycleStatus.Growing,
                occurredAt: DateTimeOffset.UtcNow,
                notes: "Transitioned").Value;

            cropCycleEvent.BindToCycle(cycle);

            cropCycleEvent.CropCycle.ShouldBe(cycle);
            cropCycleEvent.CropCycleId.ShouldBe(cycle.Id);
        }
    }
}
