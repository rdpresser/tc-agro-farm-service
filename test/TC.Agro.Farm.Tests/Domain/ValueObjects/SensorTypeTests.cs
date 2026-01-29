using TC.Agro.Farm.Domain.ValueObjects;

namespace TC.Agro.Farm.Tests.Domain.ValueObjects
{
    public class SensorTypeTests
    {
        #region Create - Valid Cases

        [Theory]
        [InlineData("Temperature")]
        [InlineData("Humidity")]
        [InlineData("SoilMoisture")]
        [InlineData("Rainfall")]
        [InlineData("WindSpeed")]
        [InlineData("SolarRadiation")]
        [InlineData("Ph")]
        public void Create_WithValidSensorType_ShouldSucceed(string typeValue)
        {
            // Act
            var result = SensorType.Create(typeValue);

            // Assert
            result.IsSuccess.ShouldBeTrue();
            result.Value.Value.ShouldBe(typeValue);
        }

        [Theory]
        [InlineData("temperature", "Temperature")]
        [InlineData("HUMIDITY", "Humidity")]
        [InlineData("soilmoisture", "SoilMoisture")]
        [InlineData("RAINFALL", "Rainfall")]
        public void Create_WithDifferentCasing_ShouldNormalizeValue(string typeValue, string expectedValue)
        {
            // Act
            var result = SensorType.Create(typeValue);

            // Assert
            result.IsSuccess.ShouldBeTrue();
            result.Value.Value.ShouldBe(expectedValue);
        }

        [Fact]
        public void Create_WithWhitespace_ShouldTrim()
        {
            // Arrange
            var typeValue = "  Temperature  ";

            // Act
            var result = SensorType.Create(typeValue);

            // Assert
            result.IsSuccess.ShouldBeTrue();
            result.Value.Value.ShouldBe("Temperature");
        }

        #endregion

        #region Create - Invalid Cases

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("   ")]
        [InlineData(null)]
        public void Create_WithEmptyOrNullValue_ShouldReturnRequiredError(string? typeValue)
        {
            // Act
            var result = SensorType.Create(typeValue!);

            // Assert
            result.IsSuccess.ShouldBeFalse();
            result.ValidationErrors.ShouldContain(e => e.Identifier == "SensorType.Required");
        }

        [Theory]
        [InlineData("InvalidType")]
        [InlineData("Pressure")]
        [InlineData("Light")]
        [InlineData("RandomSensor")]
        public void Create_WithInvalidSensorType_ShouldReturnInvalidValueError(string typeValue)
        {
            // Act
            var result = SensorType.Create(typeValue);

            // Assert
            result.IsSuccess.ShouldBeFalse();
            result.ValidationErrors.ShouldContain(e => e.Identifier == "SensorType.InvalidValue");
        }

        #endregion

        #region FromDb

        [Fact]
        public void FromDb_WithValidValue_ShouldSucceed()
        {
            // Arrange
            var typeValue = "Temperature";

            // Act
            var result = SensorType.FromDb(typeValue);

            // Assert
            result.IsSuccess.ShouldBeTrue();
            result.Value.Value.ShouldBe(typeValue);
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(null)]
        public void FromDb_WithEmptyOrNull_ShouldReturnRequiredError(string? typeValue)
        {
            // Act
            var result = SensorType.FromDb(typeValue!);

            // Assert
            result.IsSuccess.ShouldBeFalse();
            result.ValidationErrors.ShouldContain(e => e.Identifier == "SensorType.Required");
        }

        #endregion

        #region GetValidTypes

        [Fact]
        public void GetValidTypes_ShouldReturnAllValidTypes()
        {
            // Act
            var validTypes = SensorType.GetValidTypes();

            // Assert
            validTypes.ShouldContain(SensorType.Temperature);
            validTypes.ShouldContain(SensorType.Humidity);
            validTypes.ShouldContain(SensorType.SoilMoisture);
            validTypes.ShouldContain(SensorType.Rainfall);
            validTypes.ShouldContain(SensorType.WindSpeed);
            validTypes.ShouldContain(SensorType.SolarRadiation);
            validTypes.ShouldContain(SensorType.Ph);
            validTypes.Count.ShouldBe(7);
        }

        #endregion

        #region Implicit Conversion and ToString

        [Fact]
        public void ImplicitConversion_ShouldReturnValue()
        {
            // Arrange
            var typeValue = "Temperature";
            var sensorType = SensorType.Create(typeValue).Value;

            // Act
            string result = sensorType;

            // Assert
            result.ShouldBe(typeValue);
        }

        [Fact]
        public void ToString_ShouldReturnValue()
        {
            // Arrange
            var typeValue = "Humidity";
            var sensorType = SensorType.Create(typeValue).Value;

            // Act
            var result = sensorType.ToString();

            // Assert
            result.ShouldBe(typeValue);
        }

        #endregion
    }
}
