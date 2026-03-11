using TC.Agro.Farm.Application.UseCases.CropTypes.Delete;

namespace TC.Agro.Farm.Service.Endpoints.CropTypes
{
    public sealed class DeleteCropTypeEndpoint : BaseApiEndpoint<DeleteCropTypeCommand, DeleteCropTypeResponse>
    {
        public override void Configure()
        {
            Delete("crop-types/{cropTypeId:guid}");

            RequestBinder(new RequestBinder<DeleteCropTypeCommand>(BindingSource.RouteValues));

            PostProcessor<LoggingCommandPostProcessorBehavior<DeleteCropTypeCommand, DeleteCropTypeResponse>>();
            this.AddCacheInvalidationIfNotTesting();
            Roles(AppConstants.AdminRole, AppConstants.ProducerRole);
            Description(
                x => x.Produces<DeleteCropTypeResponse>(200)
                      .ProducesProblemDetails()
                      .Produces((int)HttpStatusCode.NotFound)
                      .Produces((int)HttpStatusCode.Forbidden)
                      .Produces((int)HttpStatusCode.Unauthorized));

            Summary(s =>
            {
                s.Summary = "Deactivate a crop type catalog entry.";
                s.Description = "Performs a logical deletion by deactivating a crop type catalog entry.";
                s.ExampleRequest = new DeleteCropTypeCommand(Guid.NewGuid());
                s.Responses[200] = "Returned when the crop type catalog entry is successfully deactivated.";
                s.Responses[400] = "Returned when the request is invalid.";
                s.Responses[401] = "Returned when the request is made without a valid user token.";
                s.Responses[403] = "Returned when the caller lacks the required role.";
                s.Responses[404] = "Returned when no crop type catalog entry is found with the given ID.";
            });
        }

        public override async Task HandleAsync(DeleteCropTypeCommand req, CancellationToken ct)
        {
            var response = await req.ExecuteAsync(ct: ct).ConfigureAwait(false);
            await MatchResultAsync(response, ct).ConfigureAwait(false);
        }
    }
}
