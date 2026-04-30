using TC.Agro.Farm.Application.Abstractions;
using TC.Agro.Farm.Tests.Service.Endpoints;

namespace TC.Agro.Farm.Tests.Service.Endpoints.CropTypes;

public sealed class PromoteCropTypeSuggestionEndpointConfigurationTests
{
    [Fact]
    public void PromoteCropTypeSuggestionEndpoint_ShouldExposeRouteAndAllowedRoles()
    {
        var source = EndpointSourceAssertions.LoadEndpointSource("CropTypes", "PromoteCropTypeSuggestionEndpoint.cs");

        EndpointSourceAssertions.AssertContains(
            source,
            "Post(\"crop-types/suggestions/{suggestionId:guid}/promote\")",
            $"Roles({nameof(AppConstants)}.{nameof(AppConstants.AdminRole)}, {nameof(AppConstants)}.{nameof(AppConstants.ProducerRole)})");
    }
}