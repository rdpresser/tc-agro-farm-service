var builder = WebApplication.CreateBuilder(args);

// Configure Serilog as logging provider (using SharedKernel extension)
builder.Host.UseCustomSerilog(builder.Configuration, TelemetryConstants.ServiceName, TelemetryConstants.ServiceNamespace, TelemetryConstants.Version);
builder.Services.AddFarmServices(builder);
builder.Services.AddApplication();
builder.Services.AddFarmInfrastructure(builder.Configuration);

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

app.UseAuthentication()
  .UseAuthorization()
  .UseCustomFastEndpoints(app.Configuration)
  .UseCustomMiddlewares();

await app.RunAsync();
