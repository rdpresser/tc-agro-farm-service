namespace TC.Agro.Farm.Service.Endpoints.Plots
{
    public sealed class CreatePlotEndpoint : BaseApiEndpoint<CreatePlotCommand, CreatePlotResponse>
    {
        public override void Configure()
        {
            Post("plot");
            PostProcessor<LoggingCommandPostProcessorBehavior<CreatePlotCommand, CreatePlotResponse>>();
            PostProcessor<CacheInvalidationPostProcessorBehavior<CreatePlotCommand, CreatePlotResponse>>();

            Roles(AppConstants.AdminRole, AppConstants.ProducerRole);
            Description(
                x => x.Produces<CreatePlotResponse>(201)
                      .ProducesProblemDetails()
                      .Produces((int)HttpStatusCode.NotFound)
                      .Produces((int)HttpStatusCode.Forbidden)
                      .Produces((int)HttpStatusCode.Unauthorized));

            Summary(s =>
            {
                s.Summary = "Create a new plot within a property.";
                s.Description = "This endpoint allows producers or admins to register a new plot (talhão) within an existing property. Crop type is mandatory.";
                s.ExampleRequest = new CreatePlotCommand(
                    Guid.NewGuid(),
                    "Talhão Norte",
                    "Soja",
                    50.0);
                s.ResponseExamples[201] = new CreatePlotResponse(
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    "Talhão Norte",
                    "Soja",
                    50.0,
                    true,
                    DateTimeOffset.UtcNow);
                s.Responses[201] = "Returned when the plot is successfully created.";
                s.Responses[400] = "Returned when the request contains validation errors.";
                s.Responses[401] = "Returned when the request is made without a valid user token.";
                s.Responses[403] = "Returned when the caller lacks the required role to create plots.";
                s.Responses[404] = "Returned when the property is not found.";
            });
        }

        public override async Task HandleAsync(CreatePlotCommand req, CancellationToken ct)
        {
            var response = await req.ExecuteAsync(ct: ct).ConfigureAwait(false);

            if (response.IsSuccess)
            {
                string location = $"/api/plot/{response.Value.Id}";
                object routeValues = new { id = response.Value.Id };
                await Send.CreatedAtAsync(location, routeValues, response.Value, cancellation: ct).ConfigureAwait(false);
                return;
            }

            await MatchResultAsync(response, ct).ConfigureAwait(false);
        }
    }
}
