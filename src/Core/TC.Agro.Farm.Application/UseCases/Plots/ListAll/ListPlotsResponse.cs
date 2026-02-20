namespace TC.Agro.Farm.Application.UseCases.Plots.ListAll
{
    public sealed record ListPlotsResponse(
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
