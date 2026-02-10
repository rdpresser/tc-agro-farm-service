namespace TC.Agro.Farm.Domain.Aggregates
{
    /// <summary>
    /// Sensor aggregate root - represents a sensor installed in a plot.
    /// </summary>
    public sealed class SensorAggregate : BaseAggregateRoot
    {
        public SensorType Type { get; private set; } = default!;
        public SensorStatus Status { get; private set; } = default!;
        public DateTimeOffset InstalledAt { get; private set; }
        public Name? Label { get; private set; }

        public Guid PlotId { get; private set; }
        public PlotAggregate Plot { get; private set; } = default!;

        // Private constructor for factories and ORM
        private SensorAggregate(Guid id) : base(id) { }

        // Parameterless constructor for EF Core
        private SensorAggregate() { }

        #region Factories

        public static Result<SensorAggregate> Create(
            Guid plotId,
            string type,
            string? label = null)
        {
            var typeResult = ValueObjects.SensorType.Create(type);

            var errors = new List<ValidationError>();
            errors.AddRange(ValidatePlotId(plotId));
            errors.AddErrorsIfFailure(typeResult);

            // Label is optional, but if provided must be valid
            Result<Name>? labelResult = null;
            if (!string.IsNullOrWhiteSpace(label))
            {
                labelResult = ValueObjects.Name.Create(label);
                errors.AddErrorsIfFailure(labelResult);
            }

            if (errors.Count > 0)
            {
                return Result.Invalid(errors.ToArray());
            }

            return CreateAggregate(plotId, typeResult.Value, labelResult?.Value);
        }

        private static Result<SensorAggregate> CreateAggregate(Guid plotId, SensorType type, Name? label)
        {
            var aggregate = new SensorAggregate(Guid.NewGuid());
            var @event = new SensorRegisteredDomainEvent(
                aggregate.Id,
                plotId,
                type.Value,
                SensorStatus.Active,
                label?.Value,
                DateTimeOffset.UtcNow);

            aggregate.ApplyEvent(@event);
            return Result.Success(aggregate);
        }

        #endregion

        #region Commands

        public Result UpdateLabel(string? label)
        {
            Name? labelValue = null;

            if (!string.IsNullOrWhiteSpace(label))
            {
                var labelResult = ValueObjects.Name.Create(label);
                if (!labelResult.IsSuccess)
                {
                    return Result.Invalid(labelResult.ValidationErrors);
                }

                labelValue = labelResult.Value;
            }

            var @event = new SensorLabelUpdatedDomainEvent(
                Id,
                labelValue?.Value,
                DateTimeOffset.UtcNow);

            ApplyEvent(@event);
            return Result.Success();
        }

        public Result SetActive()
        {
            if (Status.IsActive)
            {
                return Result.Invalid(FarmDomainErrors.SensorAlreadyActive);
            }

            var @event = new SensorStatusChangedDomainEvent(
                Id,
                SensorStatus.Active,
                DateTimeOffset.UtcNow);

            ApplyEvent(@event);
            return Result.Success();
        }

        public Result SetInactive()
        {
            if (Status.IsInactive)
            {
                return Result.Invalid(FarmDomainErrors.SensorAlreadyInactive);
            }

            var @event = new SensorStatusChangedDomainEvent(
                Id,
                SensorStatus.Inactive,
                DateTimeOffset.UtcNow);

            ApplyEvent(@event);
            return Result.Success();
        }

        public Result SetMaintenance()
        {
            if (Status.IsMaintenance)
            {
                return Result.Invalid(FarmDomainErrors.SensorAlreadyInMaintenance);
            }

            var @event = new SensorStatusChangedDomainEvent(
                Id,
                SensorStatus.Maintenance,
                DateTimeOffset.UtcNow);

            ApplyEvent(@event);
            return Result.Success();
        }

        public Result SetFaulty()
        {
            if (Status.IsFaulty)
            {
                return Result.Invalid(FarmDomainErrors.SensorAlreadyFaulty);
            }

            var @event = new SensorStatusChangedDomainEvent(
                Id,
                SensorStatus.Faulty,
                DateTimeOffset.UtcNow);

            ApplyEvent(@event);
            return Result.Success();
        }

        public Result Deactivate()
        {
            if (!IsActive)
            {
                return Result.Invalid(FarmDomainErrors.SensorAlreadyDeactivated);
            }

            var @event = new SensorDeactivatedDomainEvent(Id, DateTimeOffset.UtcNow);
            ApplyEvent(@event);
            return Result.Success();
        }

        public Result Activate()
        {
            if (IsActive)
            {
                return Result.Invalid(FarmDomainErrors.SensorAlreadyActivated);
            }

            var @event = new SensorActivatedDomainEvent(Id, DateTimeOffset.UtcNow);
            ApplyEvent(@event);
            return Result.Success();
        }

        #endregion

        #region Event Handlers

        public void Apply(SensorRegisteredDomainEvent @event)
        {
            SetId(@event.AggregateId);
            PlotId = @event.PlotId;
            Type = ValueObjects.SensorType.FromDb(@event.Type).Value;
            Status = ValueObjects.SensorStatus.FromDb(@event.Status).Value;
            InstalledAt = @event.OccurredOn;

            if (!string.IsNullOrWhiteSpace(@event.Label))
            {
                Label = ValueObjects.Name.FromDb(@event.Label).Value;
            }

            SetCreatedAt(@event.OccurredOn);
            SetActivate();
        }

        public void Apply(SensorLabelUpdatedDomainEvent @event)
        {
            Label = string.IsNullOrWhiteSpace(@event.Label)
                ? null
                : ValueObjects.Name.FromDb(@event.Label).Value;
            SetUpdatedAt(@event.OccurredOn);
        }

        public void Apply(SensorStatusChangedDomainEvent @event)
        {
            Status = ValueObjects.SensorStatus.FromDb(@event.Status).Value;
            SetUpdatedAt(@event.OccurredOn);
        }

        public void Apply(SensorDeactivatedDomainEvent @event)
        {
            SetDeactivate();
            SetUpdatedAt(@event.OccurredOn);
        }

        public void Apply(SensorActivatedDomainEvent @event)
        {
            SetActivate();
            SetUpdatedAt(@event.OccurredOn);
        }

        private void ApplyEvent(BaseDomainEvent @event)
        {
            AddNewEvent(@event);
            switch (@event)
            {
                case SensorRegisteredDomainEvent registeredEvent:
                    Apply(registeredEvent);
                    break;
                case SensorLabelUpdatedDomainEvent labelUpdatedEvent:
                    Apply(labelUpdatedEvent);
                    break;
                case SensorStatusChangedDomainEvent statusChangedEvent:
                    Apply(statusChangedEvent);
                    break;
                case SensorDeactivatedDomainEvent deactivatedEvent:
                    Apply(deactivatedEvent);
                    break;
                case SensorActivatedDomainEvent activatedEvent:
                    Apply(activatedEvent);
                    break;
            }
        }

        #endregion

        #region Validation Helpers

        private static IEnumerable<ValidationError> ValidatePlotId(Guid plotId)
        {
            if (plotId == Guid.Empty)
            {
                yield return FarmDomainErrors.PlotIdRequired;
            }
        }

        #endregion

        #region Domain Events

        public record SensorRegisteredDomainEvent(
            Guid AggregateId,
            Guid PlotId,
            string Type,
            string Status,
            string? Label,
            DateTimeOffset OccurredOn) : BaseDomainEvent(AggregateId, OccurredOn);

        public record SensorLabelUpdatedDomainEvent(
            Guid AggregateId,
            string? Label,
            DateTimeOffset OccurredOn) : BaseDomainEvent(AggregateId, OccurredOn);

        public record SensorStatusChangedDomainEvent(
            Guid AggregateId,
            string Status,
            DateTimeOffset OccurredOn) : BaseDomainEvent(AggregateId, OccurredOn);

        public record SensorDeactivatedDomainEvent(
            Guid AggregateId,
            DateTimeOffset OccurredOn) : BaseDomainEvent(AggregateId, OccurredOn);

        public record SensorActivatedDomainEvent(
            Guid AggregateId,
            DateTimeOffset OccurredOn) : BaseDomainEvent(AggregateId, OccurredOn);

        #endregion
    }
}
