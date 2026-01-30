namespace TC.Agro.Farm.Application.UseCases.Properties.CreateProperty
{
    /// <summary>
    /// Command to create a new property.
    /// </summary>
    public sealed record CreatePropertyCommand(
        string Name,
        string Address,
        string City,
        string State,
        string Country,
        double AreaHectares,
        double? Latitude = null,
        double? Longitude = null) : IBaseCommand<CreatePropertyResponse>;
}
