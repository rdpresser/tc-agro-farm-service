namespace TC.Agro.Farm.Application.UseCases.Plots.ListAll
{
    public sealed record ListPlotsResponse(
        Guid Id,
        Guid PropertyId,
        Guid OwnerId,
        string OwnerName,
        string PropertyName,
        string Name,
        string CropType,
        double AreaHectares,
        bool IsActive,
        int SensorCount,
        DateTimeOffset CreatedAt,
        DateTimeOffset PlantingDate,
        DateTimeOffset ExpectedHarvestDate,
        string IrrigationType,
        string? AdditionalNotes);
}
