using TC.Agro.Farm.Application.Abstractions;
using TC.Agro.Farm.Tests.Service.Endpoints;

namespace TC.Agro.Farm.Tests.Service.Endpoints.CropCycles;

public sealed class CropCycleEndpointConfigurationTests
{
    [Fact]
    public void StartCropCycleEndpoint_ShouldExposeRouteAndAllowedRoles()
    {
        var source = EndpointSourceAssertions.LoadEndpointSource("CropCycles", "StartCropCycleEndpoint.cs");

        EndpointSourceAssertions.AssertContains(
            source,
            "Post(\"crop-cycles\")",
            $"Roles({nameof(AppConstants)}.{nameof(AppConstants.AdminRole)}, {nameof(AppConstants)}.{nameof(AppConstants.ProducerRole)})");
    }

    [Fact]
    public void TransitionCropCycleEndpoint_ShouldExposeRouteAndAllowedRoles()
    {
        var source = EndpointSourceAssertions.LoadEndpointSource("CropCycles", "TransitionCropCycleEndpoint.cs");

        EndpointSourceAssertions.AssertContains(
            source,
            "Put(\"crop-cycles/{cropCycleId}/transition\")",
            $"Roles({nameof(AppConstants)}.{nameof(AppConstants.AdminRole)}, {nameof(AppConstants)}.{nameof(AppConstants.ProducerRole)})");
    }

    [Fact]
    public void CompleteCropCycleEndpoint_ShouldExposeRouteAndAllowedRoles()
    {
        var source = EndpointSourceAssertions.LoadEndpointSource("CropCycles", "CompleteCropCycleEndpoint.cs");

        EndpointSourceAssertions.AssertContains(
            source,
            "Post(\"crop-cycles/{cropCycleId}/complete\")",
            $"Roles({nameof(AppConstants)}.{nameof(AppConstants.AdminRole)}, {nameof(AppConstants)}.{nameof(AppConstants.ProducerRole)})");
    }
}
