namespace TC.Agro.Farm.Application.UseCases.Plots.Update
{
    /// <summary>
    /// Response with updated plot details.
    /// </summary>
    public sealed record UpdatePlotResponse(
        Guid PlotId,
        Guid PropertyId,
        string Name,
        string CropType,
        double AreaHectares,
        double? Latitude,
        double? Longitude,
        DateTimeOffset PlantingDate,
        DateTimeOffset ExpectedHarvestDate,
        string IrrigationType,
        string? AdditionalNotes,
        DateTimeOffset? UpdatedAt,
        Guid CropTypeCatalogId,
        Guid? SelectedCropTypeSuggestionId = null);
}
