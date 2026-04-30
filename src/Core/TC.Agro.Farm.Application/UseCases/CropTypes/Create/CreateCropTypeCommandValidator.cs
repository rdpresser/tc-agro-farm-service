namespace TC.Agro.Farm.Application.UseCases.CropTypes.Create
{
    public sealed class CreateCropTypeCommandValidator : Validator<CreateCropTypeCommand>
    {
        public CreateCropTypeCommandValidator()
        {
            RuleFor(x => x.CropType)
                .NotEmpty()
                .WithMessage("CropType is required.")
                .WithErrorCode($"{nameof(CreateCropTypeCommand.CropType)}.Required")
                .MinimumLength(2)
                .WithMessage("CropType must be at least 2 characters long.")
                .WithErrorCode($"{nameof(CreateCropTypeCommand.CropType)}.MinimumLength")
                .MaximumLength(100)
                .WithMessage("CropType must not exceed 100 characters.")
                .WithErrorCode($"{nameof(CreateCropTypeCommand.CropType)}.MaximumLength");

            RuleFor(x => x.PlantingWindow)
                .MaximumLength(200)
                .When(x => !string.IsNullOrWhiteSpace(x.PlantingWindow))
                .WithMessage("PlantingWindow must not exceed 200 characters.")
                .WithErrorCode($"{nameof(CreateCropTypeCommand.PlantingWindow)}.MaximumLength");

            RuleFor(x => x.HarvestCycleMonths)
                .InclusiveBetween(1, 36)
                .When(x => x.HarvestCycleMonths.HasValue)
                .WithMessage("HarvestCycleMonths must be between 1 and 36.")
                .WithErrorCode($"{nameof(CreateCropTypeCommand.HarvestCycleMonths)}.OutOfRange");

            RuleFor(x => x.MinSoilMoisture)
                .InclusiveBetween(0, 100)
                .When(x => x.MinSoilMoisture.HasValue)
                .WithMessage("MinSoilMoisture must be between 0 and 100.")
                .WithErrorCode($"{nameof(CreateCropTypeCommand.MinSoilMoisture)}.OutOfRange");

            RuleFor(x => x.MaxTemperature)
                .InclusiveBetween(-30, 80)
                .When(x => x.MaxTemperature.HasValue)
                .WithMessage("MaxTemperature must be between -30 and 80.")
                .WithErrorCode($"{nameof(CreateCropTypeCommand.MaxTemperature)}.OutOfRange");

            RuleFor(x => x.MinHumidity)
                .InclusiveBetween(0, 100)
                .When(x => x.MinHumidity.HasValue)
                .WithMessage("MinHumidity must be between 0 and 100.")
                .WithErrorCode($"{nameof(CreateCropTypeCommand.MinHumidity)}.OutOfRange");

            RuleFor(x => x.SuggestedIrrigationType)
                .MaximumLength(100)
                .When(x => !string.IsNullOrWhiteSpace(x.SuggestedIrrigationType))
                .WithMessage("SuggestedIrrigationType must not exceed 100 characters.")
                .WithErrorCode($"{nameof(CreateCropTypeCommand.SuggestedIrrigationType)}.MaximumLength");

            RuleFor(x => x.Notes)
                .MaximumLength(500)
                .When(x => !string.IsNullOrWhiteSpace(x.Notes))
                .WithMessage("Notes must not exceed 500 characters.")
                .WithErrorCode($"{nameof(CreateCropTypeCommand.Notes)}.MaximumLength");

            RuleFor(x => x.SuggestedImage)
                .MaximumLength(10)
                .When(x => !string.IsNullOrWhiteSpace(x.SuggestedImage))
                .WithMessage("SuggestedImage must not exceed 10 characters.")
                .WithErrorCode($"{nameof(CreateCropTypeCommand.SuggestedImage)}.MaximumLength");
        }
    }
}
