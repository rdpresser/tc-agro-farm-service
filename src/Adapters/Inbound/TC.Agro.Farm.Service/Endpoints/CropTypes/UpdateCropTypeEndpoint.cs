using TC.Agro.Farm.Application.UseCases.CropTypes.Update;

namespace TC.Agro.Farm.Service.Endpoints.CropTypes
{
    public sealed class UpdateCropTypeEndpoint : BaseApiEndpoint<UpdateCropTypeCommand, UpdateCropTypeResponse>
    {
        public override void Configure()
        {
            Put("crop-types/{cropTypeId:guid}");
            PostProcessor<LoggingCommandPostProcessorBehavior<UpdateCropTypeCommand, UpdateCropTypeResponse>>();
            this.AddCacheInvalidationIfNotTesting();
            Roles(AppConstants.AdminRole, AppConstants.ProducerRole);
            Description(
                x => x.Produces<UpdateCropTypeResponse>(200)
                      .ProducesProblemDetails()
                      .Produces((int)HttpStatusCode.NotFound)
                      .Produces((int)HttpStatusCode.Forbidden)
                      .Produces((int)HttpStatusCode.Unauthorized));

            Summary(s =>
            {
                s.Summary = "Update a crop type suggestion.";
                s.Description = "Updates a crop type suggestion. The route cropTypeId must match body cropTypeId when provided.";
                s.ExampleRequest = new UpdateCropTypeCommand(
                    Guid.NewGuid(),
                    "Corn",
                    "October to December",
                    5,
                    "Center Pivot",
                    28,
                    36,
                    42,
                    "Adjusted thresholds after field inspection.");
                s.Responses[200] = "Returned when the crop type suggestion is successfully updated.";
                s.Responses[400] = "Returned when the request contains validation errors.";
                s.Responses[401] = "Returned when the request is made without a valid user token.";
                s.Responses[403] = "Returned when the caller lacks the required role.";
                s.Responses[404] = "Returned when no crop type suggestion is found with the given ID.";
            });
        }

        public override async Task HandleAsync(UpdateCropTypeCommand req, CancellationToken ct)
        {
            var routeCropTypeId = Route<Guid>("cropTypeId");

            if (routeCropTypeId == Guid.Empty)
            {
                AddError(x => x.CropTypeId, "Crop type Id is required in route.", "CropTypeId.RouteRequired");
                await Send.ErrorsAsync((int)HttpStatusCode.BadRequest, ct).ConfigureAwait(false);
                return;
            }

            if (req.CropTypeId != Guid.Empty && req.CropTypeId != routeCropTypeId)
            {
                AddError(x => x.CropTypeId, "Route cropTypeId must match request cropTypeId.", "CropTypeId.Mismatch");
                await Send.ErrorsAsync((int)HttpStatusCode.BadRequest, ct).ConfigureAwait(false);
                return;
            }

            var command = req.CropTypeId == Guid.Empty ? req with { CropTypeId = routeCropTypeId } : req;

            var response = await command.ExecuteAsync(ct: ct).ConfigureAwait(false);
            await MatchResultAsync(response, ct).ConfigureAwait(false);
        }
    }
}
