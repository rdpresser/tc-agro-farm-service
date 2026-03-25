using TC.Agro.Farm.Application.UseCases.Plots.Submit;

namespace TC.Agro.Farm.Service.Endpoints.Plots
{
    public sealed class SubmitPlotUpdateEndpoint : BaseApiEndpoint<SubmitPlotCommand, SubmitPlotResponse>
    {
        public override void Configure()
        {
            Put("plots/{plotId:guid}/submit");
            PostProcessor<LoggingCommandPostProcessorBehavior<SubmitPlotCommand, SubmitPlotResponse>>();
            this.AddCacheInvalidationIfNotTesting();
            Roles(AppConstants.AdminRole, AppConstants.ProducerRole);
            Description(
                x => x.Produces<SubmitPlotResponse>(200)
                      .ProducesProblemDetails()
                      .Produces((int)HttpStatusCode.NotFound)
                      .Produces((int)HttpStatusCode.Forbidden)
                      .Produces((int)HttpStatusCode.Unauthorized));

            Summary(s =>
            {
                s.Summary = "Submit plot updates through the orchestrated contract.";
                s.Description = "Updates a plot through the stable submit contract. The route plotId must match the body plotId when provided.";
                s.ExampleRequest = new SubmitPlotCommand
                {
                    PlotId = Guid.NewGuid(),
                    Name = "North Plot Updated",
                    CropType = "Soy",
                    AreaHectares = 60.0,
                    Latitude = -21.1775,
                    Longitude = -47.8103,
                    BoundaryGeoJson = "{\"type\":\"Polygon\",\"coordinates\":[[[-47.811,-21.178],[-47.808,-21.178],[-47.808,-21.175],[-47.811,-21.175],[-47.811,-21.178]]]}",
                    PlantingDate = DateTimeOffset.UtcNow.AddMonths(-1),
                    ExpectedHarvestDate = DateTimeOffset.UtcNow.AddMonths(5),
                    IrrigationType = Domain.ValueObjects.IrrigationType.CenterPivot,
                    AdditionalNotes = "Updated notes about crop stage.",
                    SelectedCropTypeSuggestionId = Guid.NewGuid()
                };
                s.Responses[200] = "Returned when the plot is successfully updated through the submit contract.";
                s.Responses[400] = "Returned when the request contains validation errors.";
                s.Responses[401] = "Returned when the request is made without a valid user token.";
                s.Responses[403] = "Returned when the caller lacks the required role to submit plot updates.";
                s.Responses[404] = "Returned when the plot or crop selection source is not found.";
            });
        }

        public override async Task HandleAsync(SubmitPlotCommand req, CancellationToken ct)
        {
            var routePlotId = Route<Guid>("plotId");

            if (routePlotId == Guid.Empty)
            {
                AddError(x => x.PlotId, "Plot Id is required in route.", "PlotId.RouteRequired");
                await Send.ErrorsAsync((int)HttpStatusCode.BadRequest, ct).ConfigureAwait(false);
                return;
            }

            if (req.PlotId != Guid.Empty && req.PlotId != routePlotId)
            {
                AddError(x => x.PlotId, "Route plotId must match request plotId.", "PlotId.Mismatch");
                await Send.ErrorsAsync((int)HttpStatusCode.BadRequest, ct).ConfigureAwait(false);
                return;
            }

            var command = req.PlotId == Guid.Empty ? req with { PlotId = routePlotId } : req;
            var response = await command.ExecuteAsync(ct: ct).ConfigureAwait(false);

            await MatchResultAsync(response, ct).ConfigureAwait(false);
        }
    }
}