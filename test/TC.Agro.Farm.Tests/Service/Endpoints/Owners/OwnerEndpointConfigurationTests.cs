using TC.Agro.Farm.Application.Abstractions;
using TC.Agro.Farm.Tests.Service.Endpoints;

namespace TC.Agro.Farm.Tests.Service.Endpoints.Owners;

public sealed class OwnerEndpointConfigurationTests
{
    [Fact]
    public void ListOwnersEndpoint_ShouldExposeRouteAndAllowedRoles()
    {
        var source = EndpointSourceAssertions.LoadEndpointSource("Owners", "ListOwnersEndpoint.cs");

        EndpointSourceAssertions.AssertContains(
            source,
            "Get(\"owners\")",
            $"Roles({nameof(AppConstants)}.{nameof(AppConstants.UserRole)}, {nameof(AppConstants)}.{nameof(AppConstants.AdminRole)}, {nameof(AppConstants)}.{nameof(AppConstants.ProducerRole)})");
    }
}
