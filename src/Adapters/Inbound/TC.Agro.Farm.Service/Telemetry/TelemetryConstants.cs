namespace TC.Agro.Farm.Service.Telemetry
{
    /// <summary>
    /// Constants for telemetry across the application
    /// </summary>
    internal static class TelemetryConstants
    {
        // Versions
        public const string Version = "1.0.0";

        // Service Identity - Centralized for consistency (matches Docker Compose)
        public const string ServiceName = "tc-agro-farm";
        public const string ServiceNamespace = "tcagro";

        // Meter Names for OpenTelemetry Metrics
        public const string FarmMeterName = "TC.Agro.Farm.Service.Metrics";

        // Activity Source Names for OpenTelemetry Tracing
        public const string FarmActivitySource = "TC.Agro.Farm.Service";
        public const string DatabaseActivitySource = "TC.Agro.Farm.Service.Database";
        public const string CacheActivitySource = "TC.Agro.Farm.Service.Cache";
        public const string HandlersActivitySource = "TC.Agro.Farm.Handlers";
        public const string FastEndpointsActivitySource = "TC.Agro.Farm.FastEndpoints";

        // Header Names (standardized)
        public const string CorrelationIdHeader = "X-Correlation-ID";

        // Tag Names (using underscores for consistency with Loki labels)
        public const string ServiceComponent = "service.component";
        public const string PropertyId = "property_id";
        public const string PlotId = "plot_id";
        public const string SensorId = "sensor_id";
        public const string FarmAction = "farm_action";
        public const string ErrorType = "error_type";

        // Default Values
        public const string AnonymousUser = "anonymous";

        // Service Components
        public const string FarmComponent = "farm";
        public const string DatabaseComponent = "database";
        public const string CacheComponent = "cache";

        /// <summary>
        /// Logs telemetry configuration details using Microsoft.Extensions.Logging.ILogger
        /// </summary>
        public static void LogTelemetryConfiguration(Microsoft.Extensions.Logging.ILogger logger, IConfiguration configuration)
        {
            logger.LogInformation("=== TELEMETRY DEBUG INFO ===");
            logger.LogInformation("Service Name: {ServiceName}", ServiceName);
            logger.LogInformation("Service Namespace: {ServiceNamespace}", ServiceNamespace);
            logger.LogInformation("Telemetry Version: {Version}", Version);
            logger.LogInformation("Correlation Header: {CorrelationIdHeader}", CorrelationIdHeader);
            logger.LogInformation("Farm Meter: {FarmMeterName}", FarmMeterName);
            logger.LogInformation("Farm Activity Source: {FarmActivitySource}", FarmActivitySource);
            logger.LogInformation("Database Activity Source: {DatabaseActivitySource}", DatabaseActivitySource);
            logger.LogInformation("Cache Activity Source: {CacheActivitySource}", CacheActivitySource);
            logger.LogInformation("Environment: {Environment}", Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "NOT SET");
            logger.LogInformation("Machine Name: {MachineName}", Environment.MachineName);
            logger.LogInformation("Container Name: {ContainerName}", Environment.GetEnvironmentVariable("HOSTNAME") ?? "NOT SET");

            // Load Grafana configuration via Helper
            var grafanaSettings = TC.Agro.SharedKernel.Infrastructure.Telemetry.GrafanaHelper.Build(configuration);

            logger.LogInformation("============================");
            logger.LogInformation("=== GRAFANA AGENT CONFIG ===");

            // Agent Status (CRITICAL INFO)
            if (grafanaSettings.Agent.Enabled)
            {
                logger.LogInformation("✅ Grafana Agent: ENABLED");
                logger.LogInformation("   → OTLP Export: ACTIVE");
                logger.LogInformation("   → Traces will be sent to Grafana Agent");
                logger.LogInformation("   → Logs: stdout → Agent → Grafana Cloud Loki");
                logger.LogInformation("   → Metrics: /metrics → Agent scrape → Grafana Cloud Prometheus");
            }
            else
            {
                logger.LogWarning("⚠️  Grafana Agent: DISABLED");
                logger.LogWarning("   → OTLP Export: INACTIVE");
                logger.LogWarning("   → Traces will be generated but NOT exported");
                logger.LogWarning("   → Logs: stdout only (not sent to Grafana Cloud)");
                logger.LogWarning("   → Metrics: /metrics endpoint available (not scraped)");
                logger.LogWarning("   → To enable: Set Grafana:Agent:Enabled=true or GRAFANA_AGENT_ENABLED=true");
            }

            logger.LogInformation("Agent Host: {AgentHost}", grafanaSettings.Agent.Host);
            logger.LogInformation("Agent OTLP gRPC Port: {OtlpGrpcPort}", grafanaSettings.Agent.OtlpGrpcPort);
            logger.LogInformation("Agent OTLP HTTP Port: {OtlpHttpPort}", grafanaSettings.Agent.OtlpHttpPort);
            logger.LogInformation("Agent Metrics Port: {MetricsPort}", grafanaSettings.Agent.MetricsPort);
            logger.LogInformation("OTLP Endpoint: {OtlpEndpoint}", grafanaSettings.Otlp.Endpoint);
            logger.LogInformation("OTLP Protocol: {OtlpProtocol}", grafanaSettings.Otlp.Protocol);
            logger.LogInformation("OTLP Headers: {OtlpHeaders}",
                string.IsNullOrWhiteSpace(grafanaSettings.Otlp.Headers) ? "NOT SET" : "***CONFIGURED***");
            logger.LogInformation("OTLP Timeout: {OtlpTimeout}s", grafanaSettings.Otlp.TimeoutSeconds);
            logger.LogInformation("OTLP Insecure: {OtlpInsecure}", grafanaSettings.Otlp.Insecure);
            logger.LogInformation("============================");
        }

        /// <summary>
        /// Logs APM/Telemetry exporter configuration details.
        /// This should be called from Program.cs after the logger is fully configured.
        /// </summary>
        public static void LogApmExporterConfiguration(Microsoft.Extensions.Logging.ILogger logger, TelemetryExporterInfo? exporterInfo)
        {
            if (exporterInfo == null)
            {
                logger.LogWarning("Telemetry exporter information not available");
                return;
            }

            logger.LogInformation("====================================================================================");

            switch (exporterInfo.ExporterType.ToUpperInvariant())
            {
                case "AZUREMONITOR":
                    logger.LogInformation("Azure Monitor configured - Telemetry will be exported to Application Insights");
                    logger.LogInformation("Using DefaultAzureCredential for RBAC/Workload Identity authentication");
                    if (exporterInfo.SamplingRatio.HasValue)
                    {
                        logger.LogInformation("Sampling Ratio: {SamplingRatio:P0}", exporterInfo.SamplingRatio.Value);
                    }
                    logger.LogInformation("Live Metrics: Enabled");
                    break;

                case "OTLP":
                    logger.LogInformation("OTLP Exporter configured - Endpoint: {Endpoint}, Protocol: {Protocol}",
                        exporterInfo.Endpoint ?? "NOT SET",
                        exporterInfo.Protocol ?? "NOT SET");
                    break;

                case "NONE":
                    logger.LogWarning("No APM exporter configured - Telemetry will be generated but NOT exported.");
                    logger.LogWarning("To enable Azure Monitor: Set APPLICATIONINSIGHTS_CONNECTION_STRING");
                    logger.LogWarning("To enable Grafana: Set GRAFANA_AGENT_ENABLED=true and OTEL_EXPORTER_OTLP_ENDPOINT");
                    break;

                default:
                    logger.LogWarning("Unknown telemetry exporter type: {ExporterType}", exporterInfo.ExporterType);
                    break;
            }

            logger.LogInformation("====================================================================================");
        }
    }
}
