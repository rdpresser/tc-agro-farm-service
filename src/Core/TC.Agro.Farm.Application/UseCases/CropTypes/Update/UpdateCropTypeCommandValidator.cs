namespace TC.Agro.Farm.Application.UseCases.CropTypes.Update
{
    public sealed class UpdateCropTypeCommandValidator : Validator<UpdateCropTypeCommand>
    {
        public UpdateCropTypeCommandValidator()
        {
            RuleFor(x => x.CropTypeId)
                .NotEmpty()
                .WithMessage("CropTypeId is required.")
                .WithErrorCode($"{nameof(UpdateCropTypeCommand.CropTypeId)}.Required");

            RuleFor(x => x.CropType)
                .NotEmpty()
                .WithMessage("CropType is required.")
                .WithErrorCode($"{nameof(UpdateCropTypeCommand.CropType)}.Required")
                .MinimumLength(2)
                .WithMessage("CropType must be at least 2 characters long.")
                .WithErrorCode($"{nameof(UpdateCropTypeCommand.CropType)}.MinimumLength")
                .MaximumLength(100)
                .WithMessage("CropType must not exceed 100 characters.")
                .WithErrorCode($"{nameof(UpdateCropTypeCommand.CropType)}.MaximumLength");

            RuleFor(x => x.PlantingWindow)
                .MaximumLength(200)
                .When(x => !string.IsNullOrWhiteSpace(x.PlantingWindow))
                .WithMessage("PlantingWindow must not exceed 200 characters.")
                .WithErrorCode($"{nameof(UpdateCropTypeCommand.PlantingWindow)}.MaximumLength");

            RuleFor(x => x.HarvestCycleMonths)
                .InclusiveBetween(1, 36)
                .When(x => x.HarvestCycleMonths.HasValue)
                .WithMessage("HarvestCycleMonths must be between 1 and 36.")
                .WithErrorCode($"{nameof(UpdateCropTypeCommand.HarvestCycleMonths)}.OutOfRange");

            RuleFor(x => x.MinSoilMoisture)
                .InclusiveBetween(0, 100)
                .When(x => x.MinSoilMoisture.HasValue)
                .WithMessage("MinSoilMoisture must be between 0 and 100.")
                .WithErrorCode($"{nameof(UpdateCropTypeCommand.MinSoilMoisture)}.OutOfRange");

            RuleFor(x => x.MaxTemperature)
                .InclusiveBetween(-30, 80)
                .When(x => x.MaxTemperature.HasValue)
                .WithMessage("MaxTemperature must be between -30 and 80.")
                .WithErrorCode($"{nameof(UpdateCropTypeCommand.MaxTemperature)}.OutOfRange");

            RuleFor(x => x.MinHumidity)
                .InclusiveBetween(0, 100)
                .When(x => x.MinHumidity.HasValue)
                .WithMessage("MinHumidity must be between 0 and 100.")
                .WithErrorCode($"{nameof(UpdateCropTypeCommand.MinHumidity)}.OutOfRange");

            RuleFor(x => x.SuggestedIrrigationType)
                .MaximumLength(100)
                .When(x => !string.IsNullOrWhiteSpace(x.SuggestedIrrigationType))
                .WithMessage("SuggestedIrrigationType must not exceed 100 characters.")
                .WithErrorCode($"{nameof(UpdateCropTypeCommand.SuggestedIrrigationType)}.MaximumLength");

            RuleFor(x => x.Notes)
                .MaximumLength(500)
                .When(x => !string.IsNullOrWhiteSpace(x.Notes))
                .WithMessage("Notes must not exceed 500 characters.")
                .WithErrorCode($"{nameof(UpdateCropTypeCommand.Notes)}.MaximumLength");

            RuleFor(x => x.SuggestedImage)
                .MaximumLength(10)
                .When(x => !string.IsNullOrWhiteSpace(x.SuggestedImage))
                .WithMessage("SuggestedImage must not exceed 10 characters.")
                .WithErrorCode($"{nameof(UpdateCropTypeCommand.SuggestedImage)}.MaximumLength");
        }
    }
}
