using System.Diagnostics.Metrics;
using TC.Agro.SharedKernel;

namespace TC.Agro.Farm.Service.Telemetry
{
    /// <summary>
    /// Farm-specific metrics for tracking farm operations and user activities.
    /// 
    /// This class captures business domain metrics specific to farm operations:
    /// - Property and plot creation/management actions
    /// - Sensor registration and management
    /// - User activity patterns within farm context
    /// 
    /// Metrics are automatically exported to configured providers (OTLP, Prometheus, etc.)
    /// via OpenTelemetry configuration in ServiceCollectionExtensions.
    /// </summary>
    public class FarmMetrics
    {
        private readonly Counter<long> _farmActionsCounter;
        private readonly Counter<long> _propertyOperationsCounter;
        private readonly Counter<long> _plotOperationsCounter;
        private readonly Counter<long> _sensorOperationsCounter;
        private readonly Histogram<double> _operationDurationHistogram;
        private readonly Counter<long> _farmErrorsCounter;

        public FarmMetrics()
        {
            var meter = new Meter(TelemetryConstants.FarmMeterName, TelemetryConstants.Version);

            // General farm action counter
            _farmActionsCounter = meter.CreateCounter<long>(
                "farm_actions_total",
                description: "Total number of farm-related actions performed by users");

            // Domain-specific counters
            _propertyOperationsCounter = meter.CreateCounter<long>(
                "property_operations_total",
                description: "Total number of property operations (create, update, delete)");

            _plotOperationsCounter = meter.CreateCounter<long>(
                "plot_operations_total",
                description: "Total number of plot operations (create, update, delete)");

            _sensorOperationsCounter = meter.CreateCounter<long>(
                "sensor_operations_total",
                description: "Total number of sensor operations (register, update, delete)");

            // Performance metrics
            _operationDurationHistogram = meter.CreateHistogram<double>(
                "farm_operation_duration_seconds",
                description: "Duration of farm operations in seconds");

            // Error tracking
            _farmErrorsCounter = meter.CreateCounter<long>(
                "farm_errors_total",
                description: "Total number of errors in farm operations");
        }

        /// <summary>
        /// Records a general farm action performed by a user.
        /// </summary>
        public void RecordFarmAction(string action, string userId, string endpoint)
        {
            _farmActionsCounter.Add(1,
                new KeyValuePair<string, object?>("action", action.ToLowerInvariant()),
                new KeyValuePair<string, object?>("user_id", userId),
                new KeyValuePair<string, object?>("endpoint", endpoint),
                new KeyValuePair<string, object?>("service", "farm"));
        }

        /// <summary>
        /// Records a property-related operation (create, update, delete).
        /// </summary>
        public void RecordPropertyOperation(string operation, string userId, string? propertyId = null)
        {
            var tags = new List<KeyValuePair<string, object?>>
            {
                new("operation", operation.ToLowerInvariant()),
                new("user_id", userId),
                new("entity_type", "property")
            };

            if (!string.IsNullOrWhiteSpace(propertyId))
            {
                tags.Add(new("property_id", propertyId));
            }

            _propertyOperationsCounter.Add(1, tags.ToArray());
        }

        /// <summary>
        /// Records a plot-related operation (create, update, delete).
        /// </summary>
        public void RecordPlotOperation(string operation, string userId, string? plotId = null, string? propertyId = null)
        {
            var tags = new List<KeyValuePair<string, object?>>
            {
                new("operation", operation.ToLowerInvariant()),
                new("user_id", userId),
                new("entity_type", "plot")
            };

            if (!string.IsNullOrWhiteSpace(plotId))
            {
                tags.Add(new("plot_id", plotId));
            }

            if (!string.IsNullOrWhiteSpace(propertyId))
            {
                tags.Add(new("property_id", propertyId));
            }

            _plotOperationsCounter.Add(1, tags.ToArray());
        }

        /// <summary>
        /// Records a sensor-related operation (register, update, delete).
        /// </summary>
        public void RecordSensorOperation(string operation, string userId, string? sensorId = null, string? plotId = null)
        {
            var tags = new List<KeyValuePair<string, object?>>
            {
                new("operation", operation.ToLowerInvariant()),
                new("user_id", userId),
                new("entity_type", "sensor")
            };

            if (!string.IsNullOrWhiteSpace(sensorId))
            {
                tags.Add(new("sensor_id", sensorId));
            }

            if (!string.IsNullOrWhiteSpace(plotId))
            {
                tags.Add(new("plot_id", plotId));
            }

            _sensorOperationsCounter.Add(1, tags.ToArray());
        }

        /// <summary>
        /// Records the duration of a farm operation.
        /// </summary>
        public void RecordOperationDuration(string operation, string entityType, double durationSeconds, bool success = true)
        {
            _operationDurationHistogram.Record(durationSeconds,
                new KeyValuePair<string, object?>("operation", operation.ToLowerInvariant()),
                new KeyValuePair<string, object?>("entity_type", entityType),
                new KeyValuePair<string, object?>("success", success),
                new KeyValuePair<string, object?>("service", "farm")
            );
        }

        /// <summary>
        /// Records a farm-related error.
        /// </summary>
        public void RecordFarmError(string operation, string entityType, string errorType, string userId)
        {
            _farmErrorsCounter.Add(1,
                new KeyValuePair<string, object?>("operation", operation.ToLowerInvariant()),
                new KeyValuePair<string, object?>("entity_type", entityType),
                new KeyValuePair<string, object?>("error_type", errorType),
                new KeyValuePair<string, object?>("user_id", userId),
                new KeyValuePair<string, object?>("service", "farm")
            );
        }
    }
}
