using TC.Agro.Farm.Application.Abstractions;
using TC.Agro.Farm.Tests.Service.Endpoints;

namespace TC.Agro.Farm.Tests.Service.Endpoints.CropTypes;

public sealed class CropTypeEndpointConfigurationTests
{
    [Fact]
    public void ListCropTypeOptionsEndpoint_ShouldExposeRouteAndAllowedRoles()
    {
        var source = EndpointSourceAssertions.LoadEndpointSource("CropTypes", "ListCropTypeOptionsEndpoint.cs");

        EndpointSourceAssertions.AssertContains(
            source,
            "Get(\"crop-types/options\")",
            $"Roles({nameof(AppConstants)}.{nameof(AppConstants.UserRole)}, {nameof(AppConstants)}.{nameof(AppConstants.AdminRole)}, {nameof(AppConstants)}.{nameof(AppConstants.ProducerRole)})");
    }
}
