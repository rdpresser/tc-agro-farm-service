namespace TC.Agro.Farm.Application.UseCases.CropCycles.Start
{
    /// <summary>
    /// Validates the StartCropCycleCommand.
    /// </summary>
    public sealed class StartCropCycleCommandValidator : Validator<StartCropCycleCommand>
    {
        public StartCropCycleCommandValidator()
        {
            RuleFor(x => x.PlotId)
                .NotEmpty()
                .WithMessage("Plot ID is required.")
                .WithErrorCode($"{nameof(StartCropCycleCommand.PlotId)}.Required");

            RuleFor(x => x.CropTypeCatalogId)
                .NotEmpty()
                .WithMessage("Crop type catalog ID is required.")
                .WithErrorCode($"{nameof(StartCropCycleCommand.CropTypeCatalogId)}.Required");

            RuleFor(x => x.StartedAt)
                .NotEmpty()
                .WithMessage("Started at date is required.")
                .WithErrorCode($"{nameof(StartCropCycleCommand.StartedAt)}.Required");

            RuleFor(x => x.Status)
                .NotEmpty()
                .WithMessage("Status is required.")
                .WithErrorCode($"{nameof(StartCropCycleCommand.Status)}.Required")
                .Must(status => CropCycleStatus.Create(status).IsSuccess)
                .WithMessage($"Status must be one of: Planned, Planted, Growing, Harvesting.")
                .WithErrorCode($"{nameof(StartCropCycleCommand.Status)}.Invalid");

            RuleFor(x => x.Notes)
                .MaximumLength(1000)
                .WithMessage("Notes must not exceed 1000 characters.")
                .WithErrorCode($"{nameof(StartCropCycleCommand.Notes)}.MaximumLength")
                .When(x => x.Notes is not null);

            RuleFor(x => x.ExpectedHarvestDate)
                .Must((cmd, harvestDate) => harvestDate == null || harvestDate > cmd.StartedAt)
                .WithMessage("Expected harvest date must be after the started at date.")
                .WithErrorCode($"{nameof(StartCropCycleCommand.ExpectedHarvestDate)}.MustBeAfterStartedAt")
                .When(x => x.ExpectedHarvestDate.HasValue);
        }
    }
}
