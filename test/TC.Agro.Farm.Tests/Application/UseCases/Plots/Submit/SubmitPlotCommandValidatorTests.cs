using TC.Agro.Farm.Application.UseCases.Plots.Submit;

namespace TC.Agro.Farm.Tests.Application.UseCases.Plots.Submit;

public sealed class SubmitPlotCommandValidatorTests
{
    private readonly SubmitPlotCommandValidator _validator = new();

    [Fact]
    public void Validate_WhenCreateSubmitUsesSuggestionWithoutCatalog_ShouldBeValid()
    {
        var command = CreateValidCommand() with
        {
            CropTypeCatalogId = null,
            SelectedCropTypeSuggestionId = Guid.NewGuid()
        };

        var result = _validator.Validate(command);

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_WhenCreateSubmitOmitsPropertyId_ShouldBeInvalid()
    {
        var command = CreateValidCommand() with
        {
            PropertyId = Guid.Empty
        };

        var result = _validator.Validate(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(x => x.PropertyName == nameof(SubmitPlotCommand.PropertyId));
    }

    [Fact]
    public void Validate_WhenUpdateSubmitOmitsPropertyId_ShouldRemainValid()
    {
        var command = CreateValidCommand() with
        {
            PlotId = Guid.NewGuid(),
            PropertyId = Guid.Empty
        };

        var result = _validator.Validate(command);

        result.IsValid.ShouldBeTrue();
    }

    private static SubmitPlotCommand CreateValidCommand()
        => new()
        {
            PropertyId = Guid.NewGuid(),
            Name = "North Plot",
            CropType = "Soy",
            AreaHectares = 50,
            Latitude = -21.1775,
            Longitude = -47.8103,
            BoundaryGeoJson = "{\"type\":\"Polygon\"}",
            PlantingDate = DateTimeOffset.UtcNow.AddDays(-10),
            ExpectedHarvestDate = DateTimeOffset.UtcNow.AddDays(120),
            IrrigationType = "Center Pivot",
            AdditionalNotes = "Notes"
        };
}