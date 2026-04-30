using TC.Agro.Farm.Application.UseCases.CropTypes.Promote;

namespace TC.Agro.Farm.Service.Endpoints.CropTypes
{
    public sealed class PromoteCropTypeSuggestionEndpoint : BaseApiEndpoint<PromoteCropTypeSuggestionCommand, PromoteCropTypeSuggestionResponse>
    {
        public override void Configure()
        {
            Post("crop-types/suggestions/{suggestionId:guid}/promote");
            PostProcessor<LoggingCommandPostProcessorBehavior<PromoteCropTypeSuggestionCommand, PromoteCropTypeSuggestionResponse>>();
            this.AddCacheInvalidationIfNotTesting();
            Roles(AppConstants.AdminRole, AppConstants.ProducerRole);
            Description(
                x => x.Produces<PromoteCropTypeSuggestionResponse>(200)
                      .ProducesProblemDetails()
                      .Produces((int)HttpStatusCode.NotFound)
                      .Produces((int)HttpStatusCode.Forbidden)
                      .Produces((int)HttpStatusCode.Unauthorized));

            Summary(s =>
            {
                s.Summary = "Promote a crop type suggestion to catalog.";
                s.Description = "Promotes a single crop type suggestion to an official tenant-scoped catalog entry. The operation is idempotent and marks the source suggestion as stale.";
                s.ExampleRequest = new PromoteCropTypeSuggestionCommand(Guid.NewGuid());
                s.Responses[200] = "Returned when the suggestion is promoted or an existing catalog entry is reused.";
                s.Responses[400] = "Returned when the request contains validation errors.";
                s.Responses[401] = "Returned when the request is made without a valid user token.";
                s.Responses[403] = "Returned when the caller lacks the required role or ownership.";
                s.Responses[404] = "Returned when the suggestion does not exist.";
            });
        }

        public override async Task HandleAsync(PromoteCropTypeSuggestionCommand req, CancellationToken ct)
        {
            var routeSuggestionId = Route<Guid>("suggestionId");

            if (routeSuggestionId == Guid.Empty)
            {
                AddError(x => x.SuggestionId, "Suggestion Id is required in route.", "SuggestionId.RouteRequired");
                await Send.ErrorsAsync((int)HttpStatusCode.BadRequest, ct).ConfigureAwait(false);
                return;
            }

            if (req.SuggestionId != Guid.Empty && req.SuggestionId != routeSuggestionId)
            {
                AddError(x => x.SuggestionId, "Route suggestionId must match request suggestionId.", "SuggestionId.Mismatch");
                await Send.ErrorsAsync((int)HttpStatusCode.BadRequest, ct).ConfigureAwait(false);
                return;
            }

            var command = req.SuggestionId == Guid.Empty
                ? req with { SuggestionId = routeSuggestionId }
                : req;

            var response = await command.ExecuteAsync(ct: ct).ConfigureAwait(false);
            await MatchResultAsync(response, ct).ConfigureAwait(false);
        }
    }
}