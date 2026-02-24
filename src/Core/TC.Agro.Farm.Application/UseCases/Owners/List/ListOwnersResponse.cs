namespace TC.Agro.Farm.Application.UseCases.Owners.List
{
    /// <summary>
    /// Response item for owner listing.
    /// </summary>
    public sealed record ListOwnersResponse(
        Guid Id,
        string Name,
        string Email,
        bool IsActive,
        DateTimeOffset CreatedAt,
        DateTimeOffset? UpdatedAt);
}
