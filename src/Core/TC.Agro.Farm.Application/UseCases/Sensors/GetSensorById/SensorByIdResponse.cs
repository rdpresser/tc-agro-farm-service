namespace TC.Agro.Farm.Application.UseCases.Sensors.GetSensorById
{
    /// <summary>
    /// Response containing sensor details.
    /// </summary>
    public sealed record SensorByIdResponse(
        Guid Id,
        Guid PlotId,
        string PlotName,
        Guid PropertyId,
        string PropertyName,
        string Type,
        string Status,
        string? Label,
        DateTimeOffset InstalledAt,
        DateTimeOffset? LastMaintenanceAt);
}
