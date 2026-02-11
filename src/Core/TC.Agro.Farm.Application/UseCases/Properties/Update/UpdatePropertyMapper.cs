namespace TC.Agro.Farm.Application.UseCases.Properties.Update
{
    public static class UpdatePropertyMapper
    {
        public static UpdatePropertyResponse FromAggregate(PropertyAggregate aggregate)
        {
            return new UpdatePropertyResponse(
                Id: aggregate.Id,
                Name: aggregate.Name.Value,
                Address: aggregate.Location.Address,
                City: aggregate.Location.City,
                State: aggregate.Location.State,
                Country: aggregate.Location.Country,
                Latitude: aggregate.Location.Latitude,
                Longitude: aggregate.Location.Longitude,
                AreaHectares: aggregate.AreaHectares.Hectares,
                OwnerId: aggregate.OwnerId,
                UpdatedAt: aggregate.UpdatedAt ?? aggregate.CreatedAt);
        }

        public static PropertyUpdatedIntegrationEvent ToIntegrationEvent(PropertyUpdatedDomainEvent domainEvent, Guid ownerId)
            => new(
                domainEvent.AggregateId,
                domainEvent.Name,
                domainEvent.Address,
                domainEvent.City,
                domainEvent.State,
                domainEvent.Country,
                domainEvent.Latitude,
                domainEvent.Longitude,
                domainEvent.AreaHectares,
                ownerId,
                domainEvent.OccurredOn);
    }
}
