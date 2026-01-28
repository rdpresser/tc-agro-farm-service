using TC.Agro.Farm.Domain.ValueObjects;

namespace TC.Agro.Farm.Tests.Domain.ValueObjects
{
    public class AreaTests
    {
        #region Create - Valid Cases

        [Theory]
        [InlineData(0.01)]   // Minimum
        [InlineData(1.0)]
        [InlineData(100.5)]
        [InlineData(500.0)]
        [InlineData(1000.0)]
        [InlineData(1000000)] // Maximum
        public void Create_WithValidArea_ShouldSucceed(double hectares)
        {
            // Act
            var result = Area.Create(hectares);

            // Assert
            result.IsSuccess.ShouldBeTrue();
            result.Value.Hectares.ShouldBe(Math.Round(hectares, 4));
        }

        [Fact]
        public void Create_WithPrecision_ShouldRoundTo4Decimals()
        {
            // Arrange
            var hectares = 123.456789;

            // Act
            var result = Area.Create(hectares);

            // Assert
            result.IsSuccess.ShouldBeTrue();
            result.Value.Hectares.ShouldBe(123.4568); // Rounded to 4 decimals
        }

        #endregion

        #region Create - Invalid Cases

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(-100)]
        public void Create_WithZeroOrNegative_ShouldReturnInvalidValueError(double hectares)
        {
            // Act
            var result = Area.Create(hectares);

            // Assert
            result.IsSuccess.ShouldBeFalse();
            result.ValidationErrors.ShouldContain(e => e.Identifier == "Area.InvalidValue");
        }

        [Fact]
        public void Create_WithTooSmallValue_ShouldReturnTooSmallError()
        {
            // Arrange
            var hectares = 0.001; // Below minimum of 0.01

            // Act
            var result = Area.Create(hectares);

            // Assert
            result.IsSuccess.ShouldBeFalse();
            result.ValidationErrors.ShouldContain(e => e.Identifier == "Area.TooSmall");
        }

        [Fact]
        public void Create_WithTooLargeValue_ShouldReturnTooLargeError()
        {
            // Arrange
            var hectares = 1_000_001; // Above maximum of 1,000,000

            // Act
            var result = Area.Create(hectares);

            // Assert
            result.IsSuccess.ShouldBeFalse();
            result.ValidationErrors.ShouldContain(e => e.Identifier == "Area.TooLarge");
        }

        [Fact]
        public void Create_WithNaN_ShouldReturnInvalidValueError()
        {
            // Act
            var result = Area.Create(double.NaN);

            // Assert
            result.IsSuccess.ShouldBeFalse();
            result.ValidationErrors.ShouldContain(e => e.Identifier == "Area.InvalidValue");
        }

        [Theory]
        [InlineData(double.PositiveInfinity)]
        [InlineData(double.NegativeInfinity)]
        public void Create_WithInfinity_ShouldReturnInvalidValueError(double hectares)
        {
            // Arrange - Infinity values should be caught before range validation
            hectares.ShouldBeOneOf(double.PositiveInfinity, double.NegativeInfinity);

            // Act
            var result = Area.Create(hectares);

            // Assert - Should fail with InvalidValue (NaN/Infinity check happens first)
            result.IsSuccess.ShouldBeFalse();
            result.ValidationErrors.ShouldContain(e => e.Identifier == "Area.InvalidValue");
        }

        #endregion

        #region FromDb

        [Fact]
        public void FromDb_WithValidValue_ShouldSucceed()
        {
            // Arrange
            var hectares = 150.5;

            // Act
            var result = Area.FromDb(hectares);

            // Assert
            result.IsSuccess.ShouldBeTrue();
            result.Value.Hectares.ShouldBe(hectares);
        }

        [Fact]
        public void FromDb_WithZeroValue_ShouldReturnInvalidValueError()
        {
            // Arrange - Testing FromDb specifically (bypasses full Create validation)
            const double zeroValue = 0;

            // Act
            var result = Area.FromDb(zeroValue);

            // Assert
            result.IsSuccess.ShouldBeFalse();
            result.ValidationErrors.ShouldContain(e => e.Identifier == "Area.InvalidValue");
            result.ValidationErrors.ShouldHaveSingleItem();
        }

        [Fact]
        public void FromDb_WithNegativeValue_ShouldReturnInvalidValueError()
        {
            // Arrange - Testing FromDb specifically (bypasses full Create validation)
            const double negativeValue = -50;

            // Act
            var result = Area.FromDb(negativeValue);

            // Assert
            result.IsSuccess.ShouldBeFalse();
            result.ValidationErrors.ShouldContain(e => e.Identifier == "Area.InvalidValue");
            result.ValidationErrors.ShouldHaveSingleItem();
        }

        #endregion

        #region Conversions

        [Fact]
        public void ToSquareMeters_ShouldConvertCorrectly()
        {
            // Arrange
            var hectares = 1.0;
            var area = Area.Create(hectares).Value;

            // Act
            var squareMeters = area.ToSquareMeters();

            // Assert
            squareMeters.ShouldBe(10_000);
        }

        [Fact]
        public void ToAcres_ShouldConvertCorrectly()
        {
            // Arrange
            var hectares = 1.0;
            var area = Area.Create(hectares).Value;

            // Act
            var acres = area.ToAcres();

            // Assert
            acres.ShouldBe(2.47105, tolerance: 0.00001);
        }

        [Fact]
        public void ImplicitConversion_ShouldReturnHectares()
        {
            // Arrange
            var hectares = 100.5;
            var area = Area.Create(hectares).Value;

            // Act
            double result = area;

            // Assert
            result.ShouldBe(hectares);
        }

        #endregion

        #region ToString

        [Fact]
        public void ToString_ShouldFormatWithTwoDecimals()
        {
            // Arrange
            var hectares = 123.456;
            var area = Area.Create(hectares).Value;

            // Act
            var result = area.ToString();

            // Assert - Check format contains rounded value and unit (locale-independent)
            result.ShouldContain("123");
            result.ShouldContain("46");
            result.ShouldEndWith(" ha");
        }

        #endregion
    }
}
