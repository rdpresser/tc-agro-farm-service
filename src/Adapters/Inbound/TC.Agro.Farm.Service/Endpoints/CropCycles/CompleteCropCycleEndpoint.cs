using TC.Agro.Farm.Application.UseCases.CropCycles.Complete;

namespace TC.Agro.Farm.Service.Endpoints.CropCycles
{
    /// <summary>
    /// Endpoint: POST /api/crop-cycles/{cropCycleId}/complete
    ///
    /// Completes a crop cycle with a terminal status (Harvested or Cancelled).
    /// Once completed, the cycle cannot transition further.
    /// </summary>
    public sealed class CompleteCropCycleEndpoint : BaseApiEndpoint<CompleteCropCycleCommand, CompleteCropCycleResponse>
    {
        public override void Configure()
        {
            Post("crop-cycles/{cropCycleId}/complete");
            PostProcessor<LoggingCommandPostProcessorBehavior<CompleteCropCycleCommand, CompleteCropCycleResponse>>();
            this.AddCacheInvalidationIfNotTesting();
            Roles(AppConstants.AdminRole, AppConstants.ProducerRole);
            Description(
                x => x.Produces<CompleteCropCycleResponse>(200)
                      .ProducesProblemDetails()
                      .Produces((int)HttpStatusCode.BadRequest)
                      .Produces((int)HttpStatusCode.NotFound)
                      .Produces((int)HttpStatusCode.Forbidden)
                      .Produces((int)HttpStatusCode.Unauthorized));

            Summary(s =>
            {
                s.Summary = "Complete a crop cycle (Harvested or Cancelled).";
                s.Description = "Finalises a crop lifecycle with a terminal status. The cycle must still be in an active status. " +
                                "Producers can only complete their own cycles; Admins can complete any cycle.";
                s.ExampleRequest = new CompleteCropCycleCommand(
                    CropCycleId: Guid.NewGuid(),
                    EndedAt: DateTimeOffset.UtcNow,
                    FinalStatus: "Harvested",
                    Notes: "Harvest completed successfully. Yield recorded.");
                s.Responses[200] = "Returned when the crop cycle is successfully completed.";
                s.Responses[400] = "Returned when validation fails or the cycle is already completed.";
                s.Responses[401] = "Returned when the request is made without a valid user token.";
                s.Responses[403] = "Returned when the caller lacks permission to complete this cycle.";
                s.Responses[404] = "Returned when the crop cycle is not found.";
            });
        }

        public override async Task HandleAsync(CompleteCropCycleCommand req, CancellationToken ct)
        {
            var response = await req.ExecuteAsync(ct: ct).ConfigureAwait(false);
            await MatchResultAsync(response, ct).ConfigureAwait(false);
        }
    }
}
