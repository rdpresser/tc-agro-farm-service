using TC.Agro.Farm.Domain.Abstractions;
using TC.Agro.Farm.Domain.Snapshots;

namespace TC.Agro.Farm.Domain.Aggregates
{
    /// <summary>
    /// Crop type suggestion aggregate root.
    /// Stores AI-generated or manually curated crop suggestions for a property.
    /// </summary>
    public sealed class CropTypeSuggestionAggregate : BaseAggregateRoot, ITenantAware
    {
        private const int MaxPlantingWindowLength = 200;
        private const int MaxIrrigationTypeLength = 100;
        private const int MaxNotesLength = 500;
        private const int MaxModelLength = 100;
        private const int MaxHarvestCycleMonths = 36;

        public const string AiSource = "AI";
        public const string ManualSource = "Manual";

        public Guid PropertyId { get; private set; }
        public Guid OwnerId { get; private set; }
        public CropType CropName { get; private set; } = default!;

        public string Source { get; private set; } = ManualSource;
        public bool IsOverride { get; private set; }
        public bool IsStale { get; private set; }
        public double? ConfidenceScore { get; private set; }
        public string? PlantingWindow { get; private set; }
        public int? HarvestCycleMonths { get; private set; }
        public string? SuggestedIrrigationType { get; private set; }
        public double? MinSoilMoisture { get; private set; }
        public double? MaxTemperature { get; private set; }
        public double? MinHumidity { get; private set; }
        public string? Notes { get; private set; }
        public string? Model { get; private set; }
        public DateTimeOffset? GeneratedAt { get; private set; }

        public PropertyAggregate Property { get; private set; } = default!;
        public OwnerSnapshot Owner { get; private set; } = default!;

        private CropTypeSuggestionAggregate(Guid id) : base(id) { }
        private CropTypeSuggestionAggregate() { }

        #region Factories

        public static Result<CropTypeSuggestionAggregate> CreateManual(
            Guid propertyId,
            Guid ownerId,
            string cropType,
            string? plantingWindow,
            int? harvestCycleMonths,
            string? suggestedIrrigationType,
            double? minSoilMoisture,
            double? maxTemperature,
            double? minHumidity,
            string? notes)
        {
            return CreateInternal(
                propertyId,
                ownerId,
                cropType,
                source: ManualSource,
                isOverride: true,
                confidenceScore: null,
                plantingWindow: plantingWindow,
                harvestCycleMonths: harvestCycleMonths,
                suggestedIrrigationType: suggestedIrrigationType,
                minSoilMoisture: minSoilMoisture,
                maxTemperature: maxTemperature,
                minHumidity: minHumidity,
                notes: notes,
                model: null,
                generatedAt: null);
        }

        public static Result<CropTypeSuggestionAggregate> CreateAi(
            Guid propertyId,
            Guid ownerId,
            string cropType,
            double? confidenceScore,
            string? plantingWindow,
            int? harvestCycleMonths,
            string? suggestedIrrigationType,
            double? minSoilMoisture,
            double? maxTemperature,
            double? minHumidity,
            string? notes,
            string model,
            DateTimeOffset generatedAt)
        {
            return CreateInternal(
                propertyId,
                ownerId,
                cropType,
                source: AiSource,
                isOverride: false,
                confidenceScore: confidenceScore,
                plantingWindow: plantingWindow,
                harvestCycleMonths: harvestCycleMonths,
                suggestedIrrigationType: suggestedIrrigationType,
                minSoilMoisture: minSoilMoisture,
                maxTemperature: maxTemperature,
                minHumidity: minHumidity,
                notes: notes,
                model: model,
                generatedAt: generatedAt);
        }

        private static Result<CropTypeSuggestionAggregate> CreateInternal(
            Guid propertyId,
            Guid ownerId,
            string cropType,
            string source,
            bool isOverride,
            double? confidenceScore,
            string? plantingWindow,
            int? harvestCycleMonths,
            string? suggestedIrrigationType,
            double? minSoilMoisture,
            double? maxTemperature,
            double? minHumidity,
            string? notes,
            string? model,
            DateTimeOffset? generatedAt)
        {
            var cropTypeResult = CropType.Create(cropType);

            var errors = new List<ValidationError>();
            errors.AddErrorsIfFailure(cropTypeResult);
            errors.AddRange(ValidatePropertyId(propertyId));
            errors.AddRange(ValidateOwnerId(ownerId));
            errors.AddRange(ValidateSource(source));
            errors.AddRange(ValidateOptionalRanges(confidenceScore, minSoilMoisture, maxTemperature, minHumidity));
            errors.AddRange(ValidateOptionalLengths(plantingWindow, suggestedIrrigationType, notes, model));
            errors.AddRange(ValidateHarvestCycleMonths(harvestCycleMonths));

            if (errors.Count > 0)
            {
                return Result.Invalid(errors.ToArray());
            }

            var aggregate = new CropTypeSuggestionAggregate(Guid.NewGuid());
            var @event = new CropTypeSuggestionCreatedDomainEvent(
                aggregate.Id,
                propertyId,
                ownerId,
                cropTypeResult.Value.Value,
                source,
                isOverride,
                confidenceScore,
                plantingWindow,
                harvestCycleMonths,
                suggestedIrrigationType,
                minSoilMoisture,
                maxTemperature,
                minHumidity,
                notes,
                model,
                generatedAt,
                DateTimeOffset.UtcNow);

            aggregate.ApplyEvent(@event);
            return Result.Success(aggregate);
        }

        #endregion

        #region Commands

        public Result UpdateManual(
            string cropType,
            string? plantingWindow,
            int? harvestCycleMonths,
            string? suggestedIrrigationType,
            double? minSoilMoisture,
            double? maxTemperature,
            double? minHumidity,
            string? notes)
        {
            var cropTypeResult = CropType.Create(cropType);

            var errors = new List<ValidationError>();
            errors.AddErrorsIfFailure(cropTypeResult);
            errors.AddRange(ValidateOptionalRanges(null, minSoilMoisture, maxTemperature, minHumidity));
            errors.AddRange(ValidateOptionalLengths(plantingWindow, suggestedIrrigationType, notes, null));
            errors.AddRange(ValidateHarvestCycleMonths(harvestCycleMonths));

            if (errors.Count > 0)
            {
                return Result.Invalid(errors.ToArray());
            }

            var @event = new CropTypeSuggestionUpdatedDomainEvent(
                Id,
                cropTypeResult.Value.Value,
                plantingWindow,
                harvestCycleMonths,
                suggestedIrrigationType,
                minSoilMoisture,
                maxTemperature,
                minHumidity,
                notes,
                DateTimeOffset.UtcNow);

            ApplyEvent(@event);
            return Result.Success();
        }

        public Result MarkAsStale()
        {
            if (IsStale)
            {
                return Result.Success();
            }

            var @event = new CropTypeSuggestionMarkedStaleDomainEvent(Id, DateTimeOffset.UtcNow);
            ApplyEvent(@event);
            return Result.Success();
        }

        public Result Deactivate()
        {
            if (!IsActive)
            {
                return Result.Invalid(FarmDomainErrors.CropTypeSuggestionAlreadyDeactivated);
            }

            var @event = new CropTypeSuggestionDeactivatedDomainEvent(Id, DateTimeOffset.UtcNow);
            ApplyEvent(@event);
            return Result.Success();
        }

        #endregion

        #region Event Handlers

        public void Apply(CropTypeSuggestionCreatedDomainEvent @event)
        {
            SetId(@event.AggregateId);
            PropertyId = @event.PropertyId;
            OwnerId = @event.OwnerId;
            CropName = CropType.FromDb(@event.CropType).Value;
            Source = @event.Source;
            IsOverride = @event.IsOverride;
            IsStale = false;
            ConfidenceScore = @event.ConfidenceScore;
            PlantingWindow = @event.PlantingWindow;
            HarvestCycleMonths = @event.HarvestCycleMonths;
            SuggestedIrrigationType = @event.SuggestedIrrigationType;
            MinSoilMoisture = @event.MinSoilMoisture;
            MaxTemperature = @event.MaxTemperature;
            MinHumidity = @event.MinHumidity;
            Notes = @event.Notes;
            Model = @event.Model;
            GeneratedAt = @event.GeneratedAt;
            SetCreatedAt(@event.OccurredOn);
            SetActivate();
        }

        public void Apply(CropTypeSuggestionUpdatedDomainEvent @event)
        {
            CropName = CropType.FromDb(@event.CropType).Value;
            Source = ManualSource;
            IsOverride = true;
            IsStale = false;
            ConfidenceScore = null;
            PlantingWindow = @event.PlantingWindow;
            HarvestCycleMonths = @event.HarvestCycleMonths;
            SuggestedIrrigationType = @event.SuggestedIrrigationType;
            MinSoilMoisture = @event.MinSoilMoisture;
            MaxTemperature = @event.MaxTemperature;
            MinHumidity = @event.MinHumidity;
            Notes = @event.Notes;
            Model = null;
            SetUpdatedAt(@event.OccurredOn);
        }

        public void Apply(CropTypeSuggestionMarkedStaleDomainEvent @event)
        {
            IsStale = true;
            SetUpdatedAt(@event.OccurredOn);
        }

        public void Apply(CropTypeSuggestionDeactivatedDomainEvent @event)
        {
            SetDeactivate();
            SetUpdatedAt(@event.OccurredOn);
        }

        private void ApplyEvent(BaseDomainEvent @event)
        {
            AddNewEvent(@event);
            switch (@event)
            {
                case CropTypeSuggestionCreatedDomainEvent createdEvent:
                    Apply(createdEvent);
                    break;
                case CropTypeSuggestionUpdatedDomainEvent updatedEvent:
                    Apply(updatedEvent);
                    break;
                case CropTypeSuggestionMarkedStaleDomainEvent staleEvent:
                    Apply(staleEvent);
                    break;
                case CropTypeSuggestionDeactivatedDomainEvent deactivatedEvent:
                    Apply(deactivatedEvent);
                    break;
            }
        }

        #endregion

        #region Validation Helpers

        private static IEnumerable<ValidationError> ValidatePropertyId(Guid propertyId)
        {
            if (propertyId == Guid.Empty)
            {
                yield return FarmDomainErrors.CropTypeSuggestionPropertyIdRequired;
            }
        }

        private static IEnumerable<ValidationError> ValidateOwnerId(Guid ownerId)
        {
            if (ownerId == Guid.Empty)
            {
                yield return FarmDomainErrors.OwnerIdRequired;
            }
        }

        private static IEnumerable<ValidationError> ValidateSource(string source)
        {
            if (!string.Equals(source, AiSource, StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(source, ManualSource, StringComparison.OrdinalIgnoreCase))
            {
                yield return new ValidationError("CropTypeSuggestion.Source", "Source must be AI or Manual.");
            }
        }

        private static IEnumerable<ValidationError> ValidateOptionalRanges(
            double? confidenceScore,
            double? minSoilMoisture,
            double? maxTemperature,
            double? minHumidity)
        {
            if (confidenceScore.HasValue && (confidenceScore.Value < 0 || confidenceScore.Value > 100))
            {
                yield return new ValidationError("CropTypeSuggestion.ConfidenceScore", "Confidence score must be between 0 and 100.");
            }

            if (minSoilMoisture.HasValue && (minSoilMoisture.Value < 0 || minSoilMoisture.Value > 100))
            {
                yield return new ValidationError("CropTypeSuggestion.MinSoilMoisture", "Minimum soil moisture must be between 0 and 100.");
            }

            if (maxTemperature.HasValue && (maxTemperature.Value < -30 || maxTemperature.Value > 80))
            {
                yield return new ValidationError("CropTypeSuggestion.MaxTemperature", "Maximum temperature must be between -30 and 80.");
            }

            if (minHumidity.HasValue && (minHumidity.Value < 0 || minHumidity.Value > 100))
            {
                yield return new ValidationError("CropTypeSuggestion.MinHumidity", "Minimum humidity must be between 0 and 100.");
            }
        }

        private static IEnumerable<ValidationError> ValidateOptionalLengths(
            string? plantingWindow,
            string? suggestedIrrigationType,
            string? notes,
            string? model)
        {
            if (!string.IsNullOrWhiteSpace(plantingWindow) && plantingWindow.Trim().Length > MaxPlantingWindowLength)
            {
                yield return new ValidationError("CropTypeSuggestion.PlantingWindow", $"Planting window cannot exceed {MaxPlantingWindowLength} characters.");
            }

            if (!string.IsNullOrWhiteSpace(suggestedIrrigationType) && suggestedIrrigationType.Trim().Length > MaxIrrigationTypeLength)
            {
                yield return new ValidationError("CropTypeSuggestion.SuggestedIrrigationType", $"Suggested irrigation type cannot exceed {MaxIrrigationTypeLength} characters.");
            }

            if (!string.IsNullOrWhiteSpace(notes) && notes.Trim().Length > MaxNotesLength)
            {
                yield return new ValidationError("CropTypeSuggestion.Notes", $"Notes cannot exceed {MaxNotesLength} characters.");
            }

            if (!string.IsNullOrWhiteSpace(model) && model.Trim().Length > MaxModelLength)
            {
                yield return new ValidationError("CropTypeSuggestion.Model", $"Model name cannot exceed {MaxModelLength} characters.");
            }
        }

        private static IEnumerable<ValidationError> ValidateHarvestCycleMonths(int? harvestCycleMonths)
        {
            if (!harvestCycleMonths.HasValue)
            {
                yield break;
            }

            if (harvestCycleMonths.Value < 1 || harvestCycleMonths.Value > MaxHarvestCycleMonths)
            {
                yield return new ValidationError("CropTypeSuggestion.HarvestCycleMonths", $"Harvest cycle must be between 1 and {MaxHarvestCycleMonths} months.");
            }
        }

        #endregion

        #region Domain Events

        public record CropTypeSuggestionCreatedDomainEvent(
            Guid AggregateId,
            Guid PropertyId,
            Guid OwnerId,
            string CropType,
            string Source,
            bool IsOverride,
            double? ConfidenceScore,
            string? PlantingWindow,
            int? HarvestCycleMonths,
            string? SuggestedIrrigationType,
            double? MinSoilMoisture,
            double? MaxTemperature,
            double? MinHumidity,
            string? Notes,
            string? Model,
            DateTimeOffset? GeneratedAt,
            DateTimeOffset OccurredOn) : BaseDomainEvent(AggregateId, OccurredOn);

        public record CropTypeSuggestionUpdatedDomainEvent(
            Guid AggregateId,
            string CropType,
            string? PlantingWindow,
            int? HarvestCycleMonths,
            string? SuggestedIrrigationType,
            double? MinSoilMoisture,
            double? MaxTemperature,
            double? MinHumidity,
            string? Notes,
            DateTimeOffset OccurredOn) : BaseDomainEvent(AggregateId, OccurredOn);

        public record CropTypeSuggestionMarkedStaleDomainEvent(
            Guid AggregateId,
            DateTimeOffset OccurredOn) : BaseDomainEvent(AggregateId, OccurredOn);

        public record CropTypeSuggestionDeactivatedDomainEvent(
            Guid AggregateId,
            DateTimeOffset OccurredOn) : BaseDomainEvent(AggregateId, OccurredOn);

        #endregion
    }
}
