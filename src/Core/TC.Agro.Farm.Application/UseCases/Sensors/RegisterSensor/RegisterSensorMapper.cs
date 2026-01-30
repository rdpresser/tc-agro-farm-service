namespace TC.Agro.Farm.Application.UseCases.Sensors.RegisterSensor
{
    public static class RegisterSensorMapper
    {
        public static Result<SensorAggregate> ToAggregate(RegisterSensorCommand command)
        {
            return SensorAggregate.Create(
                command.PlotId,
                command.Type,
                command.Label);
        }

        public static RegisterSensorResponse FromAggregate(SensorAggregate aggregate)
        {
            return new RegisterSensorResponse(
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
