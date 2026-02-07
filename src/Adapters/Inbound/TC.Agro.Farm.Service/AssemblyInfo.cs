global using System.Diagnostics.CodeAnalysis;
global using System.Net;
global using Bogus;
global using FastEndpoints;
global using FastEndpoints.Security;
global using FastEndpoints.Swagger;
global using FluentValidation;
global using FluentValidation.Resources;
global using HealthChecks.UI.Client;
global using JasperFx.Resources;
global using Microsoft.AspNetCore.Diagnostics.HealthChecks;
global using Microsoft.EntityFrameworkCore;
global using Microsoft.Extensions.Caching.StackExchangeRedis;
global using Microsoft.Extensions.Diagnostics.HealthChecks;
global using Newtonsoft.Json.Converters;
global using Npgsql;
global using NSwag.AspNetCore;
global using OpenTelemetry;
global using OpenTelemetry.Logs;
global using OpenTelemetry.Metrics;
global using OpenTelemetry.Resources;
global using OpenTelemetry.Trace;
global using Serilog;
global using TC.Agro.Farm.Application;
global using TC.Agro.Farm.Application.Abstractions;
global using TC.Agro.Farm.Application.UseCases.Plots.CreatePlot;
global using TC.Agro.Farm.Application.UseCases.Plots.GetPlotById;
global using TC.Agro.Farm.Application.UseCases.Plots.GetPlotList;
global using TC.Agro.Farm.Application.UseCases.Properties.CreateProperty;
global using TC.Agro.Farm.Application.UseCases.Properties.GetPropertyById;
global using TC.Agro.Farm.Application.UseCases.Properties.GetPropertyList;
global using TC.Agro.Farm.Application.UseCases.Properties.UpdateProperty;
global using TC.Agro.Farm.Application.UseCases.Sensors.GetSensorById;
global using TC.Agro.Farm.Application.UseCases.Sensors.GetSensorList;
global using TC.Agro.Farm.Application.UseCases.Sensors.RegisterSensor;
global using TC.Agro.Farm.Infrastructure;
global using TC.Agro.Farm.Service.Extensions;
global using TC.Agro.Farm.Service.Telemetry;
global using TC.Agro.SharedKernel.Api.Endpoints;
global using TC.Agro.SharedKernel.Api.Extensions;
global using TC.Agro.SharedKernel.Application.Behaviors;
global using TC.Agro.SharedKernel.Extensions;
global using TC.Agro.SharedKernel.Infrastructure.Authentication;
global using TC.Agro.SharedKernel.Infrastructure.Caching.HealthCheck;
global using TC.Agro.SharedKernel.Infrastructure.Caching.Provider;
global using TC.Agro.SharedKernel.Infrastructure.Database;
global using TC.Agro.SharedKernel.Infrastructure.Database.EfCore;
global using TC.Agro.SharedKernel.Infrastructure.MessageBroker;
global using TC.Agro.SharedKernel.Infrastructure.Messaging;
global using TC.Agro.SharedKernel.Infrastructure.Middleware;
global using TC.Agro.SharedKernel.Infrastructure.Telemetry;
global using Wolverine;
global using Wolverine.EntityFrameworkCore;
global using Wolverine.ErrorHandling;
global using Wolverine.Postgresql;
global using Wolverine.RabbitMQ;
// ZiggyCreatures.Caching.Fusion
global using ZiggyCreatures.Caching.Fusion;
global using ZiggyCreatures.Caching.Fusion.Backplane.StackExchangeRedis;
global using ZiggyCreatures.Caching.Fusion.Serialization.SystemTextJson;
//**//
using System.Runtime.CompilerServices;
[assembly: InternalsVisibleTo("TC.Agro.Farm.Unit.Tests")]
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]
//**//REMARK: Required for functional and integration tests to work.
namespace TC.Agro.Farm.Service
{
    public partial class Program;
}
