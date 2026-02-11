namespace TC.Agro.Farm.Application.UseCases.Properties.Create
{
    public static class CreatePropertyMapper
    {
        public static Result<PropertyAggregate> ToAggregate(CreatePropertyCommand command, Guid ownerId)
        {
            return PropertyAggregate.Create(
                command.Name,
                command.Address,
                command.City,
                command.State,
                command.Country,
                command.AreaHectares,
                ownerId,
                command.Latitude,
                command.Longitude);
        }

        public static CreatePropertyResponse FromAggregate(PropertyAggregate aggregate)
        {
            return new CreatePropertyResponse(
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
                CreatedAt: aggregate.CreatedAt);
        }

        public static PropertyCreatedIntegrationEvent ToIntegrationEvent(PropertyCreatedDomainEvent domainEvent)
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
                domainEvent.OwnerId,
                domainEvent.OccurredOn);
    }
}
