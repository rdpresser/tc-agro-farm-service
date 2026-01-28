namespace TC.Agro.Farm.Domain.Aggregates
{
    /// <summary>
    /// Domain errors for Farm bounded context.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public static class FarmDomainErrors
    {
        #region Property Errors

        public static readonly ValidationError OwnerIdRequired =
            new("Property.OwnerIdRequired", "Owner ID is required.");

        public static readonly ValidationError PropertyNotFound =
            new("Property.NotFound", "Property not found.");

        public static readonly ValidationError PropertyAlreadyDeactivated =
            new("Property.AlreadyDeactivated", "Property is already deactivated.");

        public static readonly ValidationError PropertyAlreadyActivated =
            new("Property.AlreadyActivated", "Property is already activated.");

        #endregion

        #region Plot Errors

        public static readonly ValidationError PropertyIdRequired =
            new("Plot.PropertyIdRequired", "Property ID is required.");

        public static readonly ValidationError PlotNotFound =
            new("Plot.NotFound", "Plot not found.");

        public static readonly ValidationError PlotAlreadyDeactivated =
            new("Plot.AlreadyDeactivated", "Plot is already deactivated.");

        public static readonly ValidationError PlotAlreadyActivated =
            new("Plot.AlreadyActivated", "Plot is already activated.");

        #endregion

        #region Sensor Errors

        public static readonly ValidationError PlotIdRequired =
            new("Sensor.PlotIdRequired", "Plot ID is required.");

        public static readonly ValidationError SensorNotFound =
            new("Sensor.NotFound", "Sensor not found.");

        public static readonly ValidationError SensorAlreadyActive =
            new("Sensor.AlreadyActive", "Sensor is already active.");

        public static readonly ValidationError SensorAlreadyInactive =
            new("Sensor.AlreadyInactive", "Sensor is already inactive.");

        public static readonly ValidationError SensorAlreadyInMaintenance =
            new("Sensor.AlreadyInMaintenance", "Sensor is already in maintenance.");

        public static readonly ValidationError SensorAlreadyFaulty =
            new("Sensor.AlreadyFaulty", "Sensor is already marked as faulty.");

        public static readonly ValidationError SensorAlreadyDeactivated =
            new("Sensor.AlreadyDeactivated", "Sensor is already deactivated.");

        public static readonly ValidationError SensorAlreadyActivated =
            new("Sensor.AlreadyActivated", "Sensor is already activated.");

        #endregion
    }
}
