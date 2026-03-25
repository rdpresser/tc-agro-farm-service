namespace TC.Agro.Farm.Application.UseCases.Plots.Submit
{
    public sealed record SubmitPlotResponse(
        Guid Id,
        Guid PropertyId,
        Guid OwnerId,
        string Name,
        string CropType,
        double AreaHectares,
        double? Latitude,
        double? Longitude,
        string? BoundaryGeoJson,
        DateTimeOffset PlantingDate,
        DateTimeOffset ExpectedHarvestDate,
        string IrrigationType,
        string? AdditionalNotes,
        bool IsActive,
        DateTimeOffset CreatedAt,
        DateTimeOffset? UpdatedAt,
        Guid CropTypeCatalogId,
        Guid? SelectedCropTypeSuggestionId = null,
        bool IsCreated = false);
}