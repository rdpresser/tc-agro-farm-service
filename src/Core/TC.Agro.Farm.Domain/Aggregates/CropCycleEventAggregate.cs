using TC.Agro.Farm.Domain.ValueObjects;

namespace TC.Agro.Farm.Domain.Aggregates
{
    /// <summary>
    /// Immutable record of significant lifecycle transitions within a crop cycle.
    /// </summary>
    public sealed class CropCycleEventAggregate
    {
        private const int MaxEventTypeLength = 50;
        private const int MaxNotesLength = 1000;

        public const string StartedEventType = "Started";
        public const string StatusChangedEventType = "StatusChanged";
        public const string CompletedEventType = "Completed";

        public Guid Id { get; private set; }
        public Guid CropCycleId { get; private set; }
        public Guid PlotId { get; private set; }
        public Guid PropertyId { get; private set; }
        public Guid OwnerId { get; private set; }
        public string EventType { get; private set; } = default!;
        public string Status { get; private set; } = default!;
        public string? Notes { get; private set; }
        public DateTimeOffset OccurredAt { get; private set; }
        public DateTimeOffset CreatedAt { get; private set; }

        public CropCycleAggregate CropCycle { get; private set; } = default!;

        private CropCycleEventAggregate() { }

        private CropCycleEventAggregate(
            Guid id,
            Guid cropCycleId,
            Guid plotId,
            Guid propertyId,
            Guid ownerId,
            string eventType,
            string status,
            string? notes,
            DateTimeOffset occurredAt,
            DateTimeOffset createdAt)
        {
            Id = id;
            CropCycleId = cropCycleId;
            PlotId = plotId;
            PropertyId = propertyId;
            OwnerId = ownerId;
            EventType = eventType;
            Status = status;
            Notes = notes;
            OccurredAt = occurredAt;
            CreatedAt = createdAt;
        }

        public static Result<CropCycleEventAggregate> Create(
            Guid cropCycleId,
            Guid plotId,
            Guid propertyId,
            Guid ownerId,
            string eventType,
            string status,
            DateTimeOffset occurredAt,
            string? notes = null)
        {
            var statusResult = CropCycleStatus.Create(status);
            var errors = new List<ValidationError>();

            if (cropCycleId == Guid.Empty)
            {
                errors.Add(new ValidationError("CropCycleEvent.CropCycleId", "CropCycleId is required."));
            }

            if (plotId == Guid.Empty)
            {
                errors.Add(new ValidationError("CropCycleEvent.PlotId", "PlotId is required."));
            }

            if (propertyId == Guid.Empty)
            {
                errors.Add(new ValidationError("CropCycleEvent.PropertyId", "PropertyId is required."));
            }

            if (ownerId == Guid.Empty)
            {
                errors.Add(new ValidationError("CropCycleEvent.OwnerId", "OwnerId is required."));
            }

            if (string.IsNullOrWhiteSpace(eventType))
            {
                errors.Add(new ValidationError("CropCycleEvent.EventType", "EventType is required."));
            }
            else if (eventType.Trim().Length > MaxEventTypeLength)
            {
                errors.Add(new ValidationError(
                    "CropCycleEvent.EventType",
                    $"EventType cannot exceed {MaxEventTypeLength} characters."));
            }

            errors.AddErrorsIfFailure(statusResult);

            if (!string.IsNullOrWhiteSpace(notes) && notes.Trim().Length > MaxNotesLength)
            {
                errors.Add(new ValidationError(
                    "CropCycleEvent.Notes",
                    $"Notes cannot exceed {MaxNotesLength} characters."));
            }

            if (occurredAt == default)
            {
                errors.Add(new ValidationError("CropCycleEvent.OccurredAt", "OccurredAt is required."));
            }

            if (errors.Count > 0)
            {
                return Result.Invalid(errors.ToArray());
            }

            return Result.Success(new CropCycleEventAggregate(
                Guid.NewGuid(),
                cropCycleId,
                plotId,
                propertyId,
                ownerId,
                eventType.Trim(),
                statusResult.Value.Value,
                string.IsNullOrWhiteSpace(notes) ? null : notes.Trim(),
                occurredAt,
                DateTimeOffset.UtcNow));
        }

        internal void BindToCycle(CropCycleAggregate cropCycle)
        {
            CropCycle = cropCycle ?? throw new ArgumentNullException(nameof(cropCycle));
        }
    }
}
