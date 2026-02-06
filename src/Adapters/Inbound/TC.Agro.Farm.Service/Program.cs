var builder = WebApplication.CreateBuilder(args);

builder.Services.AddFarmServices(builder);
builder.Services.AddApplication();
builder.Services.AddFarmInfrastructure(builder.Configuration);

// Configure Serilog as logging provider (using SharedKernel extension)
builder.Host.UseCustomSerilog(builder.Configuration, TelemetryConstants.ServiceName, TelemetryConstants.ServiceNamespace, TelemetryConstants.Version);

var app = builder.Build();

if (!builder.Environment.IsEnvironment("Testing"))
{
    await app.ApplyMigrations().ConfigureAwait(false);
}

// Get logger instance for Program and log telemetry configuration
var logger = app.Services.GetRequiredService<ILogger<TC.Agro.Farm.Service.Program>>();
TelemetryConstants.LogTelemetryConfiguration(logger, app.Configuration);

// Log APM/exporter configuration (Azure Monitor, OTLP, etc.)
// This info was populated during service configuration in ServiceCollectionExtensions
var exporterInfo = app.Services.GetService<TelemetryExporterInfo>();
TelemetryConstants.LogApmExporterConfiguration(logger, exporterInfo);

// Configure the HTTP request pipeline.
app.UseIngressPathBase(app.Configuration);

// Cross-Origin Resource Sharing (CORS)
app.UseCors("DefaultCorsPolicy");

// CRITICAL: Middleware execution order for correlation ID propagation
// 1. Custom exception handler (catches all exceptions)
// 2. CorrelationMiddleware (generates/extracts correlation ID and sets ICorrelationIdGenerator)
// 3. SerilogRequestLogging (logs HTTP requests with correlation ID)
// 4. Health checks (status endpoints)
// 5. TelemetryMiddleware (uses correlation ID from ICorrelationIdGenerator)
// 6. Authentication and Authorization (JWT validation)
// 7. FastEndpoints (route handlers)

app.UseCustomMiddlewares();

// CRITICAL: TelemetryMiddleware MUST come AFTER CorrelationMiddleware to access correlationIdGenerator.CorrelationId
app.UseMiddleware<TC.Agro.Farm.Service.Middleware.TelemetryMiddleware>();

app.UseAuthentication()
  .UseAuthorization()
  .UseCustomFastEndpoints(app.Configuration);

await app.RunAsync();
