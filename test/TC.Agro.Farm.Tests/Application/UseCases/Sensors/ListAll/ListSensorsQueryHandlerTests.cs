using FakeItEasy;
using Microsoft.Extensions.Logging;
using TC.Agro.Farm.Application.Abstractions.Ports;
using TC.Agro.Farm.Application.UseCases.Sensors.ListAll;
using TC.Agro.Farm.Tests.TestHelpers;

namespace TC.Agro.Farm.Tests.Application.UseCases.Sensors.ListAll;

public sealed class ListSensorsQueryHandlerTests
{
    private readonly ISensorReadStore _readStore = A.Fake<ISensorReadStore>();
    private readonly ILogger<ListSensorsQueryHandler> _logger = A.Fake<ILogger<ListSensorsQueryHandler>>();

    public ListSensorsQueryHandlerTests()
    {
        FastEndpointsTestBootstrap.EnsureInitialized();
    }

    [Fact]
    public async Task ExecuteAsync_WhenReadStoreReturnsNoData_ShouldReturnEmptyPaginatedResult()
    {
        var query = new ListSensorsQuery
        {
            PageNumber = 3,
            PageSize = 2,
            Type = "Temperature"
        };

        A.CallTo(() => _readStore.ListSensorsAsync(query, A<CancellationToken>._))
            .Returns((Array.Empty<ListSensorsResponse>(), 0));

        var sut = new ListSensorsQueryHandler(_readStore, _logger);

        var result = await sut.ExecuteAsync(query, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.TotalCount.ShouldBe(0);
        result.Value.PageNumber.ShouldBe(3);
        result.Value.PageSize.ShouldBe(2);
        result.Value.Data.Count.ShouldBe(0);
    }

    [Fact]
    public async Task ExecuteAsync_WhenReadStoreReturnsRows_ShouldReturnPaginatedResult()
    {
        var query = new ListSensorsQuery
        {
            PageNumber = 1,
            PageSize = 10,
            Status = "Active"
        };

        var rows = new List<ListSensorsResponse>
        {
            new(
                Id: Guid.NewGuid(),
                PlotId: Guid.NewGuid(),
                PlotName: "Plot A",
                PropertyId: Guid.NewGuid(),
                PropertyName: "Farm A",
                OwnerId: Guid.NewGuid(),
                OwnerName: "Producer A",
                Type: "Temperature",
                Status: "Active",
                Label: "Temp-01",
                InstalledAt: DateTimeOffset.UtcNow.AddDays(-30)),
            new(
                Id: Guid.NewGuid(),
                PlotId: Guid.NewGuid(),
                PlotName: "Plot B",
                PropertyId: Guid.NewGuid(),
                PropertyName: "Farm B",
                OwnerId: Guid.NewGuid(),
                OwnerName: "Producer B",
                Type: "Humidity",
                Status: "Active",
                Label: "Hum-03",
                InstalledAt: DateTimeOffset.UtcNow.AddDays(-15))
        };

        A.CallTo(() => _readStore.ListSensorsAsync(query, A<CancellationToken>._))
            .Returns((rows, 2));

        var sut = new ListSensorsQueryHandler(_readStore, _logger);

        var result = await sut.ExecuteAsync(query, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.TotalCount.ShouldBe(2);
        result.Value.Data.Count.ShouldBe(2);
        result.Value.Data[0].Type.ShouldBe("Temperature");
        result.Value.Data[1].Type.ShouldBe("Humidity");
    }
}
