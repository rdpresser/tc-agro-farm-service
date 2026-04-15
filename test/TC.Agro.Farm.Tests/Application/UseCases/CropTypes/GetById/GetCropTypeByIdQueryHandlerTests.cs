using Ardalis.Result;
using FakeItEasy;
using Microsoft.Extensions.Logging;
using TC.Agro.Farm.Application.Abstractions;
using TC.Agro.Farm.Application.Abstractions.Ports;
using TC.Agro.Farm.Application.UseCases.CropTypes.GetById;
using TC.Agro.Farm.Application.UseCases.CropTypes.List;
using TC.Agro.Farm.Application.UseCases.Plots.ListAll;
using TC.Agro.Farm.Tests.TestHelpers;
using TC.Agro.SharedKernel.Infrastructure.Pagination;
using TC.Agro.SharedKernel.Infrastructure.UserClaims;

namespace TC.Agro.Farm.Tests.Application.UseCases.CropTypes.GetById;

/// <summary>
/// Unit tests for GetCropTypeByIdQueryHandler.
/// </summary>
public sealed class GetCropTypeByIdQueryHandlerTests
{
    private readonly ICropTypeCatalogReadStore _readStore = A.Fake<ICropTypeCatalogReadStore>();
    private readonly ILogger<GetCropTypeByIdQueryHandler> _logger =
        A.Fake<ILogger<GetCropTypeByIdQueryHandler>>();

    public GetCropTypeByIdQueryHandlerTests()
    {
        FastEndpointsTestBootstrap.EnsureInitialized();
    }

    // ──────────────────────────────────────────
    // Not found
    // ──────────────────────────────────────────

    [Fact]
    public async Task ExecuteAsync_WhenCropTypeNotFound_ShouldReturnNotFound()
    {
        var ct = TestContext.Current.CancellationToken;
        var query = new GetCropTypeByIdQuery { Id = Guid.NewGuid(), IncludeInactive = false };

        A.CallTo(() => _readStore.GetByIdAsync(query.Id, query.IncludeInactive, ct))
            .Returns(Task.FromResult<GetCropTypeByIdResponse?>(null));

        var handler = CreateHandler();
        var result = await handler.ExecuteAsync(query, ct);

        result.Status.ShouldBe(ResultStatus.NotFound);
    }

    // ──────────────────────────────────────────
    // Found
    // ──────────────────────────────────────────

    [Fact]
    public async Task ExecuteAsync_WhenCropTypeExists_ShouldReturnSuccessWithCropType()
    {
        var ct = TestContext.Current.CancellationToken;
        var cropTypeId = Guid.NewGuid();
        var query = new GetCropTypeByIdQuery { Id = cropTypeId, IncludeInactive = false };

        var expected = BuildResponse(cropTypeId);

        A.CallTo(() => _readStore.GetByIdAsync(cropTypeId, false, ct))
            .Returns(Task.FromResult<GetCropTypeByIdResponse?>(expected));

        var handler = CreateHandler();
        var result = await handler.ExecuteAsync(query, ct);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.Id.ShouldBe(cropTypeId);
        result.Value.CropType.ShouldBe("Soy");
    }

    [Fact]
    public async Task ExecuteAsync_WithIncludeInactive_ShouldPassFlagToReadStore()
    {
        var ct = TestContext.Current.CancellationToken;
        var cropTypeId = Guid.NewGuid();
        var query = new GetCropTypeByIdQuery { Id = cropTypeId, IncludeInactive = true };

        var expected = BuildResponse(cropTypeId);

        A.CallTo(() => _readStore.GetByIdAsync(cropTypeId, true, ct))
            .Returns(Task.FromResult<GetCropTypeByIdResponse?>(expected));

        var handler = CreateHandler();
        var result = await handler.ExecuteAsync(query, ct);

        result.IsSuccess.ShouldBeTrue();

        A.CallTo(() => _readStore.GetByIdAsync(cropTypeId, true, ct))
            .MustHaveHappenedOnceExactly();
    }

    // ──────────────────────────────────────────
    // Constructor guard
    // ──────────────────────────────────────────

    [Fact]
    public void Constructor_WithNullReadStore_ShouldThrow()
    {
        Should.Throw<ArgumentNullException>(() =>
            new GetCropTypeByIdQueryHandler(null!, A.Fake<IUserContext>(), _logger));
    }

    // ──────────────────────────────────────────
    // Helpers
    // ──────────────────────────────────────────

    private GetCropTypeByIdQueryHandler CreateHandler()
    {
        var userContext = TestUserContextFactory.CreateProducer();
        return new GetCropTypeByIdQueryHandler(_readStore, userContext, _logger);
    }

    private static GetCropTypeByIdResponse BuildResponse(Guid id)
        => new(
            Id: id,
            PropertyId: Guid.NewGuid(),
            OwnerId: Guid.NewGuid(),
            PropertyName: "Test Farm",
            OwnerName: "Test Owner",
            CropType: "Soy",
            SuggestedImage: "soy",
            Source: "Catalog",
            IsOverride: false,
            IsStale: false,
            ConfidenceScore: 90,
            PlantingWindow: "Oct-Dec",
            HarvestCycleMonths: 5,
            SuggestedIrrigationType: "Drip",
            MinSoilMoisture: 30,
            MaxTemperature: 35,
            MinHumidity: 45,
            Notes: null,
            Model: null,
            GeneratedAt: null,
            IsActive: true,
            CreatedAt: DateTimeOffset.UtcNow,
            UpdatedAt: null,
            CropTypeCatalogId: id);
}
