using FakeItEasy;
using Microsoft.Extensions.Logging;
using TC.Agro.Farm.Application.Abstractions;
using TC.Agro.Farm.Application.Abstractions.Ports;
using TC.Agro.Farm.Application.UseCases.Plots.Update;
using TC.Agro.Farm.Domain.Aggregates;
using TC.Agro.Farm.Tests.TestHelpers;
using TC.Agro.SharedKernel.Application.Ports;
using TC.Agro.SharedKernel.Infrastructure.UserClaims;

namespace TC.Agro.Farm.Tests.Application.UseCases.Plots.Update
{
    public sealed class UpdatePlotCommandHandlerTests
    {
        private readonly IPlotAggregateRepository _repository;
        private readonly ICropTypeCatalogRepository _cropTypeCatalogRepository;
        private readonly ICropTypeSuggestionRepository _cropTypeSuggestionRepository;
        private readonly IUserContext _userContext;
        private readonly ITransactionalOutbox _outbox;
        private readonly ILogger<UpdatePlotCommandHandler> _logger;
        private readonly UpdatePlotCommandHandler _handler;

        public UpdatePlotCommandHandlerTests()
        {
            FastEndpointsTestBootstrap.EnsureInitialized();

            _repository = A.Fake<IPlotAggregateRepository>();
            _cropTypeCatalogRepository = A.Fake<ICropTypeCatalogRepository>();
            _cropTypeSuggestionRepository = A.Fake<ICropTypeSuggestionRepository>();
            _userContext = A.Fake<IUserContext>();
            _outbox = A.Fake<ITransactionalOutbox>();
            _logger = A.Fake<ILogger<UpdatePlotCommandHandler>>();

            _handler = new UpdatePlotCommandHandler(
                _repository,
                _cropTypeCatalogRepository,
                _cropTypeSuggestionRepository,
                _userContext,
                _outbox,
                _logger);
        }

        [Fact]
        public async Task ExecuteAsync_WhenPlotDoesNotExist_ShouldReturnNotFound()
        {
            var command = CreateValidCommand();

            A.CallTo(() => _repository.GetByIdAsync(command.PlotId, A<CancellationToken>._))
                .Returns(Task.FromResult<PlotAggregate?>(null));

            var result = await _handler.ExecuteAsync(command, TestContext.Current.CancellationToken);

            result.IsSuccess.ShouldBeFalse();
            result.Errors.ShouldContain("Plot not found.");
            A.CallTo(() => _outbox.SaveChangesAsync(A<CancellationToken>._)).MustNotHaveHappened();
        }

        [Fact]
        public async Task ExecuteAsync_WhenUserIsNotOwnerAndNotAdmin_ShouldReturnUnauthorized()
        {
            var plot = CreateValidPlot(ownerId: Guid.NewGuid());
            var command = CreateValidCommand(plot.Id);
            var currentUserId = Guid.NewGuid();

            A.CallTo(() => _repository.GetByIdAsync(command.PlotId, A<CancellationToken>._))
                .Returns(Task.FromResult<PlotAggregate?>(plot));
            A.CallTo(() => _userContext.Id).Returns(currentUserId);
            A.CallTo(() => _userContext.IsAdmin).Returns(false);

            var result = await _handler.ExecuteAsync(command, TestContext.Current.CancellationToken);

            result.IsSuccess.ShouldBeFalse();
            result.Errors.ShouldContain("You are not authorized to update this plot.");
            A.CallTo(() => _outbox.SaveChangesAsync(A<CancellationToken>._)).MustNotHaveHappened();
        }

        [Fact]
        public async Task ExecuteAsync_WhenCommandIsValid_ShouldUpdatePlotAndSave()
        {
            var ownerId = Guid.NewGuid();
            var plot = CreateValidPlot(ownerId);
            var command = CreateValidCommand(plot.Id) with
            {
                Name = "Updated Plot Name",
                CropType = "Corn",
                AreaHectares = 65
            };

            A.CallTo(() => _repository.GetByIdAsync(command.PlotId, A<CancellationToken>._))
                .Returns(Task.FromResult<PlotAggregate?>(plot));
            A.CallTo(() => _repository.NameExistsForPropertyExcludingAsync(
                    command.Name,
                    plot.PropertyId,
                    plot.Id,
                    A<CancellationToken>._))
                .Returns(false);

            var existingCatalog = CropTypeCatalogAggregate.Create("Corn").Value;
            A.CallTo(() => _cropTypeCatalogRepository.GetByNameAsync("Corn", ownerId, A<CancellationToken>._))
                .Returns(existingCatalog);

            A.CallTo(() => _userContext.Id).Returns(ownerId);
            A.CallTo(() => _userContext.IsAdmin).Returns(false);

            var result = await _handler.ExecuteAsync(command, TestContext.Current.CancellationToken);

            result.IsSuccess.ShouldBeTrue();
            result.Value.Name.ShouldBe("Updated Plot Name");
            result.Value.CropType.ShouldBe("Corn");
            result.Value.AreaHectares.ShouldBe(65);

            A.CallTo(() => _outbox.SaveChangesAsync(A<CancellationToken>._)).MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task ExecuteAsync_WhenOnlyCatalogIdIsProvided_ShouldResolveLegacyCropTypeFromCatalog()
        {
            var ownerId = Guid.NewGuid();
            var plot = CreateValidPlot(ownerId);
            var catalog = CropTypeCatalogAggregate.Create("Soy").Value;

            var command = CreateValidCommand(plot.Id) with
            {
                CropType = string.Empty,
                CropTypeCatalogId = catalog.Id
            };

            A.CallTo(() => _repository.GetByIdAsync(command.PlotId, A<CancellationToken>._))
                .Returns(Task.FromResult<PlotAggregate?>(plot));
            A.CallTo(() => _repository.NameExistsForPropertyExcludingAsync(
                    command.Name,
                    plot.PropertyId,
                    plot.Id,
                    A<CancellationToken>._))
                .Returns(false);
            A.CallTo(() => _cropTypeCatalogRepository.GetByIdScopedAsync(catalog.Id, ownerId, false, A<CancellationToken>._))
                .Returns(catalog);
            A.CallTo(() => _userContext.Id).Returns(ownerId);
            A.CallTo(() => _userContext.IsAdmin).Returns(false);

            var result = await _handler.ExecuteAsync(command, TestContext.Current.CancellationToken);

            result.IsSuccess.ShouldBeTrue();
            result.Value.CropType.ShouldBe("Soy");
            result.Value.CropTypeCatalogId.ShouldBe(catalog.Id);
            A.CallTo(() => _outbox.SaveChangesAsync(A<CancellationToken>._)).MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task ExecuteAsync_WhenSelectedSuggestionIdMatchesCatalogId_ShouldTreatItAsCatalogOnlySelection()
        {
            var ownerId = Guid.NewGuid();
            var plot = CreateValidPlot(ownerId);
            var catalog = CropTypeCatalogAggregate.Create("Soy").Value;

            var command = CreateValidCommand(plot.Id) with
            {
                CropType = string.Empty,
                CropTypeCatalogId = catalog.Id,
                SelectedCropTypeSuggestionId = catalog.Id
            };

            A.CallTo(() => _repository.GetByIdAsync(command.PlotId, A<CancellationToken>._))
                .Returns(Task.FromResult<PlotAggregate?>(plot));
            A.CallTo(() => _repository.NameExistsForPropertyExcludingAsync(
                    command.Name,
                    plot.PropertyId,
                    plot.Id,
                    A<CancellationToken>._))
                .Returns(false);
            A.CallTo(() => _cropTypeCatalogRepository.GetByIdScopedAsync(catalog.Id, ownerId, false, A<CancellationToken>._))
                .Returns(catalog);
            A.CallTo(() => _userContext.Id).Returns(ownerId);
            A.CallTo(() => _userContext.IsAdmin).Returns(false);

            var result = await _handler.ExecuteAsync(command, TestContext.Current.CancellationToken);

            result.IsSuccess.ShouldBeTrue();
            result.Value.CropTypeCatalogId.ShouldBe(catalog.Id);
            result.Value.SelectedCropTypeSuggestionId.ShouldBeNull();
            A.CallTo(() => _cropTypeSuggestionRepository.GetByIdAsync(A<Guid>._, A<CancellationToken>._))
                .MustNotHaveHappened();
        }

        private static UpdatePlotCommand CreateValidCommand(Guid? plotId = null)
            => new(
                PlotId: plotId ?? Guid.NewGuid(),
                Name: "North Plot",
                CropType: "Soy",
                AreaHectares: 50,
                Latitude: -21.1775,
                Longitude: -47.8103,
                BoundaryGeoJson: "{\"type\":\"Polygon\",\"coordinates\":[[[-47.811,-21.178],[-47.809,-21.178],[-47.809,-21.176],[-47.811,-21.176],[-47.811,-21.178]]]}",
                PlantingDate: DateTimeOffset.UtcNow.AddDays(-30),
                ExpectedHarvestDate: DateTimeOffset.UtcNow.AddDays(120),
                IrrigationType: "Center Pivot",
                AdditionalNotes: "Updated notes");

        private static PlotAggregate CreateValidPlot(Guid ownerId)
        {
            var result = PlotAggregate.Create(
                propertyId: Guid.NewGuid(),
                ownerId: ownerId,
                name: "North Plot",
                cropType: "Soy",
                areaHectares: 50,
                plantingDate: DateTimeOffset.UtcNow.AddDays(-60),
                expectedHarvestDate: DateTimeOffset.UtcNow.AddDays(120),
                irrigationType: "Center Pivot",
                additionalNotes: "Initial notes",
                latitude: -21.1775,
                longitude: -47.8103,
                boundaryGeoJson: null,
                cropTypeCatalogId: Guid.NewGuid());

            result.IsSuccess.ShouldBeTrue();
            return result.Value;
        }
    }
}
