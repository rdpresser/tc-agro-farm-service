namespace TC.Agro.Farm.Application.UseCases.Properties.List
{
    /// <summary>
    /// Response item for property list.
    /// </summary>
    public sealed record ListPropertiesResponse(
        Guid Id,
        string Name,
        string City,
        string State,
        string Country,
        double AreaHectares,
        Guid OwnerId,
        bool IsActive,
        int PlotCount,
        DateTimeOffset CreatedAt);
}
