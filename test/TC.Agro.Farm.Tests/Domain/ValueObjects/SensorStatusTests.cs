using TC.Agro.Farm.Domain.ValueObjects;

namespace TC.Agro.Farm.Tests.Domain.ValueObjects
{
    public class SensorStatusTests
    {
        #region Create - Valid Cases

        [Theory]
        [InlineData("Active")]
        [InlineData("Inactive")]
        [InlineData("Maintenance")]
        [InlineData("Faulty")]
        public void Create_WithValidStatus_ShouldSucceed(string statusValue)
        {
            // Act
            var result = SensorStatus.Create(statusValue);

            // Assert
            result.IsSuccess.ShouldBeTrue();
            result.Value.Value.ShouldBe(statusValue);
        }

        [Theory]
        [InlineData("active", "Active")]
        [InlineData("INACTIVE", "Inactive")]
        [InlineData("maintenance", "Maintenance")]
        [InlineData("FAULTY", "Faulty")]
        public void Create_WithDifferentCasing_ShouldNormalizeValue(string statusValue, string expectedValue)
        {
            // Act
            var result = SensorStatus.Create(statusValue);

            // Assert
            result.IsSuccess.ShouldBeTrue();
            result.Value.Value.ShouldBe(expectedValue);
        }

        [Fact]
        public void Create_WithWhitespace_ShouldTrim()
        {
            // Arrange
            var statusValue = "  Active  ";

            // Act
            var result = SensorStatus.Create(statusValue);

            // Assert
            result.IsSuccess.ShouldBeTrue();
            result.Value.Value.ShouldBe("Active");
        }

        #endregion

        #region Create - Invalid Cases

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("   ")]
        [InlineData(null)]
        public void Create_WithEmptyOrNullValue_ShouldReturnRequiredError(string? statusValue)
        {
            // Act
            var result = SensorStatus.Create(statusValue!);

            // Assert
            result.IsSuccess.ShouldBeFalse();
            result.ValidationErrors.ShouldContain(e => e.Identifier == "SensorStatus.Required");
        }

        [Theory]
        [InlineData("InvalidStatus")]
        [InlineData("Online")]
        [InlineData("Offline")]
        [InlineData("Broken")]
        public void Create_WithInvalidStatus_ShouldReturnInvalidValueError(string statusValue)
        {
            // Act
            var result = SensorStatus.Create(statusValue);

            // Assert
            result.IsSuccess.ShouldBeFalse();
            result.ValidationErrors.ShouldContain(e => e.Identifier == "SensorStatus.InvalidValue");
        }

        #endregion

        #region FromDb

        [Fact]
        public void FromDb_WithValidValue_ShouldSucceed()
        {
            // Arrange
            var statusValue = "Active";

            // Act
            var result = SensorStatus.FromDb(statusValue);

            // Assert
            result.IsSuccess.ShouldBeTrue();
            result.Value.Value.ShouldBe(statusValue);
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(null)]
        public void FromDb_WithEmptyOrNull_ShouldReturnRequiredError(string? statusValue)
        {
            // Act
            var result = SensorStatus.FromDb(statusValue!);

            // Assert
            result.IsSuccess.ShouldBeFalse();
            result.ValidationErrors.ShouldContain(e => e.Identifier == "SensorStatus.Required");
        }

        #endregion

        #region Factory Methods

        [Fact]
        public void CreateActive_ShouldReturnActiveStatus()
        {
            // Act
            var status = SensorStatus.CreateActive();

            // Assert
            status.Value.ShouldBe(SensorStatus.Active);
            status.IsActive.ShouldBeTrue();
        }

        [Fact]
        public void CreateInactive_ShouldReturnInactiveStatus()
        {
            // Act
            var status = SensorStatus.CreateInactive();

            // Assert
            status.Value.ShouldBe(SensorStatus.Inactive);
            status.IsInactive.ShouldBeTrue();
        }

        #endregion

        #region Status Check Properties

        [Fact]
        public void IsActive_WithActiveStatus_ShouldReturnTrue()
        {
            // Arrange
            var status = SensorStatus.Create("Active").Value;

            // Assert
            status.IsActive.ShouldBeTrue();
            status.IsInactive.ShouldBeFalse();
            status.IsMaintenance.ShouldBeFalse();
            status.IsFaulty.ShouldBeFalse();
        }

        [Fact]
        public void IsInactive_WithInactiveStatus_ShouldReturnTrue()
        {
            // Arrange
            var status = SensorStatus.Create("Inactive").Value;

            // Assert
            status.IsInactive.ShouldBeTrue();
            status.IsActive.ShouldBeFalse();
            status.IsMaintenance.ShouldBeFalse();
            status.IsFaulty.ShouldBeFalse();
        }

        [Fact]
        public void IsMaintenance_WithMaintenanceStatus_ShouldReturnTrue()
        {
            // Arrange
            var status = SensorStatus.Create("Maintenance").Value;

            // Assert
            status.IsMaintenance.ShouldBeTrue();
            status.IsActive.ShouldBeFalse();
            status.IsInactive.ShouldBeFalse();
            status.IsFaulty.ShouldBeFalse();
        }

        [Fact]
        public void IsFaulty_WithFaultyStatus_ShouldReturnTrue()
        {
            // Arrange
            var status = SensorStatus.Create("Faulty").Value;

            // Assert
            status.IsFaulty.ShouldBeTrue();
            status.IsActive.ShouldBeFalse();
            status.IsInactive.ShouldBeFalse();
            status.IsMaintenance.ShouldBeFalse();
        }

        #endregion

        #region GetValidStatuses

        [Fact]
        public void GetValidStatuses_ShouldReturnAllValidStatuses()
        {
            // Act
            var validStatuses = SensorStatus.GetValidStatuses();

            // Assert
            validStatuses.ShouldContain(SensorStatus.Active);
            validStatuses.ShouldContain(SensorStatus.Inactive);
            validStatuses.ShouldContain(SensorStatus.Maintenance);
            validStatuses.ShouldContain(SensorStatus.Faulty);
            validStatuses.Count.ShouldBe(4);
        }

        #endregion

        #region Implicit Conversion and ToString

        [Fact]
        public void ImplicitConversion_ShouldReturnValue()
        {
            // Arrange
            var statusValue = "Active";
            var status = SensorStatus.Create(statusValue).Value;

            // Act
            string result = status;

            // Assert
            result.ShouldBe(statusValue);
        }

        [Fact]
        public void ToString_ShouldReturnValue()
        {
            // Arrange
            var statusValue = "Maintenance";
            var status = SensorStatus.Create(statusValue).Value;

            // Act
            var result = status.ToString();

            // Assert
            result.ShouldBe(statusValue);
        }

        #endregion
    }
}
