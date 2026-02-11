namespace TC.Agro.Farm.Application.UseCases.Plots.ListByProperty
{
    /// <summary>
    /// Response item for plot list.
    /// </summary>
    public sealed record ListPlotsFromPropertyResponse(
        Guid Id,
        Guid PropertyId,
        string PropertyName,
        string Name,
        string CropType,
        double AreaHectares,
        bool IsActive,
        int SensorCount,
        DateTimeOffset CreatedAt);
}
