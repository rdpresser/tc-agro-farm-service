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
            Guid sensorId,
            Guid plotId,
            Guid propertyId,
            string? reason,
            Guid changedByUserId)
        {
            return new SensorOperationalStatusChangedIntegrationEvent(
                EventId: Guid.NewGuid(),
                AggregateId: sensorId,
                OccurredOn: domainEvent.OccurredOn,
                SensorId: sensorId,
                PlotId: plotId,
                PropertyId: propertyId,
                PreviousStatus: "Unknown",  // Will be captured in handler
                NewStatus: domainEvent.Status,
                Reason: reason,
                ChangedByUserId: changedByUserId,
                RelatedIds: new Dictionary<string, Guid>
                {
                    ["PlotId"] = plotId,
                    ["PropertyId"] = propertyId,
                    ["ChangedByUserId"] = changedByUserId
                }
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
