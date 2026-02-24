namespace TC.Agro.Farm.Domain.Abstractions
{
    public interface ITenantAware
    {
        Guid OwnerId { get; }
    }
}
