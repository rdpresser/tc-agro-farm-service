namespace TC.Agro.Farm.Application.UseCases.Sensors.ChangeStatus
{
    /// <summary>
    /// Mapper for sensor status change operations.
    /// Converts between commands, domain entities, and integration events.
    /// </summary>
    internal static class ChangeSensorStatusMapper
    {
        /// <summary>
        /// Maps domain event to integration event for cross-service communication.
        /// </summary>
        public static SensorOperationalStatusChangedIntegrationEvent ToIntegrationEvent(
            SensorAggregate.SensorStatusChangedDomainEvent domainEvent,
            SensorAggregate aggregate,
            Guid userId,
            string? reason = null)
        {
            return new SensorOperationalStatusChangedIntegrationEvent(
                SensorId: aggregate.Id,
                OwnerId: aggregate.Plot.Property.OwnerId,
                PlotId: aggregate.PlotId,
                PropertyId: aggregate.Plot.PropertyId,
                Label: aggregate.Label?.Value,
                PropertyName: aggregate.Plot.Property.Name.Value,
                PlotName: aggregate.Plot.Name.Value,
                OccurredOn: domainEvent.OccurredOn
            );
        }

        /// <summary>
        /// Maps sensor aggregate to response DTO.
        /// </summary>
        public static ChangeSensorStatusResponse FromAggregate(
            SensorAggregate sensor,
            string previousStatus,
            DateTimeOffset changedAt)
        {
            return new ChangeSensorStatusResponse(
                SensorId: sensor.Id,
                PreviousStatus: previousStatus,
                NewStatus: sensor.Status.Value,
                ChangedAt: changedAt);
        }
    }
}
