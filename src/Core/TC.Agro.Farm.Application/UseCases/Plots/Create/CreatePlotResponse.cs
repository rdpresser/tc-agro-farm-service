namespace TC.Agro.Farm.Application.UseCases.Plots.Create
{
    /// <summary>
    /// Response after creating a plot.
    /// </summary>
    public sealed record CreatePlotResponse(
        Guid Id,
        Guid PropertyId,
        string Name,
        string CropType,
        double AreaHectares,
        DateTimeOffset PlantingDate,
        DateTimeOffset ExpectedHarvestDate,
        string IrrigationType,
        bool IsActive,
        DateTimeOffset CreatedAt);
}
