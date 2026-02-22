namespace TC.Agro.Farm.Application.UseCases.Sensors.ChangeStatus
{
    /// <summary>
    /// Command to change a sensor's operational status.
    /// 
    /// Transitions sensor between states: Active, Inactive, Maintenance, Faulty
    /// This is different from Deactivate (soft delete) which sets IsActive = false.
    /// </summary>
    public sealed record ChangeSensorStatusCommand(
        Guid SensorId,
        string NewStatus,
        string? Reason = null) : IBaseCommand<ChangeSensorStatusResponse>, IInvalidateCache
    {
        public IReadOnlyCollection<string> CacheTags =>
        [
            CacheTagCatalog.Sensors,
            CacheTagCatalog.SensorList,
            CacheTagCatalog.SensorById
        ];
    }
}
