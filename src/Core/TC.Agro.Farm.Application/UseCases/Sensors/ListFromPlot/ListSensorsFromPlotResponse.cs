namespace TC.Agro.Farm.Application.UseCases.Sensors.ListFromPlot
{
    /// <summary>
    /// Response item for sensor list.
    /// </summary>
    public sealed record ListSensorsFromPlotResponse(
        Guid Id,
        Guid PlotId,
        string PlotName,
        Guid PropertyId,
        string PropertyName,
        string Type,
        string Status,
        string? Label,
        DateTimeOffset InstalledAt);
}
