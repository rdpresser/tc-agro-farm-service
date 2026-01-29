namespace TC.Agro.Farm.Application.UseCases.Properties.UpdateProperty
{
    /// <summary>
    /// Command to update an existing property.
    /// </summary>
    public sealed record UpdatePropertyCommand(
        Guid Id,
        string Name,
        string Address,
        string City,
        string State,
        string Country,
        double AreaHectares,
        double? Latitude = null,
        double? Longitude = null) : IBaseCommand<UpdatePropertyResponse>;
}
