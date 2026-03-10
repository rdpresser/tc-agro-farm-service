using TC.Agro.Farm.Application.UseCases.CropTypes.Create;

namespace TC.Agro.Farm.Service.Endpoints.CropTypes
{
    public sealed class CreateCropTypeEndpoint : BaseApiEndpoint<CreateCropTypeCommand, CreateCropTypeResponse>
    {
        public override void Configure()
        {
            Post("crop-types");
            PostProcessor<LoggingCommandPostProcessorBehavior<CreateCropTypeCommand, CreateCropTypeResponse>>();
            this.AddCacheInvalidationIfNotTesting();
            Roles(AppConstants.AdminRole, AppConstants.ProducerRole);
            Description(
                x => x.Produces<CreateCropTypeResponse>(201)
                      .ProducesProblemDetails()
                      .Produces((int)HttpStatusCode.Forbidden)
                      .Produces((int)HttpStatusCode.Unauthorized));

            Summary(s =>
            {
                s.Summary = "Create a manual crop type suggestion.";
                s.Description = "Creates a user-managed crop type suggestion for a property.";
                s.ExampleRequest = new CreateCropTypeCommand(
                    Guid.NewGuid(),
                    "Soy",
                    "September to November",
                    5,
                    "Center Pivot",
                    30,
                    35,
                    45,
                    "Manual override based on local agronomist recommendation.");
                s.Responses[201] = "Returned when the crop type suggestion is successfully created.";
                s.Responses[400] = "Returned when the request contains validation errors.";
                s.Responses[401] = "Returned when the request is made without a valid user token.";
                s.Responses[403] = "Returned when the caller lacks the required role.";
            });
        }

        public override async Task HandleAsync(CreateCropTypeCommand req, CancellationToken ct)
        {
            var response = await req.ExecuteAsync(ct: ct).ConfigureAwait(false);

            if (response.IsSuccess)
            {
                const string location = "/api/crop-types/{id}";
                var routeValues = new { id = response.Value.Id };
                await Send.CreatedAtAsync(location, routeValues, response.Value, cancellation: ct).ConfigureAwait(false);
                return;
            }

            await MatchResultAsync(response, ct).ConfigureAwait(false);
        }
    }
}
