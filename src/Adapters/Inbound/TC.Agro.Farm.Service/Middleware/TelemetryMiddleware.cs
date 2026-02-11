using System.Diagnostics;
using System.Security.Claims;
using Serilog.Context;

namespace TC.Agro.Farm.Service.Middleware
{
    /// <summary>
    /// Middleware for capturing telemetry (traces, metrics, logs) for incoming HTTP requests.
    ///
    /// Responsibilities:
    /// - Create root activity (span) for each request
    /// - Capture user context and correlation ID
    /// - Record system and farm metrics
    /// - Add structured logging context (LogicalCallContext)
    /// - Measure request duration
    /// - Handle errors and status codes appropriately
    ///
    /// The activity created here becomes the parent for all child spans within the request.
    /// Log entries automatically include trace_id and span_id via Serilog.Enrichers.Span.
    ///
    /// Note: Correlation ID is managed by CorrelationMiddleware (must run BEFORE this middleware).
    /// </summary>
    public class TelemetryMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<TelemetryMiddleware> _logger;
        private readonly FarmMetrics _farmMetrics;
        private readonly SystemMetrics _systemMetrics;

        public TelemetryMiddleware(
            RequestDelegate next,
            ILogger<TelemetryMiddleware> logger,
            FarmMetrics farmMetrics,
            SystemMetrics systemMetrics)
        {
            _next = next;
            _logger = logger;
            _farmMetrics = farmMetrics;
            _systemMetrics = systemMetrics;
        }

        public async Task InvokeAsync(HttpContext context, ICorrelationIdGenerator correlationIdGenerator)
        {
            var stopwatch = Stopwatch.StartNew();
            var path = context.Request.Path.Value ?? "/";

            // Skip telemetry collection for infrastructure endpoints
            if (ShouldSkipTelemetry(path))
            {
                await _next(context);
                return;
            }

            // Extract user context
            var userId = ExtractUserId(context);
            var isAuthenticated = context.User?.Identity?.IsAuthenticated ?? false;
            var userRoles = ExtractUserRoles(context);

            // Get correlation ID from centralized generator (set by CorrelationMiddleware)
            // CorrelationMiddleware MUST run before TelemetryMiddleware in the pipeline
            var correlationId = correlationIdGenerator.CorrelationId ?? context.TraceIdentifier ?? "unknown";

            // Add to structured logging context (will be included in all logs within this request)
            using (LogContext.PushProperty("correlation_id", correlationId))
            using (LogContext.PushProperty("user.id", userId))
            using (LogContext.PushProperty("user.authenticated", isAuthenticated))
            {
                // Create root activity (span) for this request
                using var activity = ActivitySourceFactory.Handlers.StartActivity($"http_request_{context.Request.Method}");

                if (activity != null)
                {
                    // Set standard OTEL semantic conventions
                    activity.SetTag("http.method", context.Request.Method);
                    activity.SetTag("http.path", path);
                    activity.SetTag("http.target", context.Request.Path + context.Request.QueryString);
                    activity.SetTag("user.id", userId);
                    activity.SetTag("user.authenticated", isAuthenticated);
                    activity.SetTag("correlation_id", correlationId);

                    // Add user roles if authenticated
                    if (isAuthenticated && !string.IsNullOrWhiteSpace(userRoles))
                    {
                        activity.SetTag("user.roles", userRoles);
                    }
                }

                try
                {
                    // Record farm action if authenticated
                    if (isAuthenticated)
                    {
                        _farmMetrics.RecordFarmAction($"{context.Request.Method}", userId, path);
                    }

                    // Continue with pipeline
                    await _next(context);

                    stopwatch.Stop();
                    var durationSeconds = stopwatch.Elapsed.TotalSeconds;

                    // Record metrics
                    _systemMetrics.RecordHttpRequest(context.Request.Method, path, context.Response.StatusCode, durationSeconds);

                    // Update activity with response details
                    if (activity != null)
                    {
                        activity.SetTag("http.status_code", context.Response.StatusCode);
                        activity.SetTag("http.duration_ms", stopwatch.ElapsedMilliseconds);

                        // Set activity status based on HTTP response code
                        if (context.Response.StatusCode >= 200 && context.Response.StatusCode < 300)
                        {
                            activity.SetStatus(ActivityStatusCode.Ok);
                            LogSuccessResponse(context, path, durationSeconds, userId, correlationId);
                        }
                        else if (context.Response.StatusCode >= 300 && context.Response.StatusCode < 400)
                        {
                            activity.SetStatus(ActivityStatusCode.Ok);
                            LogRedirectResponse(context, path, durationSeconds, userId, correlationId);
                        }
                        else if (context.Response.StatusCode >= 400 && context.Response.StatusCode < 500)
                        {
                            activity.SetStatus(ActivityStatusCode.Error, "Client Error");
                            LogClientErrorResponse(context, path, durationSeconds, userId, correlationId);
                        }
                        else if (context.Response.StatusCode >= 500)
                        {
                            activity.SetStatus(ActivityStatusCode.Error, "Server Error");
                            LogServerErrorResponse(context, path, durationSeconds, userId, correlationId);
                        }
                    }
                }
#pragma warning disable S2139 // False positive: We log with context then rethrow for global handler
                catch (Exception ex)
#pragma warning restore S2139
                {
                    stopwatch.Stop();
                    var durationSeconds = stopwatch.Elapsed.TotalSeconds;

                    // Record error metrics
                    _systemMetrics.RecordHttpRequest(context.Request.Method, path, context.Response.StatusCode, durationSeconds);

                    // Update activity with exception details
                    if (activity != null)
                    {
                        activity.SetStatus(ActivityStatusCode.Error, ex.Message);
                        activity.SetTag("error.type", ex.GetType().Name);
                        activity.SetTag("error.message", ex.Message);
                        activity.SetTag("http.duration_ms", stopwatch.ElapsedMilliseconds);
                    }

                    _logger.LogError(ex,
                        "Request {Method} {Path} failed after {DurationMs}ms for user {UserId} with correlation {CorrelationId}",
                        context.Request.Method, path, stopwatch.ElapsedMilliseconds, userId, correlationId);

                    // Re-throw to let global exception handler deal with it
                    throw;
                }
            }
        }

        private static bool ShouldSkipTelemetry(string path)
        {
            return path.Contains("/health", StringComparison.OrdinalIgnoreCase) ||
                   path.Contains("/metrics", StringComparison.OrdinalIgnoreCase) ||
                   path.Contains("/prometheus", StringComparison.OrdinalIgnoreCase) ||
                   path.Contains("/swagger", StringComparison.OrdinalIgnoreCase);
        }

        private static string ExtractUserId(HttpContext context)
        {
            // Try to get user ID from JWT 'sub' claim
            var sub = context.User?.FindFirst("sub")?.Value;
            if (!string.IsNullOrWhiteSpace(sub))
                return sub;

            // Fallback to NameIdentifier claim
            var nameId = context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrWhiteSpace(nameId))
                return nameId;

            // Fallback to Name claim
            var name = context.User?.Identity?.Name;
            if (!string.IsNullOrWhiteSpace(name))
                return name;

            return TelemetryConstants.AnonymousUser;
        }

        private static string ExtractUserRoles(HttpContext context)
        {
            var roles = context.User?.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
            return roles?.Count > 0 ? string.Join(",", roles) : "";
        }

        private void LogSuccessResponse(HttpContext context, string path, double durationSeconds, string userId, string correlationId)
        {
            _logger.LogInformation(
                "Request {Method} {Path} completed successfully in {DurationMs}ms with status {StatusCode} for user {UserId} with correlation {CorrelationId}",
                context.Request.Method, path, (long)(durationSeconds * 1000), context.Response.StatusCode, userId, correlationId);
        }

        private void LogRedirectResponse(HttpContext context, string path, double durationSeconds, string userId, string correlationId)
        {
            _logger.LogInformation(
                "Request {Method} {Path} redirected in {DurationMs}ms with status {StatusCode} for user {UserId} with correlation {CorrelationId}",
                context.Request.Method, path, (long)(durationSeconds * 1000), context.Response.StatusCode, userId, correlationId);
        }

        private void LogClientErrorResponse(HttpContext context, string path, double durationSeconds, string userId, string correlationId)
        {
            var statusCode = context.Response.StatusCode;
            var logLevel = statusCode == 401 || statusCode == 403 ? "Security-related" : "Client";

            _logger.LogWarning(
                "{LogLevel} error: Request {Method} {Path} completed with status {StatusCode} in {DurationMs}ms for user {UserId} with correlation {CorrelationId}",
                logLevel, context.Request.Method, path, statusCode, (long)(durationSeconds * 1000), userId, correlationId);
        }

        private void LogServerErrorResponse(HttpContext context, string path, double durationSeconds, string userId, string correlationId)
        {
            _logger.LogError(
                "Request {Method} {Path} server error in {DurationMs}ms with status {StatusCode} for user {UserId} with correlation {CorrelationId}",
                context.Request.Method, path, (long)(durationSeconds * 1000), context.Response.StatusCode, userId, correlationId);
        }
    }
}