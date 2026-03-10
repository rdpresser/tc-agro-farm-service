namespace TC.Agro.Farm.Application.UseCases.CropTypes.Delete
{
    public sealed record DeleteCropTypeResponse(
        Guid Id,
        DateTimeOffset DeactivatedAt);
}
