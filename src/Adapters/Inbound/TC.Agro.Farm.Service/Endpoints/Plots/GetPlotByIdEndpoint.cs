namespace TC.Agro.Farm.Service.Endpoints.Plots
{
    public sealed class GetPlotByIdEndpoint : BaseApiEndpoint<GetPlotByIdQuery, PlotByIdResponse>
    {
        public override void Configure()
        {
            Get("plot/{id:guid}");

            // Force FastEndpoints to bind from route params (not JSON body)
            RequestBinder(new RequestBinder<GetPlotByIdQuery>(BindingSource.RouteValues));

            Roles(AppConstants.UserRole, AppConstants.AdminRole, AppConstants.ProducerRole);
            PreProcessor<QueryCachingPreProcessorBehavior<GetPlotByIdQuery, PlotByIdResponse>>();
            PostProcessor<QueryCachingPostProcessorBehavior<GetPlotByIdQuery, PlotByIdResponse>>();

            Description(
                x => x.Produces<PlotByIdResponse>(200)
                      .ProducesProblemDetails()
                      .Produces((int)HttpStatusCode.NotFound)
                      .Produces((int)HttpStatusCode.Forbidden)
                      .Produces((int)HttpStatusCode.Unauthorized));

            Summary(s =>
            {
                s.Summary = "Get plot details by ID.";
                s.Description = "Retrieves detailed information about a specific plot including crop type, area, and sensor count.";
                s.ExampleRequest = new GetPlotByIdQuery { Id = Guid.NewGuid() };
                s.ResponseExamples[200] = new PlotByIdResponse(
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    "Fazenda Boa Vista",
                    "Talhão Norte",
                    "Soja",
                    50.0,
                    true,
                    3,
                    DateTimeOffset.UtcNow.AddDays(-30),
                    DateTimeOffset.UtcNow);
                s.Responses[200] = "Returned when the plot is found.";
                s.Responses[400] = "Returned when the request is invalid.";
                s.Responses[401] = "Returned when the request is made without a valid user token.";
                s.Responses[403] = "Returned when the caller lacks the required role.";
                s.Responses[404] = "Returned when no plot is found with the given ID.";
            });
        }

        public override async Task HandleAsync(GetPlotByIdQuery req, CancellationToken ct)
        {
            var response = await req.ExecuteAsync(ct: ct).ConfigureAwait(false);
            await MatchResultAsync(response, ct).ConfigureAwait(false);
        }
    }
}
