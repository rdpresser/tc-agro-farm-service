using TC.Agro.Farm.Application.UseCases.Sensors.GetSensorById;

namespace TC.Agro.Farm.Service.Endpoints.Sensors
{
    public sealed class GetSensorByIdEndpoint : BaseApiEndpoint<GetSensorByIdQuery, SensorByIdResponse>
    {
        public override void Configure()
        {
            Get("sensors/{id:guid}");

            // Force FastEndpoints to bind from route params (not JSON body)
            RequestBinder(new RequestBinder<GetSensorByIdQuery>(BindingSource.RouteValues));

            Roles(AppConstants.UserRole, AppConstants.AdminRole, AppConstants.ProducerRole);
            PreProcessor<QueryCachingPreProcessorBehavior<GetSensorByIdQuery, SensorByIdResponse>>();
            PostProcessor<QueryCachingPostProcessorBehavior<GetSensorByIdQuery, SensorByIdResponse>>();

            Description(
                x => x.Produces<SensorByIdResponse>(200)
                      .ProducesProblemDetails()
                      .Produces((int)HttpStatusCode.NotFound)
                      .Produces((int)HttpStatusCode.Forbidden)
                      .Produces((int)HttpStatusCode.Unauthorized));

            Summary(s =>
            {
                s.Summary = "Get sensor details by ID.";
                s.Description = "Retrieves detailed information about a specific sensor including type, status, plot, and property information.";
                s.ExampleRequest = new GetSensorByIdQuery { Id = Guid.NewGuid() };
                s.ResponseExamples[200] = new SensorByIdResponse(
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    "Talhão Norte",
                    Guid.NewGuid(),
                    "Fazenda Boa Vista",
                    "Temperature",
                    "Active",
                    "Sensor Norte 1",
                    DateTimeOffset.UtcNow.AddDays(-30),
                    DateTimeOffset.UtcNow.AddDays(-7));
                s.Responses[200] = "Returned when the sensor is found.";
                s.Responses[400] = "Returned when the request is invalid.";
                s.Responses[401] = "Returned when the request is made without a valid user token.";
                s.Responses[403] = "Returned when the caller lacks the required role.";
                s.Responses[404] = "Returned when no sensor is found with the given ID.";
            });
        }

        public override async Task HandleAsync(GetSensorByIdQuery req, CancellationToken ct)
        {
            var response = await req.ExecuteAsync(ct: ct).ConfigureAwait(false);
            await MatchResultAsync(response, ct).ConfigureAwait(false);
        }
    }
}
