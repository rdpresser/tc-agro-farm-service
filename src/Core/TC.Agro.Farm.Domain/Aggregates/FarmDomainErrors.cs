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

        public static readonly ValidationError PropertyLocationChangeBlockedByActiveCropCycles =
            new(
                "Property.LocationChangeBlockedByActiveCropCycles",
                "Property location cannot be changed while there are active crop cycles.");

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

        public static readonly ValidationError PlantingDateRequired =
            new("Plot.PlantingDateRequired", "Planting date is required.");

        public static readonly ValidationError PlantingDateFuture =
            new("Plot.PlantingDateFuture", "Planting date cannot be in the future.");

        public static readonly ValidationError ExpectedHarvestRequired =
            new("Plot.ExpectedHarvestRequired", "Expected harvest date is required.");

        public static readonly ValidationError ExpectedHarvestBeforePlanting =
            new("Plot.ExpectedHarvestBeforePlanting", "Expected harvest must be after planting date.");

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

        public static readonly ValidationError InvalidSensorStatus =
            new("Sensor.InvalidStatus", "The provided sensor status is invalid.");

        #endregion

        #region Crop Type Catalog Errors

        public static readonly ValidationError CropTypeCatalogNotFound =
            new("CropTypeCatalog.NotFound", "Crop type catalog entry not found.");

        public static readonly ValidationError CropTypeCatalogAlreadyDeactivated =
            new("CropTypeCatalog.AlreadyDeactivated", "Crop type catalog entry is already deactivated.");

        public static readonly ValidationError CropTypeCatalogAlreadyActivated =
            new("CropTypeCatalog.AlreadyActivated", "Crop type catalog entry is already activated.");

        #endregion

        #region Crop Type Suggestion Errors

        public static readonly ValidationError CropTypeSuggestionNotFound =
            new("CropTypeSuggestion.NotFound", "Crop type suggestion not found.");

        public static readonly ValidationError CropTypeSuggestionAlreadyDeactivated =
            new("CropTypeSuggestion.AlreadyDeactivated", "Crop type suggestion is already deactivated.");

        public static readonly ValidationError CropTypeSuggestionPropertyIdRequired =
            new("CropTypeSuggestion.PropertyIdRequired", "Property ID is required.");

        #endregion

        #region Crop Cycle Errors

        public static readonly ValidationError CropCycleNotFound =
            new("CropCycle.NotFound", "Crop cycle not found.");

        public static readonly ValidationError CropCycleAlreadyActiveForPlot =
            new("CropCycle.ActiveCycleAlreadyExists", "The plot already has an active crop cycle.");

        #endregion
    }
}
