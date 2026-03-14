namespace TC.Agro.Farm.Application.UseCases.CropTypes.Regenerate
{
    public sealed record RegeneratePropertyCropTypesResponse(
        Guid PropertyId,
        string Status,
        DateTimeOffset QueuedAt);
}
