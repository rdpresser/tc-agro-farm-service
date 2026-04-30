namespace TC.Agro.Farm.Application.UseCases.Plots.Submit
{
    public sealed class SubmitPlotCommandValidator : Validator<SubmitPlotCommand>
    {
        public SubmitPlotCommandValidator()
        {
            When(x => !x.IsUpdate, () =>
            {
                RuleFor(x => x.PropertyId)
                    .NotEmpty()
                    .WithMessage("PropertyId is required.")
                    .WithErrorCode($"{nameof(SubmitPlotCommand.PropertyId)}.Required");
            });

            RuleFor(x => x.Name)
                .NotEmpty()
                .WithMessage("Name is required.")
                .WithErrorCode($"{nameof(SubmitPlotCommand.Name)}.Required")
                .MinimumLength(2)
                .WithMessage("Name must have at least 2 characters.")
                .WithErrorCode($"{nameof(SubmitPlotCommand.Name)}.MinimumLength")
                .MaximumLength(100)
                .WithMessage("Name must have at most 100 characters.")
                .WithErrorCode($"{nameof(SubmitPlotCommand.Name)}.MaximumLength");

            RuleFor(x => x.CropType)
                .MaximumLength(100)
                .When(x => !string.IsNullOrWhiteSpace(x.CropType))
                .WithMessage("CropType must have at most 100 characters.")
                .WithErrorCode($"{nameof(SubmitPlotCommand.CropType)}.MaximumLength");

            RuleFor(x => x.AreaHectares)
                .GreaterThan(0)
                .WithMessage("AreaHectares must be greater than zero.")
                .WithErrorCode($"{nameof(SubmitPlotCommand.AreaHectares)}.GreaterThanZero")
                .LessThanOrEqualTo(1_000_000)
                .WithMessage("AreaHectares exceeds the maximum supported value.")
                .WithErrorCode($"{nameof(SubmitPlotCommand.AreaHectares)}.MaximumValue");

            RuleFor(x => x.Latitude)
                .InclusiveBetween(-90, 90)
                .When(x => x.Latitude.HasValue)
                .WithMessage("Latitude must be between -90 and 90.")
                .WithErrorCode($"{nameof(SubmitPlotCommand.Latitude)}.OutOfRange");

            RuleFor(x => x.Longitude)
                .InclusiveBetween(-180, 180)
                .When(x => x.Longitude.HasValue)
                .WithMessage("Longitude must be between -180 and 180.")
                .WithErrorCode($"{nameof(SubmitPlotCommand.Longitude)}.OutOfRange");

            RuleFor(x => x.BoundaryGeoJson)
                .MaximumLength(8000)
                .When(x => !string.IsNullOrWhiteSpace(x.BoundaryGeoJson))
                .WithMessage("BoundaryGeoJson must have at most 8000 characters.")
                .WithErrorCode($"{nameof(SubmitPlotCommand.BoundaryGeoJson)}.MaximumLength");

            RuleFor(x => x.PlantingDate)
                .NotEmpty()
                .WithMessage("PlantingDate is required.")
                .WithErrorCode($"{nameof(SubmitPlotCommand.PlantingDate)}.Required");

            RuleFor(x => x.ExpectedHarvestDate)
                .NotEmpty()
                .WithMessage("ExpectedHarvestDate is required.")
                .WithErrorCode($"{nameof(SubmitPlotCommand.ExpectedHarvestDate)}.Required")
                .GreaterThan(x => x.PlantingDate)
                .WithMessage("ExpectedHarvestDate must be greater than PlantingDate.")
                .WithErrorCode($"{nameof(SubmitPlotCommand.ExpectedHarvestDate)}.BeforePlanting");

            RuleFor(x => x.IrrigationType)
                .NotEmpty()
                .WithMessage("IrrigationType is required.")
                .WithErrorCode($"{nameof(SubmitPlotCommand.IrrigationType)}.Required")
                .Must(type => Domain.ValueObjects.IrrigationType.Create(type).IsSuccess)
                .WithMessage("IrrigationType is invalid.")
                .WithErrorCode($"{nameof(SubmitPlotCommand.IrrigationType)}.InvalidType");

            RuleFor(x => x.AdditionalNotes)
                .MaximumLength(1000)
                .When(x => !string.IsNullOrWhiteSpace(x.AdditionalNotes))
                .WithMessage("AdditionalNotes must have at most 1000 characters.")
                .WithErrorCode($"{nameof(SubmitPlotCommand.AdditionalNotes)}.MaximumLength");

            RuleFor(x => x.CropTypeCatalogId)
                .Must(id => !id.HasValue || id.Value != Guid.Empty)
                .WithMessage("CropTypeCatalogId cannot be empty when informed.")
                .WithErrorCode($"{nameof(SubmitPlotCommand.CropTypeCatalogId)}.Invalid");

            RuleFor(x => x.SelectedCropTypeSuggestionId)
                .Must(id => !id.HasValue || id.Value != Guid.Empty)
                .WithMessage("SelectedCropTypeSuggestionId cannot be empty when informed.")
                .WithErrorCode($"{nameof(SubmitPlotCommand.SelectedCropTypeSuggestionId)}.Invalid");

            RuleFor(x => x)
                .Must(x => x.Latitude.HasValue == x.Longitude.HasValue)
                .WithMessage("Latitude and Longitude must be informed together.")
                .WithErrorCode("GeoCoordinates.Incomplete");

            RuleFor(x => x)
                .Must(x => x.CropTypeCatalogId.HasValue || x.SelectedCropTypeSuggestionId.HasValue || !string.IsNullOrWhiteSpace(x.CropType))
                .WithMessage("CropType, CropTypeCatalogId or SelectedCropTypeSuggestionId must be informed.")
                .WithErrorCode("CropSelection.Required");
        }
    }
}