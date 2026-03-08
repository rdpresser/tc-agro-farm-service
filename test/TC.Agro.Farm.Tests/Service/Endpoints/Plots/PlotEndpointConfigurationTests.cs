using TC.Agro.Farm.Application.Abstractions;
using TC.Agro.Farm.Tests.Service.Endpoints;

namespace TC.Agro.Farm.Tests.Service.Endpoints.Plots;

public sealed class PlotEndpointConfigurationTests
{
    [Fact]
    public void CreatePlotEndpoint_ShouldExposeRouteAndAllowedRoles()
    {
        var source = EndpointSourceAssertions.LoadEndpointSource("Plots", "CreatePlotEndpoint.cs");

        EndpointSourceAssertions.AssertContains(
            source,
            "Post(\"plots\")",
            $"Roles({nameof(AppConstants)}.{nameof(AppConstants.AdminRole)}, {nameof(AppConstants)}.{nameof(AppConstants.ProducerRole)})");
    }

    [Fact]
    public void GetPlotByIdEndpoint_ShouldExposeRouteAndAllowedRoles()
    {
        var source = EndpointSourceAssertions.LoadEndpointSource("Plots", "GetPlotByIdEndpoint.cs");

        EndpointSourceAssertions.AssertContains(
            source,
            "Get(\"plots/{id:guid}\")",
            $"Roles({nameof(AppConstants)}.{nameof(AppConstants.UserRole)}, {nameof(AppConstants)}.{nameof(AppConstants.AdminRole)}, {nameof(AppConstants)}.{nameof(AppConstants.ProducerRole)})");
    }

    [Fact]
    public void ListPlotsEndpoint_ShouldExposeRouteAndAllowedRoles()
    {
        var source = EndpointSourceAssertions.LoadEndpointSource("Plots", "ListPlotsEndpoint.cs");

        EndpointSourceAssertions.AssertContains(
            source,
            "Get(\"plots\")",
            $"Roles({nameof(AppConstants)}.{nameof(AppConstants.UserRole)}, {nameof(AppConstants)}.{nameof(AppConstants.AdminRole)}, {nameof(AppConstants)}.{nameof(AppConstants.ProducerRole)})");
    }

    [Fact]
    public void ListPlotsFromPropertyEndpoint_ShouldExposeRouteAndAllowedRoles()
    {
        var source = EndpointSourceAssertions.LoadEndpointSource("Plots", "ListPlotsFromPropertyEndpoint.cs");

        EndpointSourceAssertions.AssertContains(
            source,
            "Get(\"properties/{id:guid}/plots\")",
            $"Roles({nameof(AppConstants)}.{nameof(AppConstants.UserRole)}, {nameof(AppConstants)}.{nameof(AppConstants.AdminRole)}, {nameof(AppConstants)}.{nameof(AppConstants.ProducerRole)})");
    }

    [Fact]
    public void UpdatePlotEndpoint_ShouldExposeRouteAndAllowedRoles()
    {
        var source = EndpointSourceAssertions.LoadEndpointSource("Plots", "UpdatePlotEndpoint.cs");

        EndpointSourceAssertions.AssertContains(
            source,
            "Put(\"plots/{plotId:guid}\")",
            $"Roles({nameof(AppConstants)}.{nameof(AppConstants.AdminRole)}, {nameof(AppConstants)}.{nameof(AppConstants.ProducerRole)})");
    }
}
