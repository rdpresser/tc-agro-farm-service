namespace TC.Agro.Farm.Application.UseCases.Sensors.ListAll
{
    /// <summary>
    /// Response item for sensor list.
    /// </summary>
    public sealed record ListSensorsResponse(
        Guid Id,
        Guid PlotId,
        string PlotName,
        Guid PropertyId,
        string PropertyName,
        Guid OwnerId,
        string OwnerName,
        string Type,
        string Status,
        string? Label,
        DateTimeOffset InstalledAt);
}
