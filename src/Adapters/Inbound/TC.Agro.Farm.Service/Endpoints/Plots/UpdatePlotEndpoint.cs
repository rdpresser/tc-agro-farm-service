using TC.Agro.Farm.Application.UseCases.Plots.Update;

namespace TC.Agro.Farm.Service.Endpoints.Plots
{
    public sealed class UpdatePlotEndpoint : BaseApiEndpoint<UpdatePlotCommand, UpdatePlotResponse>
    {
        public override void Configure()
        {
            Put("plots/{plotId:guid}");
            PostProcessor<LoggingCommandPostProcessorBehavior<UpdatePlotCommand, UpdatePlotResponse>>();
            this.AddCacheInvalidationIfNotTesting();
            Roles(AppConstants.AdminRole, AppConstants.ProducerRole);
            Description(
                x => x.Produces<UpdatePlotResponse>(200)
                      .ProducesProblemDetails()
                      .Produces((int)HttpStatusCode.NotFound)
                      .Produces((int)HttpStatusCode.Forbidden)
                      .Produces((int)HttpStatusCode.Unauthorized));

            Summary(s =>
            {
                s.Summary = "Update an existing plot.";
                s.Description = "Updates plot information. The plotId in the route must match the plotId in the request body when provided.";
                s.ExampleRequest = new UpdatePlotCommand(
                    Guid.NewGuid(),
                    "North Plot Updated",
                    "Soy",
                    60.0,
                    -21.1775,
                    -47.8103,
                    "{\"type\":\"Polygon\",\"coordinates\":[[[-47.811,-21.178],[-47.808,-21.178],[-47.808,-21.175],[-47.811,-21.175],[-47.811,-21.178]]]}",
                    DateTimeOffset.UtcNow.AddMonths(-1),
                    DateTimeOffset.UtcNow.AddMonths(5),
                    Domain.ValueObjects.IrrigationType.CenterPivot,
                    "Updated notes about crop stage.");
                s.ResponseExamples[200] = new UpdatePlotResponse(
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    "North Plot Updated",
                    "Soy",
                    60.0,
                    -21.1775,
                    -47.8103,
                    DateTimeOffset.UtcNow.AddMonths(-1),
                    DateTimeOffset.UtcNow.AddMonths(5),
                    Domain.ValueObjects.IrrigationType.CenterPivot,
                    "Updated notes about crop stage.",
                    DateTimeOffset.UtcNow);
                s.Responses[200] = "Returned when the plot is successfully updated.";
                s.Responses[400] = "Returned when the request contains validation errors.";
                s.Responses[401] = "Returned when the request is made without a valid user token.";
                s.Responses[403] = "Returned when the caller lacks the required role to update plots.";
                s.Responses[404] = "Returned when no plot is found with the given ID.";
            });
        }

        public override async Task HandleAsync(UpdatePlotCommand req, CancellationToken ct)
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

