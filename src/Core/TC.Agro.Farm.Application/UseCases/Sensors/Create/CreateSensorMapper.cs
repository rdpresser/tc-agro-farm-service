namespace TC.Agro.Farm.Application.UseCases.Sensors.Create
{
    public static class CreateSensorMapper
    {
        public static Result<SensorAggregate> ToAggregate(CreateSensorCommand command, Guid ownerId, Guid propertyId, Guid plotId, string propertyName, string plotName)
        {
            return SensorAggregate.Create(
                ownerId: ownerId,
                propertyId: propertyId,
                plotId: plotId,
                label: command.Label,
                propertyName: propertyName,
                plotName: plotName,
                command.Type);
        }

        public static CreateSensorResponse FromAggregate(SensorAggregate aggregate)
        {
            return new CreateSensorResponse(
                Id: aggregate.Id,
                PlotId: aggregate.PlotId,
                Type: aggregate.Type.Value,
                Status: aggregate.Status.Value,
                Label: aggregate.Label?.Value,
                InstalledAt: aggregate.InstalledAt);
        }

        public static SensorRegisteredIntegrationEvent ToIntegrationEvent(SensorRegisteredDomainEvent domainEvent, Guid ownerId)
            => new(
                SensorId: domainEvent.AggregateId,
                OwnerId: ownerId,
                PropertyId: domainEvent.PropertyId,
                PlotId: domainEvent.PlotId,
                Label: domainEvent.Label,
                PropertyName: domainEvent.PropertyName,
                PlotName: domainEvent.PlotName,
                Type: domainEvent.Type,
                Status: domainEvent.Status,
                OccurredOn: domainEvent.OccurredOn);
    }
}
