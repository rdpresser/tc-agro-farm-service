namespace TC.Agro.Farm.Application.UseCases.Sensors.Deactivate
{
    /// <summary>
    /// Response for sensor deactivation (soft-delete) operation.
    /// </summary>
    public sealed record DeactivateSensorResponse(
        Guid SensorId,
        DateTimeOffset DeactivatedAt);
}
