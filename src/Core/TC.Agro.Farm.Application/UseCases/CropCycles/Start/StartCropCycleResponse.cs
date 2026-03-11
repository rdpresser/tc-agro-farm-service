namespace TC.Agro.Farm.Application.UseCases.CropCycles.Start
{
    /// <summary>
    /// Response returned after successfully starting a crop cycle.
    /// </summary>
    public sealed record StartCropCycleResponse(
        Guid Id,
        Guid PlotId,
        Guid PropertyId,
        Guid CropTypeCatalogId,
        string Status,
        DateTimeOffset StartedAt,
        DateTimeOffset? ExpectedHarvestDate,
        Guid? SelectedCropTypeSuggestionId,
        string? Notes,
        DateTimeOffset CreatedAt);
}
