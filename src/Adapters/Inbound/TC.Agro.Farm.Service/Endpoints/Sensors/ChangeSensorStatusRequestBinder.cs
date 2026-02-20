using TC.Agro.Farm.Application.UseCases.Sensors.ChangeStatus;

namespace TC.Agro.Farm.Service.Endpoints.Sensors;

/// <summary>
/// Custom request binder for ChangeSensorStatusEndpoint.
/// Binds SensorId from route parameter and NewStatus/Reason from request body.
/// </summary>
internal sealed class ChangeSensorStatusRequestBinder : RequestBinder<ChangeSensorStatusCommand>
{
    public override async ValueTask<ChangeSensorStatusCommand> BindAsync(BinderContext ctx, CancellationToken ct)
    {
        // 1. Get SensorId from route parameter
        var sensorId = ctx.HttpContext.Request.RouteValues.TryGetValue("sensorId", out var routeValue)
            && Guid.TryParse(routeValue?.ToString(), out var id)
            ? id
            : Guid.Empty;

        // 2. Get NewStatus and Reason from JSON body
        var bodyDto = await ctx.HttpContext.Request.ReadFromJsonAsync<ChangeSensorStatusBodyDto>(ct);

        // 3. Combine into command
        return new ChangeSensorStatusCommand(
            SensorId: sensorId,
            NewStatus: bodyDto?.NewStatus ?? string.Empty,
            Reason: bodyDto?.Reason);
    }

    /// <summary>
    /// DTO for the request body (NewStatus and Reason only).
    /// </summary>
    private sealed record ChangeSensorStatusBodyDto(
        string NewStatus,
        string? Reason = null);
}
