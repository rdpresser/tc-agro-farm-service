namespace TC.Agro.Farm.Application.UseCases.Properties.GetById
{
    /// <summary>
    /// Response containing property details.
    /// </summary>
    public sealed record GetPropertyByIdResponse(
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
        bool IsActive,
        int PlotCount,
        DateTimeOffset CreatedAt,
        DateTimeOffset? UpdatedAt);
}
