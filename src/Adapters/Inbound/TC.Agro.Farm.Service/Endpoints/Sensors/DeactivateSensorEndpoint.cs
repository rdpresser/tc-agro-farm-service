using TC.Agro.Farm.Application.UseCases.Sensors.Deactivate;

namespace TC.Agro.Farm.Service.Endpoints.Sensors
{
    /// <summary>
    /// Endpoint: DELETE /api/sensors/{sensorId}
    /// 
    /// Deactivates (soft-deletes) a sensor by setting IsActive = false.
    /// This is a logical deletion - the sensor record is preserved in the database
    /// but marked as inactive and excluded from active queries.
    /// 
    /// This is different from changing operational status (Active, Maintenance, Faulty, Inactive).
    /// Deactivation is intended to be permanent removal from active use.
    /// </summary>
    public sealed class DeactivateSensorEndpoint : BaseApiEndpoint<DeactivateSensorCommand, DeactivateSensorResponse>
    {
        public override void Configure()
        {
            Delete("sensors/{sensorId}");

            PostProcessor<LoggingCommandPostProcessorBehavior<DeactivateSensorCommand, DeactivateSensorResponse>>();
            PostProcessor<CacheInvalidationPostProcessorBehavior<DeactivateSensorCommand, DeactivateSensorResponse>>();

            Roles(AppConstants.AdminRole, AppConstants.ProducerRole);
            Description(
                x => x.Produces<DeactivateSensorResponse>(200)
                      .ProducesProblemDetails()
                      .Produces((int)HttpStatusCode.BadRequest)
                      .Produces((int)HttpStatusCode.NotFound)
                      .Produces((int)HttpStatusCode.Forbidden)
                      .Produces((int)HttpStatusCode.Unauthorized)
                      .Produces((int)HttpStatusCode.Conflict));

            Summary(s =>
            {
                s.Summary = "Deactivate (soft-delete) a sensor.";
                s.Description = "Performs a soft-delete by setting the sensor's IsActive flag to false. " +
                               "The sensor record is preserved in the database but marked as inactive. " +
                               "This operation is intended for permanent removal from active use. " +
                               "Different from operational status changes (Active, Maintenance, Faulty, Inactive). " +
                               "Emits events to notify other services to stop data collection and trigger cleanup processes. " +
                               "The sensorId is provided in the URL path, while an optional Reason can be provided in the request body.";
                s.Params["sensorId"] = "The unique identifier of the sensor to deactivate (from route)";
                s.ExampleRequest = new { Reason = "End of lifecycle" };
                s.ResponseExamples[200] = new DeactivateSensorResponse(
                    Guid.NewGuid(),
                    DateTimeOffset.UtcNow);
                s.Responses[200] = "Returned when the sensor is successfully deactivated.";
                s.Responses[400] = "Returned when the request is invalid.";
                s.Responses[401] = "Returned when the request is made without a valid user token.";
                s.Responses[403] = "Returned when the caller lacks the required role.";
                s.Responses[404] = "Returned when the sensor is not found.";
                s.Responses[409] = "Returned when the sensor is already deactivated.";
            });
        }

        public override async Task HandleAsync(DeactivateSensorCommand req, CancellationToken ct)
        {
            var response = await req.ExecuteAsync(ct: ct).ConfigureAwait(false);
            await MatchResultAsync(response, ct).ConfigureAwait(false);
        }
    }
}
