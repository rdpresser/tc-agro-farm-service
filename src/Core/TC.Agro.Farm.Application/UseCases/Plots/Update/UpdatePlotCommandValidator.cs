using TC.Agro.Farm.Domain.ValueObjects;
using TC.Agro.SharedKernel.Extensions;

namespace TC.Agro.Farm.Application.UseCases.Plots.Update
{
    public sealed class UpdatePlotCommandValidator : Validator<UpdatePlotCommand>
    {
        public UpdatePlotCommandValidator()
        {
            RuleFor(x => x.PlotId)
                .NotEmpty()
                .WithMessage("Plot Id is required.")
                .WithErrorCode($"{nameof(UpdatePlotCommand.PlotId)}.Required");

            RuleFor(x => x.Name)
                .NotEmpty()
                .WithMessage("Name is required.")
                .WithErrorCode($"{nameof(UpdatePlotCommand.Name)}.Required")
                .MinimumLength(3)
                .WithMessage("Name must be at least 3 characters long.")
                .WithErrorCode($"{nameof(UpdatePlotCommand.Name)}.MinimumLength")
                .MaximumLength(200)
                .WithMessage("Name must not exceed 200 characters.")
                .WithErrorCode($"{nameof(UpdatePlotCommand.Name)}.MaximumLength");

            RuleFor(x => x.CropType)
                .MinimumLength(2)
                .When(x => !string.IsNullOrWhiteSpace(x.CropType))
                .WithMessage("Crop type must be at least 2 characters long.")
                .WithErrorCode($"{nameof(UpdatePlotCommand.CropType)}.MinimumLength")
                .MaximumLength(100)
                .When(x => !string.IsNullOrWhiteSpace(x.CropType))
                .WithMessage("Crop type must not exceed 100 characters.")
                .WithErrorCode($"{nameof(UpdatePlotCommand.CropType)}.MaximumLength");

            RuleFor(x => x)
                .Must(x => !string.IsNullOrWhiteSpace(x.CropType) || x.CropTypeCatalogId.HasValue)
                .WithMessage("Either CropType or CropTypeCatalogId must be informed.")
                .WithErrorCode("CropTypeCatalog.CompatibilityRequired");

            RuleFor(x => x.CropTypeCatalogId)
                .Must(id => !id.HasValue || id.Value != Guid.Empty)
                .WithMessage("CropTypeCatalogId cannot be empty when informed.")
                .WithErrorCode($"{nameof(UpdatePlotCommand.CropTypeCatalogId)}.Invalid");

            RuleFor(x => x.SelectedCropTypeSuggestionId)
                .Must(id => !id.HasValue || id.Value != Guid.Empty)
                .WithMessage("SelectedCropTypeSuggestionId cannot be empty when informed.")
                .WithErrorCode($"{nameof(UpdatePlotCommand.SelectedCropTypeSuggestionId)}.Invalid");

            RuleFor(x => x)
                .Must(x => !x.SelectedCropTypeSuggestionId.HasValue || x.CropTypeCatalogId.HasValue)
                .WithMessage("CropTypeCatalogId is required when SelectedCropTypeSuggestionId is informed.")
                .WithErrorCode($"{nameof(UpdatePlotCommand.CropTypeCatalogId)}.RequiredWhenSuggestionInformed");

            RuleFor(x => x.AreaHectares)
                .GreaterThan(0)
                .WithMessage("Area must be greater than zero.")
                .WithErrorCode($"{nameof(UpdatePlotCommand.AreaHectares)}.GreaterThanZero")
                .LessThanOrEqualTo(100_000)
                .WithMessage("Area must not exceed 100,000 hectares for a single plot.")
                .WithErrorCode($"{nameof(UpdatePlotCommand.AreaHectares)}.MaximumValue");

            RuleFor(x => x.Latitude)
                .InclusiveBetween(-90, 90)
                .When(x => x.Latitude.HasValue)
                .WithMessage("Latitude must be between -90 and 90.")
                .WithErrorCode($"{nameof(UpdatePlotCommand.Latitude)}.OutOfRange");

            RuleFor(x => x.Longitude)
                .InclusiveBetween(-180, 180)
                .When(x => x.Longitude.HasValue)
                .WithMessage("Longitude must be between -180 and 180.")
                .WithErrorCode($"{nameof(UpdatePlotCommand.Longitude)}.OutOfRange");

            RuleFor(x => x)
                .Must(x => x.Latitude.HasValue == x.Longitude.HasValue)
                .WithMessage("Latitude and Longitude must be informed together.")
                .WithErrorCode("GeoCoordinates.PairRequired");

            RuleFor(x => x.BoundaryGeoJson)
                .MaximumLength(20_000)
                .WithMessage("BoundaryGeoJson must not exceed 20,000 characters.")
                .WithErrorCode($"{nameof(UpdatePlotCommand.BoundaryGeoJson)}.MaximumLength");

            RuleFor(x => x.PlantingDate)
                .NotEqual(default(DateTimeOffset))
                .WithMessage("Planting date is required.")
                .WithErrorCode($"{nameof(UpdatePlotCommand.PlantingDate)}.Required");

            RuleFor(x => x.ExpectedHarvestDate)
                .NotEqual(default(DateTimeOffset))
                .WithMessage("Expected harvest date is required.")
                .WithErrorCode($"{nameof(UpdatePlotCommand.ExpectedHarvestDate)}.Required")
                .GreaterThan(x => x.PlantingDate)
                .WithMessage("Expected harvest must be after planting date.")
                .WithErrorCode($"{nameof(UpdatePlotCommand.ExpectedHarvestDate)}.BeforePlanting");

            RuleFor(x => x.IrrigationType)
                .NotEmpty()
                .WithMessage("Irrigation type is required.")
                .WithErrorCode($"{nameof(UpdatePlotCommand.IrrigationType)}.Required")
                .Must(type => IrrigationType.ValidTypes.Contains(type, StringComparer.OrdinalIgnoreCase))
                .WithMessage(irrigationType =>
                    $"Irrigation type '{irrigationType.IrrigationType}' is not recognized. Valid types are: {IrrigationType.ValidTypes.JoinWithQuotes()}.")
                .WithErrorCode($"{nameof(UpdatePlotCommand.IrrigationType)}.InvalidType");

            RuleFor(x => x.AdditionalNotes)
                .MaximumLength(1000)
                .WithMessage("Additional notes must not exceed 1000 characters.")
                .WithErrorCode($"{nameof(UpdatePlotCommand.AdditionalNotes)}.MaximumLength");
        }
    }
}
