using TC.Agro.Farm.Application.UseCases.Properties.GetById;

namespace TC.Agro.Farm.Service.Endpoints.Properties
{
    public sealed class GetPropertyByIdEndpoint : BaseApiEndpoint<GetPropertyByIdQuery, GetPropertyByIdResponse>
    {
        public override void Configure()
        {
            Get("properties/{id:guid}");

            // Force FastEndpoints to bind from route params (not JSON body)
            RequestBinder(new RequestBinder<GetPropertyByIdQuery>(BindingSource.RouteValues));

            Roles(AppConstants.UserRole, AppConstants.AdminRole, AppConstants.ProducerRole);
            PreProcessor<QueryCachingPreProcessorBehavior<GetPropertyByIdQuery, GetPropertyByIdResponse>>();
            PostProcessor<QueryCachingPostProcessorBehavior<GetPropertyByIdQuery, GetPropertyByIdResponse>>();

            Description(
                x => x.Produces<GetPropertyByIdResponse>(200)
                      .ProducesProblemDetails()
                      .Produces((int)HttpStatusCode.NotFound)
                      .Produces((int)HttpStatusCode.Forbidden)
                      .Produces((int)HttpStatusCode.Unauthorized));

            Summary(s =>
            {
                s.Summary = "Get property details by ID.";
                s.Description = "Retrieves detailed information about a specific property including location, area, and plot count.";
                s.ExampleRequest = new GetPropertyByIdQuery { Id = Guid.NewGuid() };
                s.ResponseExamples[200] = new GetPropertyByIdResponse(
                    Guid.NewGuid(),
                    "Fazenda Boa Vista",
                    "Estrada Rural Km 15",
                    "Ribeirão Preto",
                    "SP",
                    "Brazil",
                    -21.1767,
                    -47.8208,
                    250.5,
                    Guid.NewGuid(),
                    true,
                    5,
                    DateTimeOffset.UtcNow.AddDays(-30),
                    DateTimeOffset.UtcNow);
                s.Responses[200] = "Returned when the property is found.";
                s.Responses[400] = "Returned when the request is invalid.";
                s.Responses[401] = "Returned when the request is made without a valid user token.";
                s.Responses[403] = "Returned when the caller lacks the required role.";
                s.Responses[404] = "Returned when no property is found with the given ID.";
            });
        }

        public override async Task HandleAsync(GetPropertyByIdQuery req, CancellationToken ct)
        {
            var response = await req.ExecuteAsync(ct: ct).ConfigureAwait(false);
            await MatchResultAsync(response, ct).ConfigureAwait(false);
        }
    }
}
