namespace TC.Agro.Farm.Application.UseCases.Properties.UpdateProperty
{
    /// <summary>
    /// Response after updating a property.
    /// </summary>
    public sealed record UpdatePropertyResponse(
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
        DateTimeOffset UpdatedAt);
}
