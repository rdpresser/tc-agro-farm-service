using TC.Agro.Farm.Application.UseCases.Plots.Create;

namespace TC.Agro.Farm.Tests.Application.UseCases.Plots.Create
{
    public sealed class CreatePlotCommandValidatorTests
    {
        private readonly CreatePlotCommandValidator _validator = new();

        [Fact]
        public void Validate_WithValidLegacyCropType_ShouldSucceed()
        {
            var command = CreateValidCommand();

            var result = _validator.Validate(command);

            result.IsValid.ShouldBeTrue();
        }

        [Fact]
        public void Validate_WithEmptyCropTypeAndCatalogIdProvided_ShouldSucceed()
        {
            var command = CreateValidCommand() with
            {
                CropType = string.Empty,
                CropTypeCatalogId = Guid.NewGuid()
            };

            var result = _validator.Validate(command);

            result.IsValid.ShouldBeTrue();
        }

        [Fact]
        public void Validate_WithEmptyCropTypeAndNoCatalogId_ShouldFail()
        {
            var command = CreateValidCommand() with
            {
                CropType = string.Empty,
                CropTypeCatalogId = null
            };

            var result = _validator.Validate(command);

            result.IsValid.ShouldBeFalse();
            result.Errors.ShouldContain(x => x.ErrorCode == "CropTypeCatalog.CompatibilityRequired");
        }

        [Fact]
        public void Validate_WithSuggestionIdAndNoCatalogId_ShouldFail()
        {
            var command = CreateValidCommand() with
            {
                SelectedCropTypeSuggestionId = Guid.NewGuid(),
                CropTypeCatalogId = null
            };

            var result = _validator.Validate(command);

            result.IsValid.ShouldBeFalse();
            result.Errors.ShouldContain(x => x.ErrorCode == $"{nameof(CreatePlotCommand.CropTypeCatalogId)}.RequiredWhenSuggestionInformed");
        }

        private static CreatePlotCommand CreateValidCommand()
            => new(
                PropertyId: Guid.NewGuid(),
                Name: "North Plot",
                CropType: "Soy",
                AreaHectares: 50,
                Latitude: -21.1775,
                Longitude: -47.8103,
                BoundaryGeoJson: "{\"type\":\"Polygon\",\"coordinates\":[[[-47.811,-21.178],[-47.809,-21.178],[-47.809,-21.176],[-47.811,-21.176],[-47.811,-21.178]]]}",
                PlantingDate: DateTimeOffset.UtcNow.AddDays(-30),
                ExpectedHarvestDate: DateTimeOffset.UtcNow.AddDays(120),
                IrrigationType: "Center Pivot",
                AdditionalNotes: "Healthy crop progression");
    }
}
