namespace TC.Agro.Farm.Application.UseCases.Properties.CreateProperty
{
    /// <summary>
    /// Response after creating a property.
    /// </summary>
    public sealed record CreatePropertyResponse(
        Guid Id,
        string Name,
        string Address,
        string City,
        string State,
        string Country,
        double? Latitude,
        double? Longitude,
        double AreaHectares,
        Guid OwnerId,
        DateTimeOffset CreatedAt);
}
