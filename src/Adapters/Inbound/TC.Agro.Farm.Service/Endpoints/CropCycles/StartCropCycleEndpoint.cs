using TC.Agro.Farm.Application.UseCases.CropCycles.Start;

namespace TC.Agro.Farm.Service.Endpoints.CropCycles
{
    /// <summary>
    /// Endpoint: POST /api/crop-cycles
    ///
    /// Starts a new crop cycle for a plot.
    /// A plot can only have one active crop cycle at a time.
    /// </summary>
    public sealed class StartCropCycleEndpoint : BaseApiEndpoint<StartCropCycleCommand, StartCropCycleResponse>
    {
        public override void Configure()
        {
            Post("crop-cycles");
            PostProcessor<LoggingCommandPostProcessorBehavior<StartCropCycleCommand, StartCropCycleResponse>>();
            this.AddCacheInvalidationIfNotTesting();
            Roles(AppConstants.AdminRole, AppConstants.ProducerRole);
            Description(
                x => x.Produces<StartCropCycleResponse>(201)
                      .ProducesProblemDetails()
                      .Produces((int)HttpStatusCode.BadRequest)
                      .Produces((int)HttpStatusCode.NotFound)
                      .Produces((int)HttpStatusCode.Forbidden)
                      .Produces((int)HttpStatusCode.Unauthorized)
                      .Produces((int)HttpStatusCode.Conflict));

            Summary(s =>
            {
                s.Summary = "Start a new crop cycle for a plot.";
                s.Description = "Registers a new crop lifecycle on a plot. Only one active cycle is allowed per plot at a time. " +
                                "Producers can only start cycles for their own plots; Admins must provide OwnerId.";
                s.ExampleRequest = new StartCropCycleCommand(
                    PlotId: Guid.NewGuid(),
                    CropTypeCatalogId: Guid.NewGuid(),
                    StartedAt: DateTimeOffset.UtcNow,
                    ExpectedHarvestDate: DateTimeOffset.UtcNow.AddMonths(6),
                    Status: "Planned",
                    Notes: "Soil prepared and seeds selected.");
                s.Responses[201] = "Returned when the crop cycle is successfully started.";
                s.Responses[400] = "Returned when validation fails.";
                s.Responses[401] = "Returned when the request is made without a valid user token.";
                s.Responses[403] = "Returned when the caller lacks permission to start a cycle for this plot.";
                s.Responses[404] = "Returned when the plot is not found.";
                s.Responses[409] = "Returned when the plot already has an active crop cycle.";
            });
        }

        public override async Task HandleAsync(StartCropCycleCommand req, CancellationToken ct)
        {
            var response = await req.ExecuteAsync(ct: ct).ConfigureAwait(false);

            if (response.IsSuccess)
            {
                string location = $"/api/crop-cycles/{response.Value.Id}";
                object routeValues = new { cropCycleId = response.Value.Id };
                await Send.CreatedAtAsync(location, routeValues, response.Value, cancellation: ct).ConfigureAwait(false);
                return;
            }

            await MatchResultAsync(response, ct).ConfigureAwait(false);
        }
    }
}
