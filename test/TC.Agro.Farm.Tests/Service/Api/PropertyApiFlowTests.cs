using System.Net;
using System.Net.Http.Json;
using TC.Agro.Farm.Application.UseCases.Properties.Create;
using TC.Agro.Farm.Tests.TestHelpers.Api;

namespace TC.Agro.Farm.Tests.Service.Api;

public sealed class PropertyApiFlowTests : IClassFixture<FarmApiWebApplicationFactory>
{
    private readonly FarmApiWebApplicationFactory _factory;

    public PropertyApiFlowTests(FarmApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CreateProperty_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        var ct = TestContext.Current.CancellationToken;

        await _factory.ResetDatabaseAsync();

        using var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/properties", BuildValidRequest(), ct);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateProperty_WithProducerRole_ShouldReturnCreatedWithOwnerFromToken()
    {
        var ct = TestContext.Current.CancellationToken;

        await _factory.ResetDatabaseAsync();

        var producerId = Guid.NewGuid();
        await _factory.SeedOwnerAsync(producerId);

        using var client = _factory.CreateAuthenticatedClient("Producer", producerId);

        var response = await client.PostAsJsonAsync("/api/properties", BuildValidRequest(), ct);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        var payload = await response.Content.ReadFromJsonAsync<CreatePropertyResponse>(cancellationToken: ct);
        payload.ShouldNotBeNull();
        payload!.OwnerId.ShouldBe(producerId);
        payload.Name.ShouldBe("API Property");
    }

    [Fact]
    public async Task CreateProperty_WithUserRole_ShouldReturnForbidden()
    {
        var ct = TestContext.Current.CancellationToken;

        await _factory.ResetDatabaseAsync();

        using var client = _factory.CreateAuthenticatedClient("User");

        var response = await client.PostAsJsonAsync("/api/properties", BuildValidRequest(), ct);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CreateProperty_WithInvalidArea_ShouldReturnBadRequest()
    {
        var ct = TestContext.Current.CancellationToken;

        await _factory.ResetDatabaseAsync();

        var producerId = Guid.NewGuid();
        await _factory.SeedOwnerAsync(producerId);

        using var client = _factory.CreateAuthenticatedClient("Producer", producerId);

        var response = await client.PostAsJsonAsync("/api/properties", new
        {
            Name = "API Property",
            Address = "Rural Road",
            City = "Ribeirao Preto",
            State = "SP",
            Country = "Brazil",
            AreaHectares = 0,
            Latitude = -21.1775,
            Longitude = -47.8103
        }, ct);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        var body = await response.Content.ReadAsStringAsync(ct);
        body.ShouldContain("errors", Case.Insensitive);
    }

    private static object BuildValidRequest() => new
    {
        Name = "API Property",
        Address = "Rural Road",
        City = "Ribeirao Preto",
        State = "SP",
        Country = "Brazil",
        AreaHectares = 85.4,
        Latitude = -21.1775,
        Longitude = -47.8103
    };
}
