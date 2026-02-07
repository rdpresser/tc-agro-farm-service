namespace TC.Agro.Farm.Service.Endpoints.Properties
{
    public sealed class UpdatePropertyEndpoint : BaseApiEndpoint<UpdatePropertyCommand, UpdatePropertyResponse>
    {
        public override void Configure()
        {
            Put("property/{id:guid}");
            PostProcessor<LoggingCommandPostProcessorBehavior<UpdatePropertyCommand, UpdatePropertyResponse>>();
            PostProcessor<CacheInvalidationPostProcessorBehavior<UpdatePropertyCommand, UpdatePropertyResponse>>();

            Roles(AppConstants.AdminRole, AppConstants.ProducerRole);
            Description(
                x => x.Produces<UpdatePropertyResponse>(200)
                      .ProducesProblemDetails()
                      .Produces((int)HttpStatusCode.NotFound)
                      .Produces((int)HttpStatusCode.Forbidden)
                      .Produces((int)HttpStatusCode.Unauthorized));

            Summary(s =>
            {
                s.Summary = "Update an existing property.";
                s.Description = "Updates property information. The ID in the route must match the ID in the request body.";
                s.ExampleRequest = new UpdatePropertyCommand(
                    Guid.NewGuid(),
                    "Fazenda Boa Vista Updated",
                    "Estrada Rural Km 20",
                    "Ribeirão Preto",
                    "SP",
                    "Brazil",
                    300.0,
                    -21.1800,
                    -47.8250);
                s.ResponseExamples[200] = new UpdatePropertyResponse(
                    Guid.NewGuid(),
                    "Fazenda Boa Vista Updated",
                    "Estrada Rural Km 20",
                    "Ribeirão Preto",
                    "SP",
                    "Brazil",
                    -21.1800,
                    -47.8250,
                    300.0,
                    Guid.NewGuid(),
                    DateTimeOffset.UtcNow);
                s.Responses[200] = "Returned when the property is successfully updated.";
                s.Responses[400] = "Returned when the request contains validation errors.";
                s.Responses[401] = "Returned when the request is made without a valid user token.";
                s.Responses[403] = "Returned when the caller lacks the required role.";
                s.Responses[404] = "Returned when no property is found with the given ID.";
            });
        }

        public override async Task HandleAsync(UpdatePropertyCommand req, CancellationToken ct)
        {
            var response = await req.ExecuteAsync(ct: ct).ConfigureAwait(false);
            await MatchResultAsync(response, ct).ConfigureAwait(false);
        }
    }
}
