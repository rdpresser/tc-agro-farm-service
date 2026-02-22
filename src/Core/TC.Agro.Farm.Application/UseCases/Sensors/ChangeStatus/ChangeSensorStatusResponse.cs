namespace TC.Agro.Farm.Application.UseCases.Sensors.ChangeStatus
{
    /// <summary>
    /// Response for sensor status change operation.
    /// </summary>
    public sealed record ChangeSensorStatusResponse(
        Guid SensorId,
        string NewStatus,
        DateTimeOffset ChangedAt);
}
