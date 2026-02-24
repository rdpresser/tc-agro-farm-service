using TC.Agro.Farm.Domain.Abstractions;
using TC.Agro.Farm.Domain.Snapshots;

namespace TC.Agro.Farm.Domain.Aggregates
{
    /// <summary>
    /// Plot aggregate root - represents a plot within a property.
    /// </summary>
    public sealed class PlotAggregate : BaseAggregateRoot, ITenantAware
    {
        public Name Name { get; private set; } = default!;
        public CropType CropType { get; private set; } = default!;
        public Area AreaHectares { get; private set; } = default!;
        public DateTimeOffset PlantingDate { get; private set; }
        public DateTimeOffset ExpectedHarvestDate { get; private set; }
        public IrrigationType IrrigationType { get; private set; } = default!;
        public AdditionalNotes? AdditionalNotes { get; private set; }

        public Guid PropertyId { get; private set; }
        public Guid OwnerId { get; private set; }
        public PropertyAggregate Property { get; private set; } = default!;
        public OwnerSnapshot Owner { get; private set; } = default!;

        public ICollection<SensorAggregate> Sensors { get; private set; } = [];

        // Private constructor for factories and ORM
        private PlotAggregate(Guid id) : base(id) { }

        // Parameterless constructor for EF Core
        private PlotAggregate() { }

        #region Factories

        public static Result<PlotAggregate> Create(
            Guid propertyId,
            Guid ownerId,
            string name,
            string cropType,
            double areaHectares,
            DateTimeOffset plantingDate,
            DateTimeOffset expectedHarvestDate,
            string irrigationType,
            string? additionalNotes)
        {
            var nameResult = ValueObjects.Name.Create(name);
            var cropTypeResult = ValueObjects.CropType.Create(cropType);
            var areaResult = Area.Create(areaHectares);
            var irrigationTypeResult = ValueObjects.IrrigationType.Create(irrigationType);
            var additionalNotesResult = ValueObjects.AdditionalNotes.Create(additionalNotes);

            var errors = new List<ValidationError>();
            errors.AddRange(ValidatePropertyId(propertyId));
            errors.AddRange(ValidateOwnerId(ownerId));
            errors.AddErrorsIfFailure(nameResult);
            errors.AddErrorsIfFailure(cropTypeResult);
            errors.AddErrorsIfFailure(areaResult);
            errors.AddErrorsIfFailure(irrigationTypeResult);
            if (!additionalNotesResult.IsSuccess)
                errors.AddRange(additionalNotesResult.ValidationErrors);
            errors.AddRange(ValidateDates(plantingDate, expectedHarvestDate));

            if (errors.Count > 0)
            {
                return Result.Invalid(errors.ToArray());
            }

            return CreateAggregate(
                propertyId,
                ownerId,
                nameResult.Value,
                cropTypeResult.Value,
                areaResult.Value,
                plantingDate,
                expectedHarvestDate,
                irrigationTypeResult.Value,
                additionalNotesResult.Value);
        }

        private static Result<PlotAggregate> CreateAggregate(
            Guid propertyId,
            Guid ownerId,
            Name name,
            CropType cropType,
            Area area,
            DateTimeOffset plantingDate,
            DateTimeOffset expectedHarvestDate,
            IrrigationType irrigationType,
            AdditionalNotes? additionalNotes)
        {
            var aggregate = new PlotAggregate(Guid.NewGuid());
            var @event = new PlotCreatedDomainEvent(
                aggregate.Id,
                propertyId,
                ownerId,
                name.Value,
                cropType.Value,
                area.Hectares,
                plantingDate,
                expectedHarvestDate,
                irrigationType.Value,
                additionalNotes?.Value,
                DateTimeOffset.UtcNow);

            aggregate.ApplyEvent(@event);
            return Result.Success(aggregate);
        }

        #endregion

        #region Commands

        public Result Update(
            string name,
            string cropType,
            double areaHectares,
            DateTimeOffset plantingDate,
            DateTimeOffset expectedHarvestDate,
            string irrigationType,
            string? additionalNotes)
        {
            var nameResult = ValueObjects.Name.Create(name);
            var cropTypeResult = ValueObjects.CropType.Create(cropType);
            var areaResult = Area.Create(areaHectares);
            var irrigationTypeResult = ValueObjects.IrrigationType.Create(irrigationType);
            var additionalNotesResult = ValueObjects.AdditionalNotes.Create(additionalNotes);

            var errors = new List<ValidationError>();
            errors.AddErrorsIfFailure(nameResult);
            errors.AddErrorsIfFailure(cropTypeResult);
            errors.AddErrorsIfFailure(areaResult);
            errors.AddErrorsIfFailure(irrigationTypeResult);
            if (!additionalNotesResult.IsSuccess)
                errors.AddRange(additionalNotesResult.ValidationErrors);
            errors.AddRange(ValidateDates(plantingDate, expectedHarvestDate));

            if (errors.Count > 0)
            {
                return Result.Invalid(errors.ToArray());
            }

            var @event = new PlotUpdatedDomainEvent(
                Id,
                nameResult.Value.Value,
                cropTypeResult.Value.Value,
                areaResult.Value.Hectares,
                plantingDate,
                expectedHarvestDate,
                irrigationTypeResult.Value.Value,
                additionalNotesResult.Value?.Value,
                DateTimeOffset.UtcNow);

            ApplyEvent(@event);
            return Result.Success();
        }

        public Result ChangeCropType(string cropType)
        {
            var cropTypeResult = CropType.Create(cropType);

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
            OwnerId = @event.OwnerId;
            Name = ValueObjects.Name.FromDb(@event.Name).Value;
            CropType = ValueObjects.CropType.FromDb(@event.CropType).Value;
            AreaHectares = Area.FromDb(@event.AreaHectares).Value;
            PlantingDate = @event.PlantingDate;
            ExpectedHarvestDate = @event.ExpectedHarvestDate;
            IrrigationType = ValueObjects.IrrigationType.FromDb(@event.IrrigationType).Value;
            AdditionalNotes = ValueObjects.AdditionalNotes.FromDb(@event.AdditionalNotes).Value;
            SetCreatedAt(@event.OccurredOn);
            SetActivate();
        }

        public void Apply(PlotUpdatedDomainEvent @event)
        {
            Name = ValueObjects.Name.FromDb(@event.Name).Value;
            CropType = ValueObjects.CropType.FromDb(@event.CropType).Value;
            AreaHectares = Area.FromDb(@event.AreaHectares).Value;
            PlantingDate = @event.PlantingDate;
            ExpectedHarvestDate = @event.ExpectedHarvestDate;
            IrrigationType = ValueObjects.IrrigationType.FromDb(@event.IrrigationType).Value;
            AdditionalNotes = ValueObjects.AdditionalNotes.FromDb(@event.AdditionalNotes).Value;
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

        private static IEnumerable<ValidationError> ValidateOwnerId(Guid ownerId)
        {
            if (ownerId == Guid.Empty)
            {
                yield return FarmDomainErrors.OwnerIdRequired;
            }
        }

        private static IEnumerable<ValidationError> ValidateDates(DateTimeOffset plantingDate, DateTimeOffset expectedHarvestDate)
        {
            if (plantingDate == default)
                yield return FarmDomainErrors.PlantingDateRequired;

            if (expectedHarvestDate == default)
                yield return FarmDomainErrors.ExpectedHarvestRequired;

            if (plantingDate > DateTimeOffset.UtcNow)
                yield return FarmDomainErrors.PlantingDateFuture;

            if (expectedHarvestDate <= plantingDate)
                yield return FarmDomainErrors.ExpectedHarvestBeforePlanting;
        }

        #endregion

        #region Domain Events

        public record PlotCreatedDomainEvent(
            Guid AggregateId,
            Guid PropertyId,
            Guid OwnerId,
            string Name,
            string CropType,
            double AreaHectares,
            DateTimeOffset PlantingDate,
            DateTimeOffset ExpectedHarvestDate,
            string IrrigationType,
            string? AdditionalNotes,
            DateTimeOffset OccurredOn) : BaseDomainEvent(AggregateId, OccurredOn);

        public record PlotUpdatedDomainEvent(
            Guid AggregateId,
            string Name,
            string CropType,
            double AreaHectares,
            DateTimeOffset PlantingDate,
            DateTimeOffset ExpectedHarvestDate,
            string IrrigationType,
            string? AdditionalNotes,
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
