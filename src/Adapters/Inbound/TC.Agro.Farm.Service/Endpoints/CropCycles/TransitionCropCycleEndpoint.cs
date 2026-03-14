using TC.Agro.Farm.Application.UseCases.CropCycles.Transition;

namespace TC.Agro.Farm.Service.Endpoints.CropCycles
{
    /// <summary>
    /// Endpoint: PUT /api/crop-cycles/{cropCycleId}/transition
    ///
    /// Transitions an active crop cycle to the next lifecycle status.
    /// Valid target statuses: Planned, Planted, Growing, Harvesting.
    /// </summary>
    public sealed class TransitionCropCycleEndpoint : BaseApiEndpoint<TransitionCropCycleCommand, TransitionCropCycleResponse>
    {
        public override void Configure()
        {
            Put("crop-cycles/{cropCycleId}/transition");
            PostProcessor<LoggingCommandPostProcessorBehavior<TransitionCropCycleCommand, TransitionCropCycleResponse>>();
            this.AddCacheInvalidationIfNotTesting();
            Roles(AppConstants.AdminRole, AppConstants.ProducerRole);
            Description(
                x => x.Produces<TransitionCropCycleResponse>(200)
                      .ProducesProblemDetails()
                      .Produces((int)HttpStatusCode.BadRequest)
                      .Produces((int)HttpStatusCode.NotFound)
                      .Produces((int)HttpStatusCode.Forbidden)
                      .Produces((int)HttpStatusCode.Unauthorized));

            Summary(s =>
            {
                s.Summary = "Transition an active crop cycle to the next status.";
                s.Description = "Moves a crop cycle to a new intermediate lifecycle status (Planned → Planted → Growing → Harvesting). " +
                                "The cycle must not already be in a terminal status. Producers can only update cycles for their own plots.";
                s.ExampleRequest = new TransitionCropCycleCommand(
                    CropCycleId: Guid.NewGuid(),
                    NewStatus: "Growing",
                    OccurredAt: DateTimeOffset.UtcNow,
                    Notes: "Seedlings are established and growing.");
                s.Responses[200] = "Returned when the transition succeeds.";
                s.Responses[400] = "Returned when validation fails or the transition is not valid.";
                s.Responses[401] = "Returned when the request is made without a valid user token.";
                s.Responses[403] = "Returned when the caller lacks permission to update this cycle.";
                s.Responses[404] = "Returned when the crop cycle is not found.";
            });
        }

        public override async Task HandleAsync(TransitionCropCycleCommand req, CancellationToken ct)
        {
            var response = await req.ExecuteAsync(ct: ct).ConfigureAwait(false);
            await MatchResultAsync(response, ct).ConfigureAwait(false);
        }
    }
}
