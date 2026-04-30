using TC.Agro.Farm.Application.UseCases.Plots.Submit;

namespace TC.Agro.Farm.Service.Endpoints.Plots
{
    public sealed class SubmitPlotEndpoint : BaseApiEndpoint<SubmitPlotCommand, SubmitPlotResponse>
    {
        public override void Configure()
        {
            Post("plots/submit");
            PostProcessor<LoggingCommandPostProcessorBehavior<SubmitPlotCommand, SubmitPlotResponse>>();
            this.AddCacheInvalidationIfNotTesting();
            Roles(AppConstants.AdminRole, AppConstants.ProducerRole);
            Description(
                x => x.Produces<SubmitPlotResponse>(201)
                      .ProducesProblemDetails()
                      .Produces((int)HttpStatusCode.NotFound)
                      .Produces((int)HttpStatusCode.Forbidden)
                      .Produces((int)HttpStatusCode.Unauthorized));

            Summary(s =>
            {
                s.Summary = "Submit a new plot through the orchestrated contract.";
                s.Description = "Creates a plot through the stable submit contract. When only a crop suggestion is informed, the backend promotes it to catalog idempotently during submit.";
                s.ExampleRequest = new SubmitPlotCommand
                {
                    PropertyId = Guid.NewGuid(),
                    OwnerId = Guid.NewGuid(),
                    Name = "North Plot",
                    CropType = "Soy",
                    AreaHectares = 50.0,
                    Latitude = -21.1775,
                    Longitude = -47.8103,
                    BoundaryGeoJson = "{\"type\":\"Polygon\",\"coordinates\":[[[-47.811,-21.178],[-47.809,-21.178],[-47.809,-21.176],[-47.811,-21.176],[-47.811,-21.178]]]}",
                    PlantingDate = DateTimeOffset.UtcNow.AddMonths(1),
                    ExpectedHarvestDate = DateTimeOffset.UtcNow.AddMonths(6),
                    IrrigationType = Domain.ValueObjects.IrrigationType.CenterPivot,
                    AdditionalNotes = "Optional notes about soil or irrigation.",
                    SelectedCropTypeSuggestionId = Guid.NewGuid()
                };
                s.Responses[201] = "Returned when the plot is successfully created through the submit contract.";
                s.Responses[400] = "Returned when the request contains validation errors.";
                s.Responses[401] = "Returned when the request is made without a valid user token.";
                s.Responses[403] = "Returned when the caller lacks the required role to submit plots.";
                s.Responses[404] = "Returned when the property or crop selection source is not found.";
            });
        }

        public override async Task HandleAsync(SubmitPlotCommand req, CancellationToken ct)
        {
            var response = await req.ExecuteAsync(ct: ct).ConfigureAwait(false);

            if (response.IsSuccess)
            {
                string location = $"/api/plots/{response.Value.Id}";
                object routeValues = new { id = response.Value.Id };
                await Send.CreatedAtAsync(location, routeValues, response.Value, cancellation: ct).ConfigureAwait(false);
                return;
            }

            await MatchResultAsync(response, ct).ConfigureAwait(false);
        }
    }
}