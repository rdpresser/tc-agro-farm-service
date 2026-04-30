namespace TC.Agro.Farm.Application.UseCases.CropCycles.Complete
{
    /// <summary>
    /// Response returned after successfully completing a crop cycle.
    /// </summary>
    public sealed record CompleteCropCycleResponse(
        Guid Id,
        Guid PlotId,
        Guid PropertyId,
        string Status,
        DateTimeOffset StartedAt,
        DateTimeOffset EndedAt,
        string? Notes,
        DateTimeOffset UpdatedAt);
}
