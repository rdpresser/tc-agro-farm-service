using TC.Agro.Farm.Application.UseCases.Plots.Update;

namespace TC.Agro.Farm.Tests.Application.UseCases.Plots.Update
{
    public sealed class UpdatePlotCommandValidatorTests
    {
        private readonly UpdatePlotCommandValidator _validator = new();

        [Fact]
        public void Validate_WithValidCommand_ShouldSucceed()
        {
            var command = CreateValidCommand();

            var result = _validator.Validate(command);

            result.IsValid.ShouldBeTrue();
        }

        [Fact]
        public void Validate_WithEmptyPlotId_ShouldFail()
        {
            var command = CreateValidCommand() with { PlotId = Guid.Empty };

            var result = _validator.Validate(command);

            result.IsValid.ShouldBeFalse();
            result.Errors.ShouldContain(x => x.PropertyName == nameof(UpdatePlotCommand.PlotId));
        }

        [Fact]
        public void Validate_WithExpectedHarvestBeforePlanting_ShouldFail()
        {
            var plantingDate = DateTimeOffset.UtcNow;
            var command = CreateValidCommand() with
            {
                PlantingDate = plantingDate,
                ExpectedHarvestDate = plantingDate.AddDays(-1)
            };

            var result = _validator.Validate(command);

            result.IsValid.ShouldBeFalse();
            result.Errors.ShouldContain(x => x.PropertyName == nameof(UpdatePlotCommand.ExpectedHarvestDate));
        }

        private static UpdatePlotCommand CreateValidCommand()
            => new(
                PlotId: Guid.NewGuid(),
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
