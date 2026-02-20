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
            Guid userId)
        {
            return new SensorOperationalStatusChangedIntegrationEvent(
                EventId: Guid.NewGuid(),
                AggregateId: aggregate.Id,
                OccurredOn: domainEvent.OccurredOn,
                SensorId: aggregate.Id,
                PlotId: aggregate.PlotId,
                PropertyId: Guid.Empty,  // Would need to load Plot navigation property
                PreviousStatus: domainEvent.PreviousStatus,
                NewStatus: domainEvent.NewStatus,
                Reason: string.Empty,  // Could be added to command if needed
                ChangedByUserId: userId,
                RelatedIds: new Dictionary<string, Guid>
                {
                    ["PlotId"] = aggregate.PlotId,
                    ["PropertyId"] = Guid.Empty,
                    ["ChangedByUserId"] = userId
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
