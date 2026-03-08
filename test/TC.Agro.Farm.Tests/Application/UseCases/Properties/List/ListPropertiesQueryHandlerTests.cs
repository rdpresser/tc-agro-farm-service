using FakeItEasy;
using Microsoft.Extensions.Logging;
using TC.Agro.Farm.Application.Abstractions.Ports;
using TC.Agro.Farm.Application.UseCases.Properties.List;
using TC.Agro.Farm.Tests.TestHelpers;
using TC.Agro.SharedKernel.Infrastructure.Pagination;

namespace TC.Agro.Farm.Tests.Application.UseCases.Properties.List;

public sealed class ListPropertiesQueryHandlerTests
{
    private readonly IPropertyReadStore _readStore = A.Fake<IPropertyReadStore>();
    private readonly ILogger<ListPropertiesQueryHandler> _logger = A.Fake<ILogger<ListPropertiesQueryHandler>>();

    public ListPropertiesQueryHandlerTests()
    {
        FastEndpointsTestBootstrap.EnsureInitialized();
    }

    [Fact]
    public async Task ExecuteAsync_WhenReadStoreReturnsNoData_ShouldReturnEmptyPaginatedResponse()
    {
        var query = new ListPropertiesQuery
        {
            PageNumber = 2,
            PageSize = 5,
            Filter = "missing"
        };

        A.CallTo(() => _readStore.GetPropertyListAsync(query, A<CancellationToken>._))
            .Returns((Array.Empty<ListPropertiesResponse>(), 0));

        var sut = CreateHandler();

        var result = await sut.ExecuteAsync(query, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.TotalCount.ShouldBe(0);
        result.Value.PageNumber.ShouldBe(2);
        result.Value.PageSize.ShouldBe(5);
        result.Value.Data.Count.ShouldBe(0);
        result.Value.ShouldBeOfType<PaginatedResponse<ListPropertiesResponse>>();
    }

    [Fact]
    public async Task ExecuteAsync_WhenReadStoreReturnsData_ShouldReturnPaginatedResponse()
    {
        var query = new ListPropertiesQuery
        {
            PageNumber = 1,
            PageSize = 10,
            SortBy = "name",
            SortDirection = "asc"
        };

        var rows = new List<ListPropertiesResponse>
        {
            new(
                Id: Guid.NewGuid(),
                Name: "Farm A",
                City: "Ribeirao Preto",
                State: "SP",
                Country: "Brazil",
                AreaHectares: 120,
                OwnerId: Guid.NewGuid(),
                OwnerName: "Producer A",
                IsActive: true,
                PlotCount: 3,
                CreatedAt: DateTimeOffset.UtcNow.AddDays(-30)),
            new(
                Id: Guid.NewGuid(),
                Name: "Farm B",
                City: "Franca",
                State: "SP",
                Country: "Brazil",
                AreaHectares: 80,
                OwnerId: Guid.NewGuid(),
                OwnerName: "Producer B",
                IsActive: true,
                PlotCount: 1,
                CreatedAt: DateTimeOffset.UtcNow.AddDays(-10))
        };

        A.CallTo(() => _readStore.GetPropertyListAsync(query, A<CancellationToken>._))
            .Returns((rows, 2));

        var sut = CreateHandler();

        var result = await sut.ExecuteAsync(query, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.TotalCount.ShouldBe(2);
        result.Value.Data.Count.ShouldBe(2);
        result.Value.Data[0].Name.ShouldBe("Farm A");
        result.Value.Data[1].Name.ShouldBe("Farm B");
    }

    private ListPropertiesQueryHandler CreateHandler()
        => new(_readStore, TestUserContextFactory.CreateAdmin(), _logger);
}
