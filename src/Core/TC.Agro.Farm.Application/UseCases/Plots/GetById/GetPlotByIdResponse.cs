namespace TC.Agro.Farm.Application.UseCases.Plots.GetById
{
    /// <summary>
    /// Response containing plot details.
    /// </summary>
    public sealed record GetPlotByIdResponse(
        Guid Id,
        Guid PropertyId,
        string PropertyName,
        string Name,
        string CropType,
        double AreaHectares,
        bool IsActive,
        int SensorCount,
        DateTimeOffset CreatedAt,
        DateTimeOffset? UpdatedAt);
}
