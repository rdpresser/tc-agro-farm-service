using TC.Agro.Farm.Domain.Abstractions;
using TC.Agro.Farm.Domain.Snapshots;
using TC.Agro.Farm.Domain.ValueObjects;

namespace TC.Agro.Farm.Domain.Aggregates
{
    /// <summary>
    /// Aggregate root that models a crop lifecycle running on a plot.
    /// </summary>
    public sealed class CropCycleAggregate : BaseAggregateRoot, ITenantAware
    {
        private const int MaxNotesLength = 1000;

        public Guid PlotId { get; private set; }
        public Guid PropertyId { get; private set; }
        public Guid OwnerId { get; private set; }
        public Guid CropTypeCatalogId { get; private set; }
        public Guid? SelectedCropTypeSuggestionId { get; private set; }
        public CropCycleStatus Status { get; private set; } = default!;
        public string? Notes { get; private set; }
        public DateTimeOffset StartedAt { get; private set; }
        public DateTimeOffset? ExpectedHarvestDate { get; private set; }
        public DateTimeOffset? EndedAt { get; private set; }

        public PlotAggregate Plot { get; private set; } = default!;
        public PropertyAggregate Property { get; private set; } = default!;
        public OwnerSnapshot Owner { get; private set; } = default!;
        public CropTypeCatalogAggregate CropTypeCatalog { get; private set; } = default!;
        public CropTypeSuggestionAggregate? SelectedCropTypeSuggestion { get; private set; }
        public ICollection<CropCycleEventAggregate> Events { get; private set; } = [];

        private CropCycleAggregate(Guid id) : base(id) { }
        private CropCycleAggregate() { }

        public static Result<CropCycleAggregate> Start(
            Guid plotId,
            Guid propertyId,
            Guid ownerId,
            Guid cropTypeCatalogId,
            DateTimeOffset startedAt,
            DateTimeOffset? expectedHarvestDate = null,
            Guid? selectedCropTypeSuggestionId = null,
            string status = CropCycleStatus.Planned,
            string? notes = null)
        {
            var statusResult = CropCycleStatus.Create(status);
            var errors = ValidateIdentifiers(plotId, propertyId, ownerId, cropTypeCatalogId).ToList();
            errors.AddErrorsIfFailure(statusResult);
            errors.AddRange(ValidateDates(startedAt, expectedHarvestDate));
            errors.AddRange(ValidateOptionalValues(selectedCropTypeSuggestionId, notes));

            if (statusResult.IsSuccess && !statusResult.Value.IsActiveCycle)
            {
                errors.Add(new ValidationError(
                    "CropCycle.Status",
                    "A crop cycle must start in an active lifecycle status."));
            }

            if (errors.Count > 0)
            {
                return Result.Invalid(errors.ToArray());
            }

            var aggregate = new CropCycleAggregate(Guid.NewGuid());
            var @event = new CropCycleStartedDomainEvent(
                aggregate.Id,
                plotId,
                propertyId,
                ownerId,
                cropTypeCatalogId,
                selectedCropTypeSuggestionId,
                statusResult.Value.Value,
                NormalizeNotes(notes),
                startedAt,
                expectedHarvestDate,
                DateTimeOffset.UtcNow);

            aggregate.ApplyEvent(@event);
            return Result.Success(aggregate);
        }

        public Result TransitionTo(string status, DateTimeOffset occurredAt, string? notes = null)
        {
            var statusResult = CropCycleStatus.Create(status);
            var errors = new List<ValidationError>();
            errors.AddErrorsIfFailure(statusResult);
            errors.AddRange(ValidateOptionalValues(null, notes));

            if (occurredAt == default)
            {
                errors.Add(new ValidationError("CropCycle.OccurredAt", "OccurredAt is required."));
            }

            if (EndedAt.HasValue)
            {
                errors.Add(new ValidationError(
                    "CropCycle.Status",
                    "Completed crop cycles cannot transition to another status."));
            }

            if (errors.Count > 0)
            {
                return Result.Invalid(errors.ToArray());
            }

            var @event = new CropCycleStatusChangedDomainEvent(
                Id,
                statusResult.Value.Value,
                NormalizeNotes(notes),
                occurredAt);

            ApplyEvent(@event);
            return Result.Success();
        }

        public Result Complete(DateTimeOffset endedAt, string? notes = null, string finalStatus = CropCycleStatus.Harvested)
        {
            var statusResult = CropCycleStatus.Create(finalStatus);
            var errors = new List<ValidationError>();
            errors.AddErrorsIfFailure(statusResult);
            errors.AddRange(ValidateOptionalValues(null, notes));

            if (endedAt == default)
            {
                errors.Add(new ValidationError("CropCycle.EndedAt", "EndedAt is required."));
            }

            if (endedAt < StartedAt)
            {
                errors.Add(new ValidationError("CropCycle.EndedAt", "EndedAt cannot be before StartedAt."));
            }

            if (statusResult.IsSuccess && statusResult.Value.IsActiveCycle)
            {
                errors.Add(new ValidationError(
                    "CropCycle.Status",
                    "Completed crop cycles must finish in a terminal lifecycle status."));
            }

            if (EndedAt.HasValue)
            {
                errors.Add(new ValidationError(
                    "CropCycle.Status",
                    "Crop cycle is already completed."));
            }

            if (errors.Count > 0)
            {
                return Result.Invalid(errors.ToArray());
            }

            var @event = new CropCycleCompletedDomainEvent(
                Id,
                statusResult.Value.Value,
                NormalizeNotes(notes),
                endedAt,
                DateTimeOffset.UtcNow);

            ApplyEvent(@event);
            return Result.Success();
        }

        public void Apply(CropCycleStartedDomainEvent @event)
        {
            SetId(@event.AggregateId);
            PlotId = @event.PlotId;
            PropertyId = @event.PropertyId;
            OwnerId = @event.OwnerId;
            CropTypeCatalogId = @event.CropTypeCatalogId;
            SelectedCropTypeSuggestionId = @event.SelectedCropTypeSuggestionId;
            Status = CropCycleStatus.FromDb(@event.Status).Value;
            Notes = @event.Notes;
            StartedAt = @event.StartedAt;
            ExpectedHarvestDate = @event.ExpectedHarvestDate;
            EndedAt = null;
            SelectedCropTypeSuggestion = null;
            RegisterLifecycleEvent(CropCycleEventAggregate.StartedEventType, Status.Value, Notes, @event.OccurredOn);
            SetCreatedAt(@event.OccurredOn);
            SetActivate();
        }

        public void Apply(CropCycleStatusChangedDomainEvent @event)
        {
            Status = CropCycleStatus.FromDb(@event.Status).Value;
            Notes = @event.Notes;
            RegisterLifecycleEvent(CropCycleEventAggregate.StatusChangedEventType, Status.Value, Notes, @event.OccurredOn);
            SetUpdatedAt(@event.OccurredOn);
        }

        public void Apply(CropCycleCompletedDomainEvent @event)
        {
            Status = CropCycleStatus.FromDb(@event.Status).Value;
            Notes = @event.Notes;
            EndedAt = @event.EndedAt;
            RegisterLifecycleEvent(CropCycleEventAggregate.CompletedEventType, Status.Value, Notes, @event.EndedAt);
            SetUpdatedAt(@event.OccurredOn);
        }

        private void ApplyEvent(BaseDomainEvent @event)
        {
            AddNewEvent(@event);

            switch (@event)
            {
                case CropCycleStartedDomainEvent startedEvent:
                    Apply(startedEvent);
                    break;
                case CropCycleStatusChangedDomainEvent statusChangedEvent:
                    Apply(statusChangedEvent);
                    break;
                case CropCycleCompletedDomainEvent completedEvent:
                    Apply(completedEvent);
                    break;
            }
        }

        private void RegisterLifecycleEvent(string eventType, string status, string? notes, DateTimeOffset occurredAt)
        {
            var eventResult = CropCycleEventAggregate.Create(
                Id,
                PlotId,
                PropertyId,
                OwnerId,
                eventType,
                status,
                occurredAt,
                notes);

            if (eventResult.IsSuccess)
            {
                var cycleEvent = eventResult.Value;
                cycleEvent.BindToCycle(this);
                Events.Add(cycleEvent);
            }
        }

        private static IEnumerable<ValidationError> ValidateIdentifiers(
            Guid plotId,
            Guid propertyId,
            Guid ownerId,
            Guid cropTypeCatalogId)
        {
            if (plotId == Guid.Empty)
            {
                yield return new ValidationError("CropCycle.PlotId", "PlotId is required.");
            }

            if (propertyId == Guid.Empty)
            {
                yield return new ValidationError("CropCycle.PropertyId", "PropertyId is required.");
            }

            if (ownerId == Guid.Empty)
            {
                yield return FarmDomainErrors.OwnerIdRequired;
            }

            if (cropTypeCatalogId == Guid.Empty)
            {
                yield return new ValidationError("CropCycle.CropTypeCatalogId", "CropTypeCatalogId is required.");
            }
        }

        private static IEnumerable<ValidationError> ValidateDates(
            DateTimeOffset startedAt,
            DateTimeOffset? expectedHarvestDate)
        {
            if (startedAt == default)
            {
                yield return new ValidationError("CropCycle.StartedAt", "StartedAt is required.");
            }

            if (expectedHarvestDate.HasValue && expectedHarvestDate.Value <= startedAt)
            {
                yield return new ValidationError(
                    "CropCycle.ExpectedHarvestDate",
                    "ExpectedHarvestDate must be after StartedAt.");
            }
        }

        private static IEnumerable<ValidationError> ValidateOptionalValues(Guid? selectedCropTypeSuggestionId, string? notes)
        {
            if (selectedCropTypeSuggestionId.HasValue && selectedCropTypeSuggestionId.Value == Guid.Empty)
            {
                yield return new ValidationError(
                    "CropCycle.SelectedCropTypeSuggestionId",
                    "SelectedCropTypeSuggestionId cannot be empty when informed.");
            }

            if (!string.IsNullOrWhiteSpace(notes) && notes.Trim().Length > MaxNotesLength)
            {
                yield return new ValidationError(
                    "CropCycle.Notes",
                    $"Notes cannot exceed {MaxNotesLength} characters.");
            }
        }

        private static string? NormalizeNotes(string? notes)
            => string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();

        public record CropCycleStartedDomainEvent(
            Guid AggregateId,
            Guid PlotId,
            Guid PropertyId,
            Guid OwnerId,
            Guid CropTypeCatalogId,
            Guid? SelectedCropTypeSuggestionId,
            string Status,
            string? Notes,
            DateTimeOffset StartedAt,
            DateTimeOffset? ExpectedHarvestDate,
            DateTimeOffset OccurredOn) : BaseDomainEvent(AggregateId, OccurredOn);

        public record CropCycleStatusChangedDomainEvent(
            Guid AggregateId,
            string Status,
            string? Notes,
            DateTimeOffset OccurredOn) : BaseDomainEvent(AggregateId, OccurredOn);

        public record CropCycleCompletedDomainEvent(
            Guid AggregateId,
            string Status,
            string? Notes,
            DateTimeOffset EndedAt,
            DateTimeOffset OccurredOn) : BaseDomainEvent(AggregateId, OccurredOn);
    }
}
