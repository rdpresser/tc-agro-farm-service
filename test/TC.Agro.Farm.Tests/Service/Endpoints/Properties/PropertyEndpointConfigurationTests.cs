using TC.Agro.Farm.Application.Abstractions;
using TC.Agro.Farm.Tests.Service.Endpoints;

namespace TC.Agro.Farm.Tests.Service.Endpoints.Properties;

public sealed class PropertyEndpointConfigurationTests
{
    [Fact]
    public void CreatePropertyEndpoint_ShouldExposeRouteAndAllowedRoles()
    {
        var source = EndpointSourceAssertions.LoadEndpointSource("Properties", "CreatePropertyEndpoint.cs");

        EndpointSourceAssertions.AssertContains(
            source,
            "Post(\"properties\")",
            $"Roles({nameof(AppConstants)}.{nameof(AppConstants.AdminRole)}, {nameof(AppConstants)}.{nameof(AppConstants.ProducerRole)})");
    }

    [Fact]
    public void GetPropertyByIdEndpoint_ShouldExposeRouteAndAllowedRoles()
    {
        var source = EndpointSourceAssertions.LoadEndpointSource("Properties", "GetPropertyByIdEndpoint.cs");

        EndpointSourceAssertions.AssertContains(
            source,
            "Get(\"properties/{id:guid}\")",
            $"Roles({nameof(AppConstants)}.{nameof(AppConstants.UserRole)}, {nameof(AppConstants)}.{nameof(AppConstants.AdminRole)}, {nameof(AppConstants)}.{nameof(AppConstants.ProducerRole)})");
    }

    [Fact]
    public void ListPropertiesEndpoint_ShouldExposeRouteAndAllowedRoles()
    {
        var source = EndpointSourceAssertions.LoadEndpointSource("Properties", "ListPropertiesEndpoint.cs");

        EndpointSourceAssertions.AssertContains(
            source,
            "Get(\"properties\")",
            $"Roles({nameof(AppConstants)}.{nameof(AppConstants.UserRole)}, {nameof(AppConstants)}.{nameof(AppConstants.AdminRole)}, {nameof(AppConstants)}.{nameof(AppConstants.ProducerRole)})");
    }

    [Fact]
    public void UpdatePropertyEndpoint_ShouldExposeRouteAndAllowedRoles()
    {
        var source = EndpointSourceAssertions.LoadEndpointSource("Properties", "UpdatePropertyEndpoint.cs");

        EndpointSourceAssertions.AssertContains(
            source,
            "Put(\"properties/{id:guid}\")",
            $"Roles({nameof(AppConstants)}.{nameof(AppConstants.AdminRole)}, {nameof(AppConstants)}.{nameof(AppConstants.ProducerRole)})");
    }
}
