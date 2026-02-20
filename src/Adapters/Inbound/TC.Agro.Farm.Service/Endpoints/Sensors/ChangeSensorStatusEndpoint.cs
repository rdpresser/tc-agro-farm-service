using TC.Agro.Farm.Application.UseCases.Sensors.ChangeStatus;

namespace TC.Agro.Farm.Service.Endpoints.Sensors
{
    /// <summary>
    /// Endpoint: PUT /api/sensors/{sensorId}/status-change
    /// 
    /// Changes the operational status of a sensor.
    /// Valid transitions: Active ↔ Maintenance, Active → Inactive, Active → Faulty
    /// 
    /// This is different from Deactivate which permanently removes (soft-delete) the sensor.
    /// Status changes are reversible operational state transitions.
    /// </summary>
    public sealed class ChangeSensorStatusEndpoint : BaseApiEndpoint<ChangeSensorStatusCommand, ChangeSensorStatusResponse>
    {
        public override void Configure()
        {
            Put("sensors/{sensorId}/status-change");
            
            // Explicit binding: sensorId from route, NewStatus/Reason from body
            RequestBinder(new ChangeSensorStatusRequestBinder());
            
            PostProcessor<LoggingCommandPostProcessorBehavior<ChangeSensorStatusCommand, ChangeSensorStatusResponse>>();
            PostProcessor<CacheInvalidationPostProcessorBehavior<ChangeSensorStatusCommand, ChangeSensorStatusResponse>>();

            Roles(AppConstants.AdminRole, AppConstants.ProducerRole);
            Description(
                x => x.Produces<ChangeSensorStatusResponse>(200)
                      .ProducesProblemDetails()
                      .Produces((int)HttpStatusCode.BadRequest)
                      .Produces((int)HttpStatusCode.NotFound)
                      .Produces((int)HttpStatusCode.Forbidden)
                      .Produces((int)HttpStatusCode.Unauthorized)
                      .Produces((int)HttpStatusCode.Conflict));

            Summary(s =>
            {
                s.Summary = "Change sensor operational status.";
                s.Description = "Transitions a sensor between operational states: Active, Maintenance, Faulty, Inactive. " +
                               "Different from deactivation (soft delete). Status changes are reversible and emit events to other services. " +
                               "The sensorId is provided in the URL path, while NewStatus and Reason are in the request body.";
                s.Params["sensorId"] = "The unique identifier of the sensor (from route)";
                s.ExampleRequest = new { NewStatus = "Maintenance", Reason = "Preventive maintenance scheduled" };
                s.ResponseExamples[200] = new ChangeSensorStatusResponse(
                    Guid.NewGuid(),
                    "Active",
                    "Maintenance",
                    DateTimeOffset.UtcNow);
                s.Responses[200] = "Returned when the sensor status is successfully changed.";
                s.Responses[400] = "Returned when status transition is invalid or validation fails.";
                s.Responses[401] = "Returned when the request is made without a valid user token.";
                s.Responses[403] = "Returned when the caller lacks the required role.";
                s.Responses[404] = "Returned when the sensor is not found.";
                s.Responses[409] = "Returned when the sensor is deactivated or sensor state conflict.";
            });
        }

        public override async Task HandleAsync(ChangeSensorStatusCommand req, CancellationToken ct)
        {
            var response = await req.ExecuteAsync(ct: ct).ConfigureAwait(false);
            await MatchResultAsync(response, ct).ConfigureAwait(false);
        }
    }
}
