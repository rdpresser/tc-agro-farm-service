using Ardalis.Result;
using FakeItEasy;
using Microsoft.Extensions.Logging;
using TC.Agro.Farm.Application.Abstractions.Ports;
using TC.Agro.Farm.Application.UseCases.Properties.GetById;
using TC.Agro.Farm.Tests.TestHelpers;
using TC.Agro.SharedKernel.Infrastructure.UserClaims;

namespace TC.Agro.Farm.Tests.Application.UseCases.Properties.GetById;

public sealed class GetPropertyByIdQueryHandlerTests
{
    private readonly IPropertyReadStore _readStore = A.Fake<IPropertyReadStore>();
    private readonly ILogger<GetPropertyByIdQueryHandler> _logger = A.Fake<ILogger<GetPropertyByIdQueryHandler>>();

    public GetPropertyByIdQueryHandlerTests()
    {
        FastEndpointsTestBootstrap.EnsureInitialized();
    }

    [Fact]
    public async Task ExecuteAsync_WhenPropertyDoesNotExist_ShouldReturnNotFound()
    {
        var userContext = TestUserContextFactory.CreateAdmin();
        var propertyId = Guid.NewGuid();

        A.CallTo(() => _readStore.GetByIdAsync(propertyId, A<CancellationToken>._))
            .Returns((GetPropertyByIdResponse?)null);

        var sut = CreateHandler(userContext);

        var result = await sut.ExecuteAsync(new GetPropertyByIdQuery { Id = propertyId }, CancellationToken.None);

        result.Status.ShouldBe(ResultStatus.NotFound);
    }

    [Fact]
    public async Task ExecuteAsync_WhenCallerIsNotAdminAndNotOwner_ShouldReturnUnauthorized()
    {
        var callerId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var userContext = TestUserContextFactory.CreateProducer(callerId);
        var response = CreateResponse(ownerId: ownerId);

        A.CallTo(() => _readStore.GetByIdAsync(response.Id, A<CancellationToken>._))
            .Returns(response);

        var sut = CreateHandler(userContext);

        var result = await sut.ExecuteAsync(new GetPropertyByIdQuery { Id = response.Id }, CancellationToken.None);

        result.Status.ShouldBe(ResultStatus.Unauthorized);
        result.Errors.ShouldContain(error => error.Contains("not authorized", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ExecuteAsync_WhenCallerIsOwner_ShouldReturnProperty()
    {
        var ownerId = Guid.NewGuid();
        var userContext = TestUserContextFactory.CreateProducer(ownerId);
        var response = CreateResponse(ownerId: ownerId);

        A.CallTo(() => _readStore.GetByIdAsync(response.Id, A<CancellationToken>._))
            .Returns(response);

        var sut = CreateHandler(userContext);

        var result = await sut.ExecuteAsync(new GetPropertyByIdQuery { Id = response.Id }, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Id.ShouldBe(response.Id);
        result.Value.OwnerId.ShouldBe(ownerId);
    }

    [Fact]
    public async Task ExecuteAsync_WhenCallerIsAdmin_ShouldReturnPropertyFromAnyOwner()
    {
        var userContext = TestUserContextFactory.CreateAdmin();
        var response = CreateResponse(ownerId: Guid.NewGuid());

        A.CallTo(() => _readStore.GetByIdAsync(response.Id, A<CancellationToken>._))
            .Returns(response);

        var sut = CreateHandler(userContext);

        var result = await sut.ExecuteAsync(new GetPropertyByIdQuery { Id = response.Id }, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Name.ShouldBe("Farm Delta");
    }

    private GetPropertyByIdQueryHandler CreateHandler(IUserContext userContext)
        => new(_readStore, userContext, _logger);

    private static GetPropertyByIdResponse CreateResponse(Guid ownerId)
        => new(
            Id: Guid.NewGuid(),
            Name: "Farm Delta",
            Address: "Rural Road",
            City: "Franca",
            State: "SP",
            Country: "Brazil",
            Latitude: -20.5397,
            Longitude: -47.4009,
            AreaHectares: 90,
            OwnerId: ownerId,
            IsActive: true,
            PlotCount: 2,
            CreatedAt: DateTimeOffset.UtcNow.AddDays(-8),
            UpdatedAt: DateTimeOffset.UtcNow.AddDays(-1));
}
