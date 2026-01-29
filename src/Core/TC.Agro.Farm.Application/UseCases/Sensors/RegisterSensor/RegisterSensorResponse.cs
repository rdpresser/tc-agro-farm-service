namespace TC.Agro.Farm.Application.UseCases.Sensors.RegisterSensor
{
    /// <summary>
    /// Response after registering a sensor.
    /// </summary>
    public sealed record RegisterSensorResponse(
        Guid Id,
        Guid PlotId,
        string Type,
        string Status,
        string? Label,
        DateTimeOffset InstalledAt);
}
