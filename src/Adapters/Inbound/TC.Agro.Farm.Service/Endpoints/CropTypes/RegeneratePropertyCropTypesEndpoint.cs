using TC.Agro.Farm.Application.UseCases.CropTypes.Regenerate;

namespace TC.Agro.Farm.Service.Endpoints.CropTypes
{
    public sealed class RegeneratePropertyCropTypesEndpoint : BaseApiEndpoint<RegeneratePropertyCropTypesCommand, RegeneratePropertyCropTypesResponse>
    {
        public override void Configure()
        {
            Post("properties/{propertyId:guid}/crop-types/regenerate");

            RequestBinder(new RequestBinder<RegeneratePropertyCropTypesCommand>(BindingSource.RouteValues));

            PostProcessor<LoggingCommandPostProcessorBehavior<RegeneratePropertyCropTypesCommand, RegeneratePropertyCropTypesResponse>>();
            this.AddCacheInvalidationIfNotTesting();
            Roles(AppConstants.AdminRole, AppConstants.ProducerRole);
            Description(
                x => x.Produces<RegeneratePropertyCropTypesResponse>(202)
                      .ProducesProblemDetails()
                      .Produces((int)HttpStatusCode.NotFound)
                      .Produces((int)HttpStatusCode.Forbidden)
                      .Produces((int)HttpStatusCode.Unauthorized));

            Summary(s =>
            {
                s.Summary = "Regenerate AI crop suggestions for a property.";
                s.Description = "Queues asynchronous regeneration of location-based crop suggestions for the target property.";
                s.ExampleRequest = new RegeneratePropertyCropTypesCommand(Guid.NewGuid());
                s.Responses[202] = "Returned when regeneration is successfully queued.";
                s.Responses[400] = "Returned when the request contains validation errors.";
                s.Responses[401] = "Returned when the request is made without a valid user token.";
                s.Responses[403] = "Returned when the caller lacks the required role.";
                s.Responses[404] = "Returned when no property is found with the given ID.";
            });
        }

        public override async Task HandleAsync(RegeneratePropertyCropTypesCommand req, CancellationToken ct)
        {
            var response = await req.ExecuteAsync(ct: ct).ConfigureAwait(false);

            if (response.IsSuccess)
            {
                await HttpContext!.Response.SendAsync(response.Value, (int)HttpStatusCode.Accepted, cancellation: ct).ConfigureAwait(false);
                return;
            }

            await MatchResultAsync(response, ct).ConfigureAwait(false);
        }
    }
}
