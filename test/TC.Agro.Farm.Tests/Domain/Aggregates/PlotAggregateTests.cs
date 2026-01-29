using TC.Agro.Farm.Domain.Aggregates;

namespace TC.Agro.Farm.Tests.Domain.Aggregates
{
    public class PlotAggregateTests
    {
        #region Create - Valid Cases

        [Fact]
        public void Create_WithValidParameters_ShouldSucceed()
        {
            // Arrange
            var propertyId = Guid.NewGuid();
            var name = "Talhão A1";
            var cropType = "Soy";
            var areaHectares = 25.5;

            // Act
            var result = PlotAggregate.Create(propertyId, name, cropType, areaHectares);

            // Assert
            result.IsSuccess.ShouldBeTrue();
            result.Value.ShouldNotBeNull();
            result.Value.Id.ShouldNotBe(Guid.Empty);
            result.Value.PropertyId.ShouldBe(propertyId);
            result.Value.Name.Value.ShouldBe(name);
            result.Value.CropType.Value.ShouldBe(cropType);
            result.Value.AreaHectares.Hectares.ShouldBe(areaHectares);
            result.Value.IsActive.ShouldBeTrue();
        }

        [Theory]
        [InlineData("Corn")]
        [InlineData("Wheat")]
        [InlineData("Coffee")]
        [InlineData("Sugarcane")]
        [InlineData("Pasture")]
        public void Create_WithVariousCropTypes_ShouldSucceed(string cropType)
        {
            // Arrange
            var propertyId = Guid.NewGuid();
            var name = "Talhão Test";
            var areaHectares = 10.0;

            // Act
            var result = PlotAggregate.Create(propertyId, name, cropType, areaHectares);

            // Assert
            result.IsSuccess.ShouldBeTrue();
            result.Value.CropType.Value.ShouldBe(cropType);
        }

        #endregion

        #region Create - Invalid Cases

        [Fact]
        public void Create_WithEmptyPropertyId_ShouldReturnValidationErrors()
        {
            // Arrange
            var propertyId = Guid.Empty;
            var name = "Talhão A1";
            var cropType = "Soy";
            var areaHectares = 25.5;

            // Act
            var result = PlotAggregate.Create(propertyId, name, cropType, areaHectares);

            // Assert
            result.IsSuccess.ShouldBeFalse();
            result.ValidationErrors.ShouldContain(e => e.Identifier == "Plot.PropertyIdRequired");
        }

        [Fact]
        public void Create_WithEmptyName_ShouldReturnValidationErrors()
        {
            // Arrange
            var propertyId = Guid.NewGuid();
            var name = "";
            var cropType = "Soy";
            var areaHectares = 25.5;

            // Act
            var result = PlotAggregate.Create(propertyId, name, cropType, areaHectares);

            // Assert
            result.IsSuccess.ShouldBeFalse();
            result.ValidationErrors.ShouldContain(e => e.Identifier == "Name.Required");
        }

        [Fact]
        public void Create_WithEmptyCropType_ShouldReturnValidationErrors()
        {
            // Arrange
            var propertyId = Guid.NewGuid();
            var name = "Talhão A1";
            var cropType = "";
            var areaHectares = 25.5;

            // Act
            var result = PlotAggregate.Create(propertyId, name, cropType, areaHectares);

            // Assert
            result.IsSuccess.ShouldBeFalse();
            result.ValidationErrors.ShouldContain(e => e.Identifier == "CropType.Required");
        }

        [Fact]
        public void Create_WithInvalidArea_ShouldReturnValidationErrors()
        {
            // Arrange
            var propertyId = Guid.NewGuid();
            var name = "Talhão A1";
            var cropType = "Soy";
            var areaHectares = 0.0;

            // Act
            var result = PlotAggregate.Create(propertyId, name, cropType, areaHectares);

            // Assert
            result.IsSuccess.ShouldBeFalse();
            result.ValidationErrors.ShouldContain(e => e.Identifier == "Area.InvalidValue");
        }

        [Fact]
        public void Create_WithMultipleInvalidFields_ShouldReturnAllValidationErrors()
        {
            // Arrange
            var propertyId = Guid.Empty;
            var name = "";
            var cropType = "";
            var areaHectares = -10.0;

            // Act
            var result = PlotAggregate.Create(propertyId, name, cropType, areaHectares);

            // Assert
            result.IsSuccess.ShouldBeFalse();
            result.ValidationErrors.Count().ShouldBeGreaterThan(1);
        }

        #endregion

        #region Update - Valid Cases

        [Fact]
        public void Update_WithValidParameters_ShouldSucceed()
        {
            // Arrange
            var plot = CreateValidPlot();
            var newName = "Talhão Atualizado";
            var newCropType = "Corn";
            var newArea = 30.0;

            // Act
            var result = plot.Update(newName, newCropType, newArea);

            // Assert
            result.IsSuccess.ShouldBeTrue();
            plot.Name.Value.ShouldBe(newName);
            plot.CropType.Value.ShouldBe(newCropType);
            plot.AreaHectares.Hectares.ShouldBe(newArea);
        }

        #endregion

        #region Update - Invalid Cases

        [Fact]
        public void Update_WithEmptyName_ShouldReturnValidationErrors()
        {
            // Arrange
            var plot = CreateValidPlot();

            // Act
            var result = plot.Update("", "Corn", 30.0);

            // Assert
            result.IsSuccess.ShouldBeFalse();
            result.ValidationErrors.ShouldContain(e => e.Identifier == "Name.Required");
        }

        [Fact]
        public void Update_WithEmptyCropType_ShouldReturnValidationErrors()
        {
            // Arrange
            var plot = CreateValidPlot();

            // Act
            var result = plot.Update("Valid Name", "", 30.0);

            // Assert
            result.IsSuccess.ShouldBeFalse();
            result.ValidationErrors.ShouldContain(e => e.Identifier == "CropType.Required");
        }

        [Fact]
        public void Update_WithInvalidArea_ShouldReturnValidationErrors()
        {
            // Arrange
            var plot = CreateValidPlot();

            // Act
            var result = plot.Update("Valid Name", "Corn", -100.0);

            // Assert
            result.IsSuccess.ShouldBeFalse();
            result.ValidationErrors.ShouldContain(e => e.Identifier == "Area.InvalidValue");
        }

        #endregion

        #region ChangeCropType

        [Fact]
        public void ChangeCropType_WithValidCropType_ShouldSucceed()
        {
            // Arrange
            var plot = CreateValidPlot();
            var newCropType = "Wheat";

            // Act
            var result = plot.ChangeCropType(newCropType);

            // Assert
            result.IsSuccess.ShouldBeTrue();
            plot.CropType.Value.ShouldBe(newCropType);
        }

        [Fact]
        public void ChangeCropType_WithEmptyCropType_ShouldReturnValidationErrors()
        {
            // Arrange
            var plot = CreateValidPlot();

            // Act
            var result = plot.ChangeCropType("");

            // Assert
            result.IsSuccess.ShouldBeFalse();
            result.ValidationErrors.ShouldContain(e => e.Identifier == "CropType.Required");
        }

        #endregion

        #region Deactivate

        [Fact]
        public void Deactivate_WhenActive_ShouldSucceed()
        {
            // Arrange
            var plot = CreateValidPlot();
            plot.IsActive.ShouldBeTrue();

            // Act
            var result = plot.Deactivate();

            // Assert
            result.IsSuccess.ShouldBeTrue();
            plot.IsActive.ShouldBeFalse();
        }

        [Fact]
        public void Deactivate_WhenAlreadyDeactivated_ShouldReturnError()
        {
            // Arrange
            var plot = CreateValidPlot();
            plot.Deactivate();

            // Act
            var result = plot.Deactivate();

            // Assert
            result.IsSuccess.ShouldBeFalse();
            result.ValidationErrors.ShouldContain(e => e.Identifier == "Plot.AlreadyDeactivated");
        }

        #endregion

        #region Activate

        [Fact]
        public void Activate_WhenDeactivated_ShouldSucceed()
        {
            // Arrange
            var plot = CreateValidPlot();
            plot.Deactivate();
            plot.IsActive.ShouldBeFalse();

            // Act
            var result = plot.Activate();

            // Assert
            result.IsSuccess.ShouldBeTrue();
            plot.IsActive.ShouldBeTrue();
        }

        [Fact]
        public void Activate_WhenAlreadyActive_ShouldReturnError()
        {
            // Arrange
            var plot = CreateValidPlot();

            // Act
            var result = plot.Activate();

            // Assert
            result.IsSuccess.ShouldBeFalse();
            result.ValidationErrors.ShouldContain(e => e.Identifier == "Plot.AlreadyActivated");
        }

        #endregion

        #region Helper Methods

        private static PlotAggregate CreateValidPlot()
        {
            var result = PlotAggregate.Create(
                Guid.NewGuid(),
                "Talhão A1",
                "Soy",
                25.5);

            return result.Value;
        }

        #endregion
    }
}
