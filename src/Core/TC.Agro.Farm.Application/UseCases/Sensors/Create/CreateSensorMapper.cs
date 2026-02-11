namespace TC.Agro.Farm.Application.UseCases.Sensors.Create
{
    public static class CreateSensorMapper
    {
        public static Result<SensorAggregate> ToAggregate(CreateSensorCommand command)
        {
            return SensorAggregate.Create(
                command.PlotId,
                command.Type,
                command.Label);
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

        public static SensorRegisteredIntegrationEvent ToIntegrationEvent(SensorRegisteredDomainEvent domainEvent)
            => new(
                domainEvent.AggregateId,
                domainEvent.PlotId,
                domainEvent.Type,
                domainEvent.Status,
                domainEvent.Label,
                domainEvent.OccurredOn);
    }
}
