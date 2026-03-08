using TC.Agro.Farm.Application.Abstractions;
using TC.Agro.Farm.Tests.Service.Endpoints;

namespace TC.Agro.Farm.Tests.Service.Endpoints.Sensors;

public sealed class SensorEndpointConfigurationTests
{
    [Fact]
    public void ChangeSensorStatusEndpoint_ShouldExposeRouteAndAllowedRoles()
    {
        var source = EndpointSourceAssertions.LoadEndpointSource("Sensors", "ChangeSensorStatusEndpoint.cs");

        EndpointSourceAssertions.AssertContains(
            source,
            "Put(\"sensors/{sensorId}/status-change\")",
            $"Roles({nameof(AppConstants)}.{nameof(AppConstants.AdminRole)}, {nameof(AppConstants)}.{nameof(AppConstants.ProducerRole)})");
    }

    [Fact]
    public void CreateSensorEndpoint_ShouldExposeRouteAndAllowedRoles()
    {
        var source = EndpointSourceAssertions.LoadEndpointSource("Sensors", "CreateSensorEndpoint.cs");

        EndpointSourceAssertions.AssertContains(
            source,
            "Post(\"sensors\")",
            $"Roles({nameof(AppConstants)}.{nameof(AppConstants.AdminRole)}, {nameof(AppConstants)}.{nameof(AppConstants.ProducerRole)})");
    }

    [Fact]
    public void DeactivateSensorEndpoint_ShouldExposeRouteAndAllowedRoles()
    {
        var source = EndpointSourceAssertions.LoadEndpointSource("Sensors", "DeactivateSensorEndpoint.cs");

        EndpointSourceAssertions.AssertContains(
            source,
            "Delete(\"sensors/{sensorId}\")",
            $"Roles({nameof(AppConstants)}.{nameof(AppConstants.AdminRole)}, {nameof(AppConstants)}.{nameof(AppConstants.ProducerRole)})");
    }

    [Fact]
    public void GetSensorByIdEndpoint_ShouldExposeRouteAndAllowedRoles()
    {
        var source = EndpointSourceAssertions.LoadEndpointSource("Sensors", "GetSensorByIdEndpoint.cs");

        EndpointSourceAssertions.AssertContains(
            source,
            "Get(\"sensors/{id:guid}\")",
            $"Roles({nameof(AppConstants)}.{nameof(AppConstants.UserRole)}, {nameof(AppConstants)}.{nameof(AppConstants.AdminRole)}, {nameof(AppConstants)}.{nameof(AppConstants.ProducerRole)})");
    }

    [Fact]
    public void ListSensorsEndpoint_ShouldExposeRouteAndAllowedRoles()
    {
        var source = EndpointSourceAssertions.LoadEndpointSource("Sensors", "ListSensorsEndpoint.cs");

        EndpointSourceAssertions.AssertContains(
            source,
            "Get(\"sensors\")",
            $"Roles({nameof(AppConstants)}.{nameof(AppConstants.UserRole)}, {nameof(AppConstants)}.{nameof(AppConstants.AdminRole)}, {nameof(AppConstants)}.{nameof(AppConstants.ProducerRole)})");
    }

    [Fact]
    public void ListSensorsFromPlotEndpoint_ShouldExposeRouteAndAllowedRoles()
    {
        var source = EndpointSourceAssertions.LoadEndpointSource("Sensors", "ListSensorsFromPlotEndpoint.cs");

        EndpointSourceAssertions.AssertContains(
            source,
            "Get(\"plots/{id:guid}/sensors\")",
            $"Roles({nameof(AppConstants)}.{nameof(AppConstants.UserRole)}, {nameof(AppConstants)}.{nameof(AppConstants.AdminRole)}, {nameof(AppConstants)}.{nameof(AppConstants.ProducerRole)})");
    }
}
