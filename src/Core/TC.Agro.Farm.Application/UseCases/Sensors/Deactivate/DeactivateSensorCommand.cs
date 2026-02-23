namespace TC.Agro.Farm.Application.UseCases.Sensors.Deactivate
{
    /// <summary>
    /// Command to deactivate (soft-delete) a sensor.
    /// 
    /// Sets the sensor's IsActive flag to false.
    /// This is different from changing operational status (Active, Inactive, Maintenance, Faulty).
    /// </summary>
    public sealed record DeactivateSensorCommand(
        Guid SensorId,
        string? Reason = null) : IBaseCommand<DeactivateSensorResponse>, IInvalidateCache
    {
        public IReadOnlyCollection<string> CacheTags =>
        [
            CacheTagCatalog.Sensors,
            CacheTagCatalog.SensorList,
            CacheTagCatalog.SensorById
        ];
    }
}
