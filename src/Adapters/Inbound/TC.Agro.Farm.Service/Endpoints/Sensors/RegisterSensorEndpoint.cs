using System.Net;

using Bogus;

using FastEndpoints;

using TC.Agro.Farm.Application.Abstractions;
using TC.Agro.Farm.Application.UseCases.Sensors.RegisterSensor;
using TC.Agro.SharedKernel.Api.Endpoints;
using TC.Agro.SharedKernel.Application.Behaviors;
using TC.Agro.SharedKernel.Infrastructure;

namespace TC.Agro.Farm.Service.Endpoints.Sensors
{
    public sealed class RegisterSensorEndpoint : BaseApiEndpoint<RegisterSensorCommand, RegisterSensorResponse>
    {
        private static readonly string[] SensorTypes = ["Temperature", "Humidity", "SoilMoisture", "Rainfall"];

        public override void Configure()
        {
            Post("sensor");
            PostProcessor<LoggingCommandPostProcessorBehavior<RegisterSensorCommand, RegisterSensorResponse>>();

            Roles(AppConstants.AdminRole, AppConstants.ProducerRole);
            Description(
                x => x.Produces<RegisterSensorResponse>(201)
                      .ProducesProblemDetails()
                      .Produces((int)HttpStatusCode.NotFound)
                      .Produces((int)HttpStatusCode.Forbidden)
                      .Produces((int)HttpStatusCode.Unauthorized));

            var faker = new Faker();
            Summary(s =>
            {
                s.Summary = "Register a new sensor in a plot.";
                s.Description = "This endpoint allows producers or admins to register a new IoT sensor within an existing plot. Supported types: Temperature, Humidity, SoilMoisture, Rainfall.";
                s.ExampleRequest = new RegisterSensorCommand(
                    Guid.NewGuid(),
                    faker.PickRandom(SensorTypes),
                    "Sensor Norte 1");
                s.ResponseExamples[201] = new RegisterSensorResponse(
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    "Temperature",
                    "Active",
                    "Sensor Norte 1",
                    DateTimeOffset.UtcNow);
                s.Responses[201] = "Returned when the sensor is successfully registered.";
                s.Responses[400] = "Returned when the request contains validation errors.";
                s.Responses[401] = "Returned when the request is made without a valid user token.";
                s.Responses[403] = "Returned when the caller lacks the required role to register sensors.";
                s.Responses[404] = "Returned when the plot is not found.";
            });
        }

        public override async Task HandleAsync(RegisterSensorCommand req, CancellationToken ct)
        {
            var response = await req.ExecuteAsync(ct: ct).ConfigureAwait(false);

            if (response.IsSuccess)
            {
                string location = $"/api/sensor/{response.Value.Id}";
                object routeValues = new { id = response.Value.Id };
                await Send.CreatedAtAsync(location, routeValues, response.Value, cancellation: ct).ConfigureAwait(false);
                return;
            }

            await MatchResultAsync(response, ct).ConfigureAwait(false);
        }
    }
}
