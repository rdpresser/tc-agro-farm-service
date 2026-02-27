namespace TC.Agro.Farm.Application.UseCases.Plots.ListByProperty
{
    /// <summary>
    /// Response item for plot list.
    /// </summary>
    public sealed record ListPlotsFromPropertyResponse(
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
