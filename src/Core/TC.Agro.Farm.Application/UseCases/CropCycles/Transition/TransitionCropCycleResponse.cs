namespace TC.Agro.Farm.Application.UseCases.CropCycles.Transition
{
    /// <summary>
    /// Response returned after successfully transitioning a crop cycle status.
    /// </summary>
    public sealed record TransitionCropCycleResponse(
        Guid Id,
        Guid PlotId,
        Guid PropertyId,
        string Status,
        DateTimeOffset? EndedAt,
        string? Notes,
        DateTimeOffset UpdatedAt);
}
