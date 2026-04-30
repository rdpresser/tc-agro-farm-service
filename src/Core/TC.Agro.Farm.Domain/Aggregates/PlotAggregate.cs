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
        public Area AreaHectares { get; private set; } = default!;
        public Guid CropTypeCatalogId { get; private set; }
        public Guid? SelectedCropTypeSuggestionId { get; private set; }

        public double? Latitude { get; private set; } = default!;
        public double? Longitude { get; private set; } = default!;
        public string? BoundaryGeoJson { get; private set; } = default!;

        public DateTimeOffset PlantingDate { get; private set; }
        public DateTimeOffset ExpectedHarvestDate { get; private set; }
        public IrrigationType IrrigationType { get; private set; } = default!;
        public AdditionalNotes? AdditionalNotes { get; private set; }

        public Guid PropertyId { get; private set; }
        public Guid OwnerId { get; private set; }
        public PropertyAggregate Property { get; private set; } = default!;
        public OwnerSnapshot Owner { get; private set; } = default!;
        public CropTypeCatalogAggregate? CropTypeCatalog { get; private set; }
        public CropTypeSuggestionAggregate? SelectedCropTypeSuggestion { get; private set; }

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
            string? additionalNotes,
            double? latitude = null,
            double? longitude = null,
            string? boundaryGeoJson = null,
            Guid cropTypeCatalogId = default,
            Guid? selectedCropTypeSuggestionId = null)
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
            errors.AddRange(ValidateCoordinates(latitude, longitude));
            errors.AddRange(ValidateCropReferences(cropTypeCatalogId, selectedCropTypeSuggestionId));

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
                additionalNotesResult.Value,
                latitude,
                longitude,
                boundaryGeoJson,
                cropTypeCatalogId,
                selectedCropTypeSuggestionId);
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
            AdditionalNotes? additionalNotes,
            double? latitude = null,
            double? longitude = null,
            string? boundaryGeoJson = null,
            Guid cropTypeCatalogId = default,
            Guid? selectedCropTypeSuggestionId = null)
        {
            var aggregate = new PlotAggregate(Guid.NewGuid());
            var @event = new PlotCreatedDomainEvent(
                aggregate.Id,
                propertyId,
                ownerId,
                name.Value,
                cropType.Value,
                area.Hectares,
                latitude,
                longitude,
                boundaryGeoJson,
                plantingDate,
                expectedHarvestDate,
                irrigationType.Value,
                additionalNotes?.Value,
                DateTimeOffset.UtcNow,
                cropTypeCatalogId,
                selectedCropTypeSuggestionId);

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
            string? additionalNotes,
            double? latitude = null,
            double? longitude = null,
            string? boundaryGeoJson = null,
            Guid cropTypeCatalogId = default,
            Guid? selectedCropTypeSuggestionId = null)
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
            errors.AddRange(ValidateCoordinates(latitude, longitude));
            errors.AddRange(ValidateCropReferences(cropTypeCatalogId, selectedCropTypeSuggestionId));

            if (errors.Count > 0)
            {
                return Result.Invalid(errors.ToArray());
            }

            var @event = new PlotUpdatedDomainEvent(
                Id,
                nameResult.Value.Value,
                cropTypeResult.Value.Value,
                areaResult.Value.Hectares,
                latitude,
                longitude,
                boundaryGeoJson,
                plantingDate,
                expectedHarvestDate,
                irrigationTypeResult.Value.Value,
                additionalNotesResult.Value?.Value,
                DateTimeOffset.UtcNow,
                cropTypeCatalogId,
                selectedCropTypeSuggestionId);

            ApplyEvent(@event);
            return Result.Success();
        }

        public Result ChangeCropType(
            string cropType,
            Guid cropTypeCatalogId = default,
            Guid? selectedCropTypeSuggestionId = null)
        {
            var cropTypeResult = CropType.Create(cropType);

            if (!cropTypeResult.IsSuccess)
            {
                return Result.Invalid(cropTypeResult.ValidationErrors);
            }

            var referenceErrors = ValidateCropReferences(cropTypeCatalogId, selectedCropTypeSuggestionId).ToArray();
            if (referenceErrors.Length > 0)
            {
                return Result.Invalid(referenceErrors);
            }

            var @event = new PlotCropTypeChangedDomainEvent(
                Id,
                cropTypeResult.Value.Value,
                DateTimeOffset.UtcNow,
                cropTypeCatalogId,
                selectedCropTypeSuggestionId);

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
            AreaHectares = Area.FromDb(@event.AreaHectares).Value;
            Latitude = @event.Latitude;
            Longitude = @event.Longitude;
            BoundaryGeoJson = @event.BoundaryGeoJson;
            PlantingDate = @event.PlantingDate;
            ExpectedHarvestDate = @event.ExpectedHarvestDate;
            IrrigationType = ValueObjects.IrrigationType.FromDb(@event.IrrigationType).Value;
            AdditionalNotes = ValueObjects.AdditionalNotes.FromDb(@event.AdditionalNotes).Value;
            CropTypeCatalogId = @event.CropTypeCatalogId;
            SelectedCropTypeSuggestionId = @event.SelectedCropTypeSuggestionId;
            CropTypeCatalog = null;
            SelectedCropTypeSuggestion = null;
            SetCreatedAt(@event.OccurredOn);
            SetActivate();
        }

        public void Apply(PlotUpdatedDomainEvent @event)
        {
            Name = ValueObjects.Name.FromDb(@event.Name).Value;
            AreaHectares = Area.FromDb(@event.AreaHectares).Value;
            Latitude = @event.Latitude;
            Longitude = @event.Longitude;
            BoundaryGeoJson = @event.BoundaryGeoJson;
            PlantingDate = @event.PlantingDate;
            ExpectedHarvestDate = @event.ExpectedHarvestDate;
            IrrigationType = ValueObjects.IrrigationType.FromDb(@event.IrrigationType).Value;
            AdditionalNotes = ValueObjects.AdditionalNotes.FromDb(@event.AdditionalNotes).Value;
            CropTypeCatalogId = @event.CropTypeCatalogId;
            SelectedCropTypeSuggestionId = @event.SelectedCropTypeSuggestionId;
            CropTypeCatalog = null;
            SelectedCropTypeSuggestion = null;
            SetUpdatedAt(@event.OccurredOn);
        }

        public void Apply(PlotCropTypeChangedDomainEvent @event)
        {
            CropTypeCatalogId = @event.CropTypeCatalogId;
            SelectedCropTypeSuggestionId = @event.SelectedCropTypeSuggestionId;
            CropTypeCatalog = null;
            SelectedCropTypeSuggestion = null;
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

            if (expectedHarvestDate <= plantingDate)
                yield return FarmDomainErrors.ExpectedHarvestBeforePlanting;
        }

        private static IEnumerable<ValidationError> ValidateCoordinates(double? latitude, double? longitude)
        {
            if (latitude.HasValue && (latitude.Value < -90 || latitude.Value > 90))
            {
                yield return new ValidationError(nameof(Latitude), "Latitude must be between -90 and 90.");
            }

            if (longitude.HasValue && (longitude.Value < -180 || longitude.Value > 180))
            {
                yield return new ValidationError(nameof(Longitude), "Longitude must be between -180 and 180.");
            }

            if (latitude.HasValue != longitude.HasValue)
            {
                yield return new ValidationError("GeoCoordinates", "Latitude and Longitude must be informed together.");
            }
        }

        private static IEnumerable<ValidationError> ValidateCropReferences(Guid cropTypeCatalogId, Guid? selectedCropTypeSuggestionId)
        {
            if (cropTypeCatalogId == Guid.Empty)
            {
                yield return new ValidationError("Plot.CropTypeCatalogId", "CropTypeCatalogId is required.");
            }

            if (selectedCropTypeSuggestionId.HasValue && selectedCropTypeSuggestionId.Value == Guid.Empty)
            {
                yield return new ValidationError("Plot.SelectedCropTypeSuggestionId", "SelectedCropTypeSuggestionId cannot be empty when provided.");
            }

            if (selectedCropTypeSuggestionId.HasValue && cropTypeCatalogId == Guid.Empty)
            {
                yield return new ValidationError(
                    "Plot.CropTypeCatalogId",
                    "CropTypeCatalogId is required when SelectedCropTypeSuggestionId is informed.");
            }
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
            double? Latitude,
            double? Longitude,
            string? BoundaryGeoJson,
            DateTimeOffset PlantingDate,
            DateTimeOffset ExpectedHarvestDate,
            string IrrigationType,
            string? AdditionalNotes,
            DateTimeOffset OccurredOn,
            Guid CropTypeCatalogId,
            Guid? SelectedCropTypeSuggestionId = null) : BaseDomainEvent(AggregateId, OccurredOn);

        public record PlotUpdatedDomainEvent(
            Guid AggregateId,
            string Name,
            string CropType,
            double AreaHectares,
            double? Latitude,
            double? Longitude,
            string? BoundaryGeoJson,
            DateTimeOffset PlantingDate,
            DateTimeOffset ExpectedHarvestDate,
            string IrrigationType,
            string? AdditionalNotes,
            DateTimeOffset OccurredOn,
            Guid CropTypeCatalogId,
            Guid? SelectedCropTypeSuggestionId = null) : BaseDomainEvent(AggregateId, OccurredOn);

        public record PlotCropTypeChangedDomainEvent(
            Guid AggregateId,
            string CropType,
            DateTimeOffset OccurredOn,
            Guid CropTypeCatalogId,
            Guid? SelectedCropTypeSuggestionId = null) : BaseDomainEvent(AggregateId, OccurredOn);

        public record PlotDeactivatedDomainEvent(
            Guid AggregateId,
            DateTimeOffset OccurredOn) : BaseDomainEvent(AggregateId, OccurredOn);

        public record PlotActivatedDomainEvent(
            Guid AggregateId,
            DateTimeOffset OccurredOn) : BaseDomainEvent(AggregateId, OccurredOn);

        #endregion
    }
}
