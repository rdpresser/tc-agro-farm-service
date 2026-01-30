namespace TC.Agro.Farm.Application.UseCases.Sensors.RegisterSensor
{
    /// <summary>
    /// Command to register a new sensor in a plot.
    /// </summary>
    public sealed record RegisterSensorCommand(
        Guid PlotId,
        string Type,
        string? Label = null) : IBaseCommand<RegisterSensorResponse>;
}
