using System.Diagnostics;

namespace TC.Agro.Farm.Service.Telemetry
{
    /// <summary>
    /// Factory for creating ActivitySource instances for different domains.
    /// These activity sources are used to emit traces in handlers and business logic.
    /// </summary>
    internal static class ActivitySourceFactory
    {
        /// <summary>
        /// Activity source for handlers (CQRS Commands/Queries)
        /// Usage: StartActivity for processing commands/queries
        /// </summary>
        public static readonly ActivitySource Handlers =
            new(TelemetryConstants.HandlersActivitySource, TelemetryConstants.Version);

        /// <summary>
        /// Activity source for FastEndpoints operations
        /// Usage: StartActivity for FastEndpoint pipeline events
        /// </summary>
        public static readonly ActivitySource FastEndpoints =
            new(TelemetryConstants.FastEndpointsActivitySource, TelemetryConstants.Version);

        /// <summary>
        /// Activity source for database operations (beyond ORM instrumentation)
        /// Usage: custom spans for complex database operations
        /// </summary>
        public static readonly ActivitySource Database =
            new(TelemetryConstants.DatabaseActivitySource, TelemetryConstants.Version);

        /// <summary>
        /// Activity source for cache operations (beyond automatic instrumentation)
        /// Usage: custom spans for cache strategies or key-value patterns
        /// </summary>
        public static readonly ActivitySource Cache =
            new(TelemetryConstants.CacheActivitySource, TelemetryConstants.Version);

        // ============================================================
        // Helper Methods for Common Patterns
        // ============================================================

        /// <summary>
        /// Start a handler operation span (Command/Query processing)
        /// </summary>
        /// <param name="operationName">Handler name (e.g., "CreatePropertyCommand")</param>
        /// <param name="userId">User ID performing the operation</param>
        public static Activity? StartHandlerOperation(string operationName, string userId = TelemetryConstants.AnonymousUser)
        {
            var activity = Handlers.StartActivity(operationName);
            activity?.SetTag("handler.name", operationName);
            activity?.SetTag("user.id", userId);
            return activity;
        }

        /// <summary>
        /// Start a database operation span (beyond automatic instrumentation)
        /// </summary>
        /// <param name="operation">Operation name (e.g., "GetPropertyById")</param>
        /// <param name="tableName">Table name involved</param>
        public static Activity? StartDatabaseOperation(string operation, string tableName)
        {
            var activity = Database.StartActivity(operation);
            activity?.SetTag("db.operation", operation);
            activity?.SetTag("db.table", tableName);
            return activity;
        }

        /// <summary>
        /// Start a cache operation span
        /// </summary>
        /// <param name="operation">Operation (get, set, delete)</param>
        /// <param name="cacheKey">Cache key being accessed</param>
        public static Activity? StartCacheOperation(string operation, string cacheKey)
        {
            var activity = Cache.StartActivity(operation);
            activity?.SetTag("cache.operation", operation);
            activity?.SetTag("cache.key", cacheKey);
            return activity;
        }

        /// <summary>
        /// Start a farm-specific operation span
        /// </summary>
        /// <param name="operationName">Operation name (e.g., "CreateProperty", "RegisterSensor")</param>
        /// <param name="entityType">Entity type (property, plot, sensor)</param>
        /// <param name="entityId">Entity ID if available</param>
        /// <param name="userId">User ID performing the operation</param>
        public static Activity? StartFarmOperation(string operationName, string entityType, string? entityId = null, string userId = TelemetryConstants.AnonymousUser)
        {
            var activity = Handlers.StartActivity(operationName);
            activity?.SetTag("farm.operation", operationName);
            activity?.SetTag("farm.entity_type", entityType);
            activity?.SetTag("user.id", userId);

            if (!string.IsNullOrWhiteSpace(entityId))
            {
                activity?.SetTag("farm.entity_id", entityId);
            }

            return activity;
        }

        /// <summary>
        /// Start an endpoint operation span
        /// </summary>
        /// <param name="endpointName">Endpoint name (e.g., "CreateProperty")</param>
        /// <param name="userId">User ID calling the endpoint</param>
        public static Activity? StartEndpointOperation(string endpointName, string userId = TelemetryConstants.AnonymousUser)
        {
            var activity = FastEndpoints.StartActivity(endpointName);
            activity?.SetTag("endpoint.name", endpointName);
            activity?.SetTag("user.id", userId);
            return activity;
        }
    }
}