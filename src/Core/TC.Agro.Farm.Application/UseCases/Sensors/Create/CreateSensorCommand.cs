namespace TC.Agro.Farm.Application.UseCases.Sensors.Create
{
    /// <summary>
    /// Command to register a new sensor in a plot.
    /// </summary>
    public sealed record CreateSensorCommand(
        Guid PlotId,
        string Type,
        string? Label = null,
        Guid? OwnerId = null) : IBaseCommand<CreateSensorResponse>, IInvalidateCache
    {
        public IReadOnlyCollection<string> CacheTags =>
        [
            CacheTagCatalog.Sensors,
            CacheTagCatalog.SensorList,
            CacheTagCatalog.SensorById
        ];
    }
}
