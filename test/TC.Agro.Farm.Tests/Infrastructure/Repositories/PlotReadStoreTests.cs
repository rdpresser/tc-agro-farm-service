using FakeItEasy;
using Microsoft.EntityFrameworkCore;
using TC.Agro.Farm.Application.Abstractions;
using TC.Agro.Farm.Application.UseCases.Plots.ListAll;
using TC.Agro.Farm.Application.UseCases.Plots.ListByProperty;
using TC.Agro.Farm.Domain.Aggregates;
using TC.Agro.Farm.Domain.Snapshots;
using TC.Agro.Farm.Domain.ValueObjects;
using TC.Agro.Farm.Infrastructure;
using TC.Agro.Farm.Infrastructure.Repositories;
using TC.Agro.SharedKernel.Infrastructure.UserClaims;

namespace TC.Agro.Farm.Tests.Infrastructure.Repositories
{
    public class PlotReadStoreTests
    {
        [Fact]
        public async Task ListPlotsAsync_WhenPlotCoordinatesMissing_ShouldFallbackToPropertyCoordinates()
        {
            await using var dbContext = CreateDbContext();
            var seedData = await SeedPlotAsync(dbContext, propertyLatitude: -21.1775, propertyLongitude: -47.8103);

            var sut = new PlotReadStore(dbContext, CreateUserContext(AppConstants.AdminRole, seedData.OwnerId));

            var query = new ListPlotsQuery
            {
                PageNumber = 1,
                PageSize = 10,
                SortBy = "name",
                SortDirection = "asc"
            };

            var (plots, totalCount) = await sut.ListPlotsAsync(query, TestContext.Current.CancellationToken);

            totalCount.ShouldBe(1);
            plots.Count.ShouldBe(1);
            plots[0].Latitude.ShouldBe(-21.1775);
            plots[0].Longitude.ShouldBe(-47.8103);
        }

        [Fact]
        public async Task ListPlotsFromPropertyAsync_WhenPlotCoordinatesMissing_ShouldFallbackToPropertyCoordinates()
        {
            await using var dbContext = CreateDbContext();
            var seedData = await SeedPlotAsync(dbContext, propertyLatitude: -22.9035, propertyLongitude: -43.2096);

            var sut = new PlotReadStore(dbContext, CreateUserContext(AppConstants.AdminRole, seedData.OwnerId));

            var query = new ListPlotsFromPropertyQuery
            {
                Id = seedData.PropertyId,
                PageNumber = 1,
                PageSize = 10,
                SortBy = "name",
                SortDirection = "asc"
            };

            var (plots, totalCount) = await sut.ListPlotsFromPropertyAsync(query, TestContext.Current.CancellationToken);

            totalCount.ShouldBe(1);
            plots.Count.ShouldBe(1);
            plots[0].Latitude.ShouldBe(-22.9035);
            plots[0].Longitude.ShouldBe(-43.2096);
        }

        [Fact]
        public async Task GetByIdAsync_WhenPlotCoordinatesMissing_ShouldFallbackToPropertyCoordinates()
        {
            await using var dbContext = CreateDbContext();
            var seedData = await SeedPlotAsync(dbContext, propertyLatitude: -20.3155, propertyLongitude: -40.3128);

            var sut = new PlotReadStore(dbContext, CreateUserContext(AppConstants.AdminRole, seedData.OwnerId));

            var plot = await sut.GetByIdAsync(seedData.PlotId, TestContext.Current.CancellationToken);

            plot.ShouldNotBeNull();
            plot.Latitude.ShouldBe(-20.3155);
            plot.Longitude.ShouldBe(-40.3128);
        }

        [Fact]
        public async Task GetByIdAsync_WhenPlotCoordinatesExist_ShouldPrioritizePlotCoordinates()
        {
            await using var dbContext = CreateDbContext();
            var seedData = await SeedPlotAsync(
                dbContext,
                propertyLatitude: -21.1775,
                propertyLongitude: -47.8103,
                plotLatitude: -23.5505,
                plotLongitude: -46.6333);

            var sut = new PlotReadStore(dbContext, CreateUserContext(AppConstants.AdminRole, seedData.OwnerId));

            var plot = await sut.GetByIdAsync(seedData.PlotId, TestContext.Current.CancellationToken);

            plot.ShouldNotBeNull();
            plot.Latitude.ShouldBe(-23.5505);
            plot.Longitude.ShouldBe(-46.6333);
        }

        [Fact]
        public async Task ListPlotsAsync_WhenFilteringByCropTypeCatalogId_ShouldReturnOnlyMatchingPlots()
        {
            await using var dbContext = CreateDbContext();

            var soySeed = await SeedPlotAsync(
                dbContext,
                propertyLatitude: -21.1775,
                propertyLongitude: -47.8103,
                catalogCropType: "Soy");

            await SeedPlotAsync(
                dbContext,
                propertyLatitude: -22.0000,
                propertyLongitude: -48.0000,
                catalogCropType: "Corn");

            var sut = new PlotReadStore(dbContext, CreateUserContext(AppConstants.AdminRole, soySeed.OwnerId));

            var query = new ListPlotsQuery
            {
                PageNumber = 1,
                PageSize = 10,
                CropTypeCatalogId = soySeed.CatalogId
            };

            var (plots, totalCount) = await sut.ListPlotsAsync(query, TestContext.Current.CancellationToken);

            totalCount.ShouldBe(1);
            plots.Count.ShouldBe(1);
            plots[0].Id.ShouldBe(soySeed.PlotId);
            plots[0].CropTypeCatalogId.ShouldBe(soySeed.CatalogId);
            plots[0].CropType.ShouldBe("Soy");
        }

        [Fact]
        public async Task GetByIdAsync_WhenCurrentCycleDiffersFromLegacyPlotFields_ShouldReturnCurrentCycleData()
        {
            await using var dbContext = CreateDbContext();
            var seedData = await SeedPlotAsync(
                dbContext,
                propertyLatitude: -21.1775,
                propertyLongitude: -47.8103,
                plotCropType: "Soy",
                currentCycleCropType: "Corn");

            var persistedPlot = await dbContext.Plots
                .AsNoTracking()
                .FirstAsync(p => p.Id == seedData.PlotId, TestContext.Current.CancellationToken);

            var persistedCycle = await dbContext.CropCycles
                .AsNoTracking()
                .Where(c => c.PlotId == seedData.PlotId)
                .OrderByDescending(c => c.EndedAt == null)
                .ThenByDescending(c => c.StartedAt)
                .FirstAsync(TestContext.Current.CancellationToken);

            var sut = new PlotReadStore(dbContext, CreateUserContext(AppConstants.AdminRole, seedData.OwnerId));

            var plot = await sut.GetByIdAsync(seedData.PlotId, TestContext.Current.CancellationToken);

            plot.ShouldNotBeNull();
            plot.CropType.ShouldBe("Corn");
            plot.CropTypeCatalogId.ShouldBe(seedData.CatalogId);
            plot.IrrigationType.ShouldBe("Drip Irrigation");
            plot.AdditionalNotes.ShouldBe("Cycle notes");
            plot.PlantingDate.ShouldBe(persistedCycle.StartedAt);
            plot.ExpectedHarvestDate.ShouldBe(persistedCycle.ExpectedHarvestDate!.Value);

            // Guardrail assertion to ensure this test would catch a fallback-to-plot regression.
            (persistedPlot.PlantingDate == persistedCycle.StartedAt).ShouldBeFalse();
            (persistedPlot.ExpectedHarvestDate == persistedCycle.ExpectedHarvestDate!.Value).ShouldBeFalse();
        }

        private static async Task<(Guid OwnerId, Guid PropertyId, Guid PlotId, Guid CatalogId)> SeedPlotAsync(
            ApplicationDbContext dbContext,
            double? propertyLatitude,
            double? propertyLongitude,
            double? plotLatitude = null,
            double? plotLongitude = null,
            string? catalogCropType = null,
            string? plotCropType = null,
            string? currentCycleCropType = null)
        {
            var ownerId = Guid.NewGuid();
            var ownerSnapshot = OwnerSnapshot.Create(ownerId, "Producer A", "producer.a@tcagro.com");
            dbContext.OwnerSnapshots.Add(ownerSnapshot);

            var normalizedPlotCropType = plotCropType;
            if (string.IsNullOrWhiteSpace(normalizedPlotCropType))
            {
                normalizedPlotCropType = string.IsNullOrWhiteSpace(catalogCropType)
                    ? "Soy"
                    : catalogCropType;
            }

            var normalizedCurrentCycleCropType = currentCycleCropType;
            if (string.IsNullOrWhiteSpace(normalizedCurrentCycleCropType))
            {
                normalizedCurrentCycleCropType = string.IsNullOrWhiteSpace(catalogCropType)
                    ? normalizedPlotCropType
                    : catalogCropType;
            }

            var plotCatalogResult = CropTypeCatalogAggregate.Create(normalizedPlotCropType);
            plotCatalogResult.IsSuccess.ShouldBeTrue();
            var plotCatalogAggregate = plotCatalogResult.Value;
            dbContext.CropTypeCatalogs.Add(plotCatalogAggregate);

            CropTypeCatalogAggregate activeCatalogAggregate;
            if (string.Equals(normalizedCurrentCycleCropType, normalizedPlotCropType, StringComparison.OrdinalIgnoreCase))
            {
                activeCatalogAggregate = plotCatalogAggregate;
            }
            else
            {
                var activeCatalogResult = CropTypeCatalogAggregate.Create(normalizedCurrentCycleCropType);
                activeCatalogResult.IsSuccess.ShouldBeTrue();
                activeCatalogAggregate = activeCatalogResult.Value;
                dbContext.CropTypeCatalogs.Add(activeCatalogAggregate);
            }

            var propertyResult = PropertyAggregate.Create(
                name: "Property A",
                address: "Road 1",
                city: "Ribeirao Preto",
                state: "SP",
                country: "Brazil",
                areaHectares: 120,
                ownerId: ownerId,
                latitude: propertyLatitude,
                longitude: propertyLongitude);

            propertyResult.IsSuccess.ShouldBeTrue();
            var property = propertyResult.Value;
            dbContext.Properties.Add(property);

            var plotResult = PlotAggregate.Create(
                propertyId: property.Id,
                ownerId: ownerId,
                name: "Plot A",
                cropType: normalizedPlotCropType,
                areaHectares: 25,
                plantingDate: DateTimeOffset.UtcNow.AddDays(-30),
                expectedHarvestDate: DateTimeOffset.UtcNow.AddDays(120),
                irrigationType: IrrigationType.CenterPivot,
                additionalNotes: "Legacy plot notes",
                latitude: plotLatitude,
                longitude: plotLongitude,
                boundaryGeoJson: null,
                cropTypeCatalogId: plotCatalogAggregate.Id);

            plotResult.IsSuccess.ShouldBeTrue();
            var plot = plotResult.Value;
            dbContext.Plots.Add(plot);

            var cropCycleResult = CropCycleAggregate.Start(
                plotId: plot.Id,
                propertyId: property.Id,
                ownerId: ownerId,
                cropTypeCatalogId: activeCatalogAggregate.Id,
                startedAt: DateTimeOffset.UtcNow.AddDays(-20),
                irrigationType: "Drip Irrigation",
                expectedHarvestDate: DateTimeOffset.UtcNow.AddDays(100),
                status: CropCycleStatus.Planted,
                notes: "Cycle notes");

            cropCycleResult.IsSuccess.ShouldBeTrue();
            dbContext.CropCycles.Add(cropCycleResult.Value);

            await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

            return (ownerId, property.Id, plot.Id, activeCatalogAggregate.Id);
        }

        private static ApplicationDbContext CreateDbContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase($"plot-read-store-tests-{Guid.NewGuid()}")
                .Options;

            return new ApplicationDbContext(options);
        }

        private static IUserContext CreateUserContext(string role, Guid userId)
        {
            var userContext = A.Fake<IUserContext>();
            A.CallTo(() => userContext.Role).Returns(role);
            A.CallTo(() => userContext.IsAdmin)
                .Returns(string.Equals(role, AppConstants.AdminRole, StringComparison.OrdinalIgnoreCase));
            A.CallTo(() => userContext.Id).Returns(userId);
            A.CallTo(() => userContext.Name).Returns("Test User");
            A.CallTo(() => userContext.Email).Returns("test.user@tcagro.com");
            A.CallTo(() => userContext.Username).Returns("test.user");
            A.CallTo(() => userContext.IsAuthenticated).Returns(true);
            return userContext;
        }
    }
}
