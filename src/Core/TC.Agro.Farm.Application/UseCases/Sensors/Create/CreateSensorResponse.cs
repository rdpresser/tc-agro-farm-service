namespace TC.Agro.Farm.Application.UseCases.Sensors.Create
{
    /// <summary>
    /// Response after registering a sensor.
    /// </summary>
    public sealed record CreateSensorResponse(
        Guid Id,
        Guid PlotId,
        string Type,
        string Status,
        string? Label,
        DateTimeOffset InstalledAt);
}
