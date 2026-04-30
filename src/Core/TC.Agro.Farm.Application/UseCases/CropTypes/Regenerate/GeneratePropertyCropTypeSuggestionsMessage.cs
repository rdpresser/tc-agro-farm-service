namespace TC.Agro.Farm.Application.UseCases.CropTypes.Regenerate
{
    /// <summary>
    /// Internal async message to generate crop type suggestions for a property.
    /// Processed by Wolverine handlers.
    /// </summary>
    public sealed record GeneratePropertyCropTypeSuggestionsMessage(
        Guid PropertyId,
        Guid OwnerId,
        Guid TriggeredByUserId,
        string TriggerReason,
        DateTimeOffset RequestedAt);
}
