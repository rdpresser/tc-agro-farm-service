using TC.Agro.Farm.Domain.Aggregates;

namespace TC.Agro.Farm.Tests.Domain.Aggregates
{
    public class SensorAggregateTests
    {
        #region Create - Valid Cases

        [Fact]
        public void Create_WithValidParameters_ShouldSucceed()
        {
            // Arrange
            var plotId = Guid.NewGuid();
            var type = "Temperature";

            // Act
            var result = SensorAggregate.Create(plotId, type);

            // Assert
            result.IsSuccess.ShouldBeTrue();
            result.Value.ShouldNotBeNull();
            result.Value.Id.ShouldNotBe(Guid.Empty);
            result.Value.PlotId.ShouldBe(plotId);
            result.Value.Type.Value.ShouldBe(type);
            result.Value.Status.IsActive.ShouldBeTrue();
            result.Value.Label.ShouldBeNull();
            result.Value.IsActive.ShouldBeTrue();
        }

        [Fact]
        public void Create_WithLabel_ShouldSucceed()
        {
            // Arrange
            var plotId = Guid.NewGuid();
            var type = "Humidity";
            var label = "Sensor Norte";

            // Act
            var result = SensorAggregate.Create(plotId, type, label);

            // Assert
            result.IsSuccess.ShouldBeTrue();
            result.Value.Label.ShouldNotBeNull();
            result.Value.Label!.Value.ShouldBe(label);
        }

        [Theory]
        [InlineData("Temperature")]
        [InlineData("Humidity")]
        [InlineData("SoilMoisture")]
        [InlineData("Rainfall")]
        [InlineData("WindSpeed")]
        [InlineData("SolarRadiation")]
        [InlineData("Ph")]
        public void Create_WithVariousSensorTypes_ShouldSucceed(string type)
        {
            // Arrange
            var plotId = Guid.NewGuid();

            // Act
            var result = SensorAggregate.Create(plotId, type);

            // Assert
            result.IsSuccess.ShouldBeTrue();
            result.Value.Type.Value.ShouldBe(type);
        }

        #endregion

        #region Create - Invalid Cases

        [Fact]
        public void Create_WithEmptyPlotId_ShouldReturnValidationErrors()
        {
            // Arrange
            var plotId = Guid.Empty;
            var type = "Temperature";

            // Act
            var result = SensorAggregate.Create(plotId, type);

            // Assert
            result.IsSuccess.ShouldBeFalse();
            result.ValidationErrors.ShouldContain(e => e.Identifier == "Sensor.PlotIdRequired");
        }

        [Fact]
        public void Create_WithEmptyType_ShouldReturnValidationErrors()
        {
            // Arrange
            var plotId = Guid.NewGuid();
            var type = "";

            // Act
            var result = SensorAggregate.Create(plotId, type);

            // Assert
            result.IsSuccess.ShouldBeFalse();
            result.ValidationErrors.ShouldContain(e => e.Identifier == "SensorType.Required");
        }

        [Fact]
        public void Create_WithInvalidType_ShouldReturnValidationErrors()
        {
            // Arrange
            var plotId = Guid.NewGuid();
            var type = "InvalidSensorType";

            // Act
            var result = SensorAggregate.Create(plotId, type);

            // Assert
            result.IsSuccess.ShouldBeFalse();
            result.ValidationErrors.ShouldContain(e => e.Identifier == "SensorType.InvalidValue");
        }

        [Fact]
        public void Create_WithInvalidLabel_ShouldReturnValidationErrors()
        {
            // Arrange
            var plotId = Guid.NewGuid();
            var type = "Temperature";
            var label = "A"; // Too short (minimum 2 characters)

            // Act
            var result = SensorAggregate.Create(plotId, type, label);

            // Assert
            result.IsSuccess.ShouldBeFalse();
            result.ValidationErrors.ShouldContain(e => e.Identifier == "Name.TooShort");
        }

        [Fact]
        public void Create_WithMultipleInvalidFields_ShouldReturnAllValidationErrors()
        {
            // Arrange
            var plotId = Guid.Empty;
            var type = "";

            // Act
            var result = SensorAggregate.Create(plotId, type);

            // Assert
            result.IsSuccess.ShouldBeFalse();
            result.ValidationErrors.Count().ShouldBeGreaterThan(1);
        }

        #endregion

        #region UpdateLabel

        [Fact]
        public void UpdateLabel_WithValidLabel_ShouldSucceed()
        {
            // Arrange
            var sensor = CreateValidSensor();
            var newLabel = "Sensor Sul";

            // Act
            var result = sensor.UpdateLabel(newLabel);

            // Assert
            result.IsSuccess.ShouldBeTrue();
            sensor.Label.ShouldNotBeNull();
            sensor.Label!.Value.ShouldBe(newLabel);
        }

        [Fact]
        public void UpdateLabel_WithNull_ShouldClearLabel()
        {
            // Arrange
            var sensor = CreateValidSensorWithLabel();
            sensor.Label.ShouldNotBeNull();

            // Act
            var result = sensor.UpdateLabel(null);

            // Assert
            result.IsSuccess.ShouldBeTrue();
            sensor.Label.ShouldBeNull();
        }

        [Fact]
        public void UpdateLabel_WithWhitespace_ShouldClearLabel()
        {
            // Arrange
            var sensor = CreateValidSensorWithLabel();
            sensor.Label.ShouldNotBeNull();

            // Act
            var result = sensor.UpdateLabel("   ");

            // Assert
            result.IsSuccess.ShouldBeTrue();
            sensor.Label.ShouldBeNull();
        }

        [Fact]
        public void UpdateLabel_WithInvalidLabel_ShouldReturnValidationErrors()
        {
            // Arrange
            var sensor = CreateValidSensor();
            var invalidLabel = "A"; // Too short

            // Act
            var result = sensor.UpdateLabel(invalidLabel);

            // Assert
            result.IsSuccess.ShouldBeFalse();
            result.ValidationErrors.ShouldContain(e => e.Identifier == "Name.TooShort");
        }

        #endregion

        #region SetActive

        [Fact]
        public void SetActive_WhenInactive_ShouldSucceed()
        {
            // Arrange
            var sensor = CreateValidSensor();
            sensor.SetInactive();
            sensor.Status.IsInactive.ShouldBeTrue();

            // Act
            var result = sensor.SetActive();

            // Assert
            result.IsSuccess.ShouldBeTrue();
            sensor.Status.IsActive.ShouldBeTrue();
        }

        [Fact]
        public void SetActive_WhenAlreadyActive_ShouldReturnError()
        {
            // Arrange
            var sensor = CreateValidSensor();

            // Act
            var result = sensor.SetActive();

            // Assert
            result.IsSuccess.ShouldBeFalse();
            result.ValidationErrors.ShouldContain(e => e.Identifier == "Sensor.AlreadyActive");
        }

        #endregion

        #region SetInactive

        [Fact]
        public void SetInactive_WhenActive_ShouldSucceed()
        {
            // Arrange
            var sensor = CreateValidSensor();
            sensor.Status.IsActive.ShouldBeTrue();

            // Act
            var result = sensor.SetInactive();

            // Assert
            result.IsSuccess.ShouldBeTrue();
            sensor.Status.IsInactive.ShouldBeTrue();
        }

        [Fact]
        public void SetInactive_WhenAlreadyInactive_ShouldReturnError()
        {
            // Arrange
            var sensor = CreateValidSensor();
            sensor.SetInactive();

            // Act
            var result = sensor.SetInactive();

            // Assert
            result.IsSuccess.ShouldBeFalse();
            result.ValidationErrors.ShouldContain(e => e.Identifier == "Sensor.AlreadyInactive");
        }

        #endregion

        #region SetMaintenance

        [Fact]
        public void SetMaintenance_WhenActive_ShouldSucceed()
        {
            // Arrange
            var sensor = CreateValidSensor();

            // Act
            var result = sensor.SetMaintenance();

            // Assert
            result.IsSuccess.ShouldBeTrue();
            sensor.Status.IsMaintenance.ShouldBeTrue();
        }

        [Fact]
        public void SetMaintenance_WhenAlreadyInMaintenance_ShouldReturnError()
        {
            // Arrange
            var sensor = CreateValidSensor();
            sensor.SetMaintenance();

            // Act
            var result = sensor.SetMaintenance();

            // Assert
            result.IsSuccess.ShouldBeFalse();
            result.ValidationErrors.ShouldContain(e => e.Identifier == "Sensor.AlreadyInMaintenance");
        }

        #endregion

        #region SetFaulty

        [Fact]
        public void SetFaulty_WhenActive_ShouldSucceed()
        {
            // Arrange
            var sensor = CreateValidSensor();

            // Act
            var result = sensor.SetFaulty();

            // Assert
            result.IsSuccess.ShouldBeTrue();
            sensor.Status.IsFaulty.ShouldBeTrue();
        }

        [Fact]
        public void SetFaulty_WhenAlreadyFaulty_ShouldReturnError()
        {
            // Arrange
            var sensor = CreateValidSensor();
            sensor.SetFaulty();

            // Act
            var result = sensor.SetFaulty();

            // Assert
            result.IsSuccess.ShouldBeFalse();
            result.ValidationErrors.ShouldContain(e => e.Identifier == "Sensor.AlreadyFaulty");
        }

        #endregion

        #region Status Transitions

        [Fact]
        public void StatusTransition_FromActiveToMaintenance_ThenToFaulty_ShouldSucceed()
        {
            // Arrange
            var sensor = CreateValidSensor();
            sensor.Status.IsActive.ShouldBeTrue();

            // Act & Assert - Transition to Maintenance
            var maintenanceResult = sensor.SetMaintenance();
            maintenanceResult.IsSuccess.ShouldBeTrue();
            sensor.Status.IsMaintenance.ShouldBeTrue();

            // Act & Assert - Transition to Faulty
            var faultyResult = sensor.SetFaulty();
            faultyResult.IsSuccess.ShouldBeTrue();
            sensor.Status.IsFaulty.ShouldBeTrue();
        }

        [Fact]
        public void StatusTransition_FromFaultyToActive_ShouldSucceed()
        {
            // Arrange
            var sensor = CreateValidSensor();
            sensor.SetFaulty();
            sensor.Status.IsFaulty.ShouldBeTrue();

            // Act
            var result = sensor.SetActive();

            // Assert
            result.IsSuccess.ShouldBeTrue();
            sensor.Status.IsActive.ShouldBeTrue();
        }

        #endregion

        #region Helper Methods

        private static SensorAggregate CreateValidSensor()
        {
            var result = SensorAggregate.Create(
                Guid.NewGuid(),
                "Temperature");

            return result.Value;
        }

        private static SensorAggregate CreateValidSensorWithLabel()
        {
            var result = SensorAggregate.Create(
                Guid.NewGuid(),
                "Temperature",
                "Sensor Norte");

            return result.Value;
        }

        #endregion
    }
}
