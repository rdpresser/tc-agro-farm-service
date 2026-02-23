namespace TC.Agro.Farm.Application.UseCases.Sensors.Deactivate
{
    /// <summary>
    /// Mapper for sensor deactivation operations.
    /// Converts between commands, domain entities, and integration events.
    /// </summary>
    internal static class DeactivateSensorMapper
    {
        /// <summary>
        /// Maps domain event to integration event for cross-service communication.
        /// </summary>
        public static SensorDeactivatedIntegrationEvent ToIntegrationEvent(
            SensorAggregate.SensorDeactivatedDomainEvent domainEvent,
            SensorAggregate aggregate,
            Guid userId,
            string? reason = null)
        {
            return new SensorDeactivatedIntegrationEvent(
                SensorId: aggregate.Id,
                PlotId: aggregate.PlotId,
                PropertyId: aggregate.Plot.PropertyId,
                Reason: reason ?? "Sensor deactivated",
                DeactivatedByUserId: userId,
                OccurredOn: domainEvent.OccurredOn
            );
        }

        /// <summary>
        /// Maps sensor aggregate to response DTO.
        /// </summary>
        public static DeactivateSensorResponse FromAggregate(
            SensorAggregate sensor,
            DateTimeOffset deactivatedAt)
        {
            return new DeactivateSensorResponse(
                SensorId: sensor.Id,
                DeactivatedAt: deactivatedAt);
        }
    }
}
