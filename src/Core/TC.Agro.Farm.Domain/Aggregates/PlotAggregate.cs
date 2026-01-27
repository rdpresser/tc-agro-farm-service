namespace TC.Agro.Farm.Domain.Aggregates
{
    /// <summary>
    /// Plot aggregate root - represents a plot within a property.
    /// </summary>
    public sealed class PlotAggregate : BaseAggregateRoot
    {
        public Guid PropertyId { get; private set; }
        public Name Name { get; private set; } = default!;
        public CropType CropType { get; private set; } = default!;
        public Area AreaHectares { get; private set; } = default!;

        // Private constructor for factories and ORM
        private PlotAggregate(Guid id) : base(id) { }

        // Parameterless constructor for EF Core
        private PlotAggregate() { }

        #region Factories

        public static Result<PlotAggregate> Create(
            Guid propertyId,
            string name,
            string cropType,
            double areaHectares)
        {
            var nameResult = ValueObjects.Name.Create(name);
            var cropTypeResult = ValueObjects.CropType.Create(cropType);
            var areaResult = Area.Create(areaHectares);

            var errors = new List<ValidationError>();
            errors.AddRange(ValidatePropertyId(propertyId));
            errors.AddErrorsIfFailure(nameResult);
            errors.AddErrorsIfFailure(cropTypeResult);
            errors.AddErrorsIfFailure(areaResult);

            if (errors.Count > 0)
            {
                return Result.Invalid(errors.ToArray());
            }

            return CreateAggregate(propertyId, nameResult.Value, cropTypeResult.Value, areaResult.Value);
        }

        private static Result<PlotAggregate> CreateAggregate(Guid propertyId, Name name, CropType cropType, Area area)
        {
            var aggregate = new PlotAggregate(Guid.NewGuid());
            var @event = new PlotCreatedDomainEvent(
                aggregate.Id,
                propertyId,
                name.Value,
                cropType.Value,
                area.Hectares,
                DateTimeOffset.UtcNow);

            aggregate.ApplyEvent(@event);
            return Result.Success(aggregate);
        }

        #endregion

        #region Commands

        public Result Update(string name, string cropType, double areaHectares)
        {
            var nameResult = ValueObjects.Name.Create(name);
            var cropTypeResult = ValueObjects.CropType.Create(cropType);
            var areaResult = Area.Create(areaHectares);

            var errors = new List<ValidationError>();
            errors.AddErrorsIfFailure(nameResult);
            errors.AddErrorsIfFailure(cropTypeResult);
            errors.AddErrorsIfFailure(areaResult);

            if (errors.Count > 0)
            {
                return Result.Invalid(errors.ToArray());
            }

            var @event = new PlotUpdatedDomainEvent(
                Id,
                nameResult.Value.Value,
                cropTypeResult.Value.Value,
                areaResult.Value.Hectares,
                DateTimeOffset.UtcNow);

            ApplyEvent(@event);
            return Result.Success();
        }

        public Result ChangeCropType(string cropType)
        {
            var cropTypeResult = ValueObjects.CropType.Create(cropType);

            if (!cropTypeResult.IsSuccess)
            {
                return Result.Invalid(cropTypeResult.ValidationErrors);
            }

            var @event = new PlotCropTypeChangedDomainEvent(
                Id,
                cropTypeResult.Value.Value,
                DateTimeOffset.UtcNow);

            ApplyEvent(@event);
            return Result.Success();
        }

        public Result Deactivate()
        {
            if (!IsActive)
            {
                return Result.Invalid(FarmDomainErrors.PlotAlreadyDeactivated);
            }

            var @event = new PlotDeactivatedDomainEvent(Id, DateTimeOffset.UtcNow);
            ApplyEvent(@event);
            return Result.Success();
        }

        public Result Activate()
        {
            if (IsActive)
            {
                return Result.Invalid(FarmDomainErrors.PlotAlreadyActivated);
            }

            var @event = new PlotActivatedDomainEvent(Id, DateTimeOffset.UtcNow);
            ApplyEvent(@event);
            return Result.Success();
        }

        #endregion

        #region Event Handlers

        public void Apply(PlotCreatedDomainEvent @event)
        {
            SetId(@event.AggregateId);
            PropertyId = @event.PropertyId;
            Name = ValueObjects.Name.FromDb(@event.Name).Value;
            CropType = ValueObjects.CropType.FromDb(@event.CropType).Value;
            AreaHectares = Area.FromDb(@event.AreaHectares).Value;
            SetCreatedAt(@event.OccurredOn);
            SetActivate();
        }

        public void Apply(PlotUpdatedDomainEvent @event)
        {
            Name = ValueObjects.Name.FromDb(@event.Name).Value;
            CropType = ValueObjects.CropType.FromDb(@event.CropType).Value;
            AreaHectares = Area.FromDb(@event.AreaHectares).Value;
            SetUpdatedAt(@event.OccurredOn);
        }

        public void Apply(PlotCropTypeChangedDomainEvent @event)
        {
            CropType = ValueObjects.CropType.FromDb(@event.CropType).Value;
            SetUpdatedAt(@event.OccurredOn);
        }

        public void Apply(PlotDeactivatedDomainEvent @event)
        {
            SetDeactivate();
            SetUpdatedAt(@event.OccurredOn);
        }

        public void Apply(PlotActivatedDomainEvent @event)
        {
            SetActivate();
            SetUpdatedAt(@event.OccurredOn);
        }

        private void ApplyEvent(BaseDomainEvent @event)
        {
            AddNewEvent(@event);
            switch (@event)
            {
                case PlotCreatedDomainEvent createdEvent:
                    Apply(createdEvent);
                    break;
                case PlotUpdatedDomainEvent updatedEvent:
                    Apply(updatedEvent);
                    break;
                case PlotCropTypeChangedDomainEvent cropTypeChangedEvent:
                    Apply(cropTypeChangedEvent);
                    break;
                case PlotDeactivatedDomainEvent deactivatedEvent:
                    Apply(deactivatedEvent);
                    break;
                case PlotActivatedDomainEvent activatedEvent:
                    Apply(activatedEvent);
                    break;
            }
        }

        #endregion

        #region Validation Helpers

        private static IEnumerable<ValidationError> ValidatePropertyId(Guid propertyId)
        {
            if (propertyId == Guid.Empty)
            {
                yield return FarmDomainErrors.PropertyIdRequired;
            }
        }

        #endregion

        #region Domain Events

        public record PlotCreatedDomainEvent(
            Guid AggregateId,
            Guid PropertyId,
            string Name,
            string CropType,
            double AreaHectares,
            DateTimeOffset OccurredOn) : BaseDomainEvent(AggregateId, OccurredOn);

        public record PlotUpdatedDomainEvent(
            Guid AggregateId,
            string Name,
            string CropType,
            double AreaHectares,
            DateTimeOffset OccurredOn) : BaseDomainEvent(AggregateId, OccurredOn);

        public record PlotCropTypeChangedDomainEvent(
            Guid AggregateId,
            string CropType,
            DateTimeOffset OccurredOn) : BaseDomainEvent(AggregateId, OccurredOn);

        public record PlotDeactivatedDomainEvent(
            Guid AggregateId,
            DateTimeOffset OccurredOn) : BaseDomainEvent(AggregateId, OccurredOn);

        public record PlotActivatedDomainEvent(
            Guid AggregateId,
            DateTimeOffset OccurredOn) : BaseDomainEvent(AggregateId, OccurredOn);

        #endregion
    }
}
