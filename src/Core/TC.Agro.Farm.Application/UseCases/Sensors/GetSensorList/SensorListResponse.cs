namespace TC.Agro.Farm.Application.UseCases.Sensors.GetSensorList
{
    /// <summary>
    /// Response item for sensor list.
    /// </summary>
    public sealed record SensorListResponse(
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
