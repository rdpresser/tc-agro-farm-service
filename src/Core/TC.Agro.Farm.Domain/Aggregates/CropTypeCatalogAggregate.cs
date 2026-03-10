namespace TC.Agro.Farm.Domain.Aggregates
{
    /// <summary>
    /// Catalog aggregate root for crop types used by plots.
    /// </summary>
    public sealed class CropTypeCatalogAggregate : BaseAggregateRoot
    {
        private const int MaxDescriptionLength = 500;
        private const int MaxIrrigationTypeLength = 100;
        private const int MaxScientificNameLength = 150;
        private const int MaxHarvestCycleMonths = 120;
        private const int MinMonth = 1;
        private const int MaxMonth = 12;
        private const double MinAllowedTemperature = -30;
        private const double MaxAllowedTemperature = 80;
        private const double MinAllowedPercentage = 0;
        private const double MaxAllowedPercentage = 100;

        public CropType CropTypeName { get; private set; } = default!;
        public bool IsSystemDefined { get; private set; }
        public string? Description { get; private set; }
        public string? ScientificName { get; private set; }
        public int? TypicalPlantingStartMonth { get; private set; }
        public int? TypicalPlantingEndMonth { get; private set; }
        public string? RecommendedIrrigationType { get; private set; }
        public int? TypicalHarvestCycleMonths { get; private set; }
        public double? MinTemperature { get; private set; }
        public double? MaxTemperature { get; private set; }
        public double? MinHumidity { get; private set; }
        public double? MinSoilMoisture { get; private set; }
        public double? MaxSoilMoisture { get; private set; }

        public ICollection<PlotAggregate> Plots { get; private set; } = [];

        private CropTypeCatalogAggregate(Guid id) : base(id) { }
        private CropTypeCatalogAggregate() { }

        #region Factories

        public static Result<CropTypeCatalogAggregate> Create(
            string cropTypeName,
            bool isSystemDefined = true,
            string? description = null,
            string? recommendedIrrigationType = null,
            int? typicalHarvestCycleMonths = null,
            string? scientificName = null,
            int? typicalPlantingStartMonth = null,
            int? typicalPlantingEndMonth = null,
            double? minTemperature = null,
            double? maxTemperature = null,
            double? minHumidity = null,
            double? minSoilMoisture = null,
            double? maxSoilMoisture = null)
        {
            var cropTypeResult = CropType.Create(cropTypeName);

            var errors = new List<ValidationError>();
            errors.AddErrorsIfFailure(cropTypeResult);
            errors.AddRange(ValidateOptionalText(description, recommendedIrrigationType, scientificName));
            errors.AddRange(ValidateHarvestCycle(typicalHarvestCycleMonths));
            errors.AddRange(ValidatePlantingWindow(typicalPlantingStartMonth, typicalPlantingEndMonth));
            errors.AddRange(ValidateClimateProfile(
                minTemperature,
                maxTemperature,
                minHumidity,
                minSoilMoisture,
                maxSoilMoisture));

            if (errors.Count > 0)
            {
                return Result.Invalid(errors.ToArray());
            }

            var aggregate = new CropTypeCatalogAggregate(Guid.NewGuid());
            var @event = new CropTypeCatalogCreatedDomainEvent(
                aggregate.Id,
                cropTypeResult.Value.Value,
                isSystemDefined,
                description?.Trim(),
                scientificName?.Trim(),
                typicalPlantingStartMonth,
                typicalPlantingEndMonth,
                recommendedIrrigationType?.Trim(),
                typicalHarvestCycleMonths,
                minTemperature,
                maxTemperature,
                minHumidity,
                minSoilMoisture,
                maxSoilMoisture,
                DateTimeOffset.UtcNow);

            aggregate.ApplyEvent(@event);
            return Result.Success(aggregate);
        }

        #endregion

        #region Commands

        public Result UpdateMetadata(
            string? description,
            string? recommendedIrrigationType,
            int? typicalHarvestCycleMonths,
            string? scientificName = null,
            int? typicalPlantingStartMonth = null,
            int? typicalPlantingEndMonth = null,
            double? minTemperature = null,
            double? maxTemperature = null,
            double? minHumidity = null,
            double? minSoilMoisture = null,
            double? maxSoilMoisture = null)
        {
            var errors = new List<ValidationError>();
            errors.AddRange(ValidateOptionalText(description, recommendedIrrigationType, scientificName));
            errors.AddRange(ValidateHarvestCycle(typicalHarvestCycleMonths));
            errors.AddRange(ValidatePlantingWindow(typicalPlantingStartMonth, typicalPlantingEndMonth));
            errors.AddRange(ValidateClimateProfile(
                minTemperature,
                maxTemperature,
                minHumidity,
                minSoilMoisture,
                maxSoilMoisture));

            if (errors.Count > 0)
            {
                return Result.Invalid(errors.ToArray());
            }

            var @event = new CropTypeCatalogUpdatedDomainEvent(
                Id,
                description?.Trim(),
                scientificName?.Trim(),
                typicalPlantingStartMonth,
                typicalPlantingEndMonth,
                recommendedIrrigationType?.Trim(),
                typicalHarvestCycleMonths,
                minTemperature,
                maxTemperature,
                minHumidity,
                minSoilMoisture,
                maxSoilMoisture,
                DateTimeOffset.UtcNow);

            ApplyEvent(@event);
            return Result.Success();
        }

        public Result Deactivate()
        {
            if (!IsActive)
            {
                return Result.Invalid(FarmDomainErrors.CropTypeCatalogAlreadyDeactivated);
            }

            var @event = new CropTypeCatalogDeactivatedDomainEvent(Id, DateTimeOffset.UtcNow);
            ApplyEvent(@event);
            return Result.Success();
        }

        public Result Activate()
        {
            if (IsActive)
            {
                return Result.Invalid(FarmDomainErrors.CropTypeCatalogAlreadyActivated);
            }

            var @event = new CropTypeCatalogActivatedDomainEvent(Id, DateTimeOffset.UtcNow);
            ApplyEvent(@event);
            return Result.Success();
        }

        #endregion

        #region Event Handlers

        public void Apply(CropTypeCatalogCreatedDomainEvent @event)
        {
            SetId(@event.AggregateId);
            CropTypeName = CropType.FromDb(@event.CropTypeName).Value;
            IsSystemDefined = @event.IsSystemDefined;
            Description = @event.Description;
            ScientificName = @event.ScientificName;
            TypicalPlantingStartMonth = @event.TypicalPlantingStartMonth;
            TypicalPlantingEndMonth = @event.TypicalPlantingEndMonth;
            RecommendedIrrigationType = @event.RecommendedIrrigationType;
            TypicalHarvestCycleMonths = @event.TypicalHarvestCycleMonths;
            MinTemperature = @event.MinTemperature;
            MaxTemperature = @event.MaxTemperature;
            MinHumidity = @event.MinHumidity;
            MinSoilMoisture = @event.MinSoilMoisture;
            MaxSoilMoisture = @event.MaxSoilMoisture;
            SetCreatedAt(@event.OccurredOn);
            SetActivate();
        }

        public void Apply(CropTypeCatalogUpdatedDomainEvent @event)
        {
            Description = @event.Description;
            ScientificName = @event.ScientificName;
            TypicalPlantingStartMonth = @event.TypicalPlantingStartMonth;
            TypicalPlantingEndMonth = @event.TypicalPlantingEndMonth;
            RecommendedIrrigationType = @event.RecommendedIrrigationType;
            TypicalHarvestCycleMonths = @event.TypicalHarvestCycleMonths;
            MinTemperature = @event.MinTemperature;
            MaxTemperature = @event.MaxTemperature;
            MinHumidity = @event.MinHumidity;
            MinSoilMoisture = @event.MinSoilMoisture;
            MaxSoilMoisture = @event.MaxSoilMoisture;
            SetUpdatedAt(@event.OccurredOn);
        }

        public void Apply(CropTypeCatalogDeactivatedDomainEvent @event)
        {
            SetDeactivate();
            SetUpdatedAt(@event.OccurredOn);
        }

        public void Apply(CropTypeCatalogActivatedDomainEvent @event)
        {
            SetActivate();
            SetUpdatedAt(@event.OccurredOn);
        }

        private void ApplyEvent(BaseDomainEvent @event)
        {
            AddNewEvent(@event);
            switch (@event)
            {
                case CropTypeCatalogCreatedDomainEvent createdEvent:
                    Apply(createdEvent);
                    break;
                case CropTypeCatalogUpdatedDomainEvent updatedEvent:
                    Apply(updatedEvent);
                    break;
                case CropTypeCatalogDeactivatedDomainEvent deactivatedEvent:
                    Apply(deactivatedEvent);
                    break;
                case CropTypeCatalogActivatedDomainEvent activatedEvent:
                    Apply(activatedEvent);
                    break;
            }
        }

        #endregion

        #region Validation Helpers

        private static IEnumerable<ValidationError> ValidateOptionalText(
            string? description,
            string? recommendedIrrigationType,
            string? scientificName)
        {
            if (!string.IsNullOrWhiteSpace(description) && description.Trim().Length > MaxDescriptionLength)
            {
                yield return new ValidationError(
                    "CropTypeCatalog.Description",
                    $"Description cannot exceed {MaxDescriptionLength} characters.");
            }

            if (!string.IsNullOrWhiteSpace(recommendedIrrigationType) && recommendedIrrigationType.Trim().Length > MaxIrrigationTypeLength)
            {
                yield return new ValidationError(
                    "CropTypeCatalog.RecommendedIrrigationType",
                    $"Recommended irrigation type cannot exceed {MaxIrrigationTypeLength} characters.");
            }

            if (!string.IsNullOrWhiteSpace(scientificName) && scientificName.Trim().Length > MaxScientificNameLength)
            {
                yield return new ValidationError(
                    "CropTypeCatalog.ScientificName",
                    $"Scientific name cannot exceed {MaxScientificNameLength} characters.");
            }
        }

        private static IEnumerable<ValidationError> ValidateHarvestCycle(int? typicalHarvestCycleMonths)
        {
            if (!typicalHarvestCycleMonths.HasValue)
            {
                yield break;
            }

            if (typicalHarvestCycleMonths.Value < 1 || typicalHarvestCycleMonths.Value > MaxHarvestCycleMonths)
            {
                yield return new ValidationError(
                    "CropTypeCatalog.TypicalHarvestCycleMonths",
                    $"Typical harvest cycle must be between 1 and {MaxHarvestCycleMonths} months.");
            }
        }

        private static IEnumerable<ValidationError> ValidatePlantingWindow(
            int? typicalPlantingStartMonth,
            int? typicalPlantingEndMonth)
        {
            if (typicalPlantingStartMonth.HasValue &&
                (typicalPlantingStartMonth.Value < MinMonth || typicalPlantingStartMonth.Value > MaxMonth))
            {
                yield return new ValidationError(
                    "CropTypeCatalog.TypicalPlantingStartMonth",
                    "Typical planting start month must be between 1 and 12.");
            }

            if (typicalPlantingEndMonth.HasValue &&
                (typicalPlantingEndMonth.Value < MinMonth || typicalPlantingEndMonth.Value > MaxMonth))
            {
                yield return new ValidationError(
                    "CropTypeCatalog.TypicalPlantingEndMonth",
                    "Typical planting end month must be between 1 and 12.");
            }
        }

        private static IEnumerable<ValidationError> ValidateClimateProfile(
            double? minTemperature,
            double? maxTemperature,
            double? minHumidity,
            double? minSoilMoisture,
            double? maxSoilMoisture)
        {
            if (minTemperature.HasValue &&
                (minTemperature.Value < MinAllowedTemperature || minTemperature.Value > MaxAllowedTemperature))
            {
                yield return new ValidationError(
                    "CropTypeCatalog.MinTemperature",
                    "Minimum temperature must be between -30 and 80.");
            }

            if (maxTemperature.HasValue &&
                (maxTemperature.Value < MinAllowedTemperature || maxTemperature.Value > MaxAllowedTemperature))
            {
                yield return new ValidationError(
                    "CropTypeCatalog.MaxTemperature",
                    "Maximum temperature must be between -30 and 80.");
            }

            if (minTemperature.HasValue && maxTemperature.HasValue && minTemperature.Value > maxTemperature.Value)
            {
                yield return new ValidationError(
                    "CropTypeCatalog.TemperatureRange",
                    "Minimum temperature cannot be greater than maximum temperature.");
            }

            if (minHumidity.HasValue &&
                (minHumidity.Value < MinAllowedPercentage || minHumidity.Value > MaxAllowedPercentage))
            {
                yield return new ValidationError(
                    "CropTypeCatalog.MinHumidity",
                    "Minimum humidity must be between 0 and 100.");
            }

            if (minSoilMoisture.HasValue &&
                (minSoilMoisture.Value < MinAllowedPercentage || minSoilMoisture.Value > MaxAllowedPercentage))
            {
                yield return new ValidationError(
                    "CropTypeCatalog.MinSoilMoisture",
                    "Minimum soil moisture must be between 0 and 100.");
            }

            if (maxSoilMoisture.HasValue &&
                (maxSoilMoisture.Value < MinAllowedPercentage || maxSoilMoisture.Value > MaxAllowedPercentage))
            {
                yield return new ValidationError(
                    "CropTypeCatalog.MaxSoilMoisture",
                    "Maximum soil moisture must be between 0 and 100.");
            }

            if (minSoilMoisture.HasValue && maxSoilMoisture.HasValue && minSoilMoisture.Value > maxSoilMoisture.Value)
            {
                yield return new ValidationError(
                    "CropTypeCatalog.SoilMoistureRange",
                    "Minimum soil moisture cannot be greater than maximum soil moisture.");
            }
        }

        #endregion

        #region Domain Events

        public record CropTypeCatalogCreatedDomainEvent(
            Guid AggregateId,
            string CropTypeName,
            bool IsSystemDefined,
            string? Description,
            string? ScientificName,
            int? TypicalPlantingStartMonth,
            int? TypicalPlantingEndMonth,
            string? RecommendedIrrigationType,
            int? TypicalHarvestCycleMonths,
            double? MinTemperature,
            double? MaxTemperature,
            double? MinHumidity,
            double? MinSoilMoisture,
            double? MaxSoilMoisture,
            DateTimeOffset OccurredOn) : BaseDomainEvent(AggregateId, OccurredOn);

        public record CropTypeCatalogUpdatedDomainEvent(
            Guid AggregateId,
            string? Description,
            string? ScientificName,
            int? TypicalPlantingStartMonth,
            int? TypicalPlantingEndMonth,
            string? RecommendedIrrigationType,
            int? TypicalHarvestCycleMonths,
            double? MinTemperature,
            double? MaxTemperature,
            double? MinHumidity,
            double? MinSoilMoisture,
            double? MaxSoilMoisture,
            DateTimeOffset OccurredOn) : BaseDomainEvent(AggregateId, OccurredOn);

        public record CropTypeCatalogDeactivatedDomainEvent(
            Guid AggregateId,
            DateTimeOffset OccurredOn) : BaseDomainEvent(AggregateId, OccurredOn);

        public record CropTypeCatalogActivatedDomainEvent(
            Guid AggregateId,
            DateTimeOffset OccurredOn) : BaseDomainEvent(AggregateId, OccurredOn);

        #endregion
    }
}
