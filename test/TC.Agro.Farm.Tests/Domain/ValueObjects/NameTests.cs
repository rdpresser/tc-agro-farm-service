using TC.Agro.Farm.Domain.ValueObjects;

namespace TC.Agro.Farm.Tests.Domain.ValueObjects
{
    public class NameTests
    {
        #region Create - Valid Cases

        [Theory]
        [InlineData("Fazenda São João")]
        [InlineData("Plot A1")]
        [InlineData("My Farm")]
        [InlineData("Fazenda do Vale-Norte")]
        [InlineData("Sítio D'água")]
        public void Create_WithValidName_ShouldSucceed(string nameValue)
        {
            // Act
            var result = Name.Create(nameValue);

            // Assert
            result.IsSuccess.ShouldBeTrue();
            result.Value.Value.ShouldBe(nameValue.Trim());
        }

        [Fact]
        public void Create_WithMinimumLength_ShouldSucceed()
        {
            // Arrange
            var nameValue = "AB"; // 2 characters (minimum)

            // Act
            var result = Name.Create(nameValue);

            // Assert
            result.IsSuccess.ShouldBeTrue();
            result.Value.Value.ShouldBe(nameValue);
        }

        [Fact]
        public void Create_WithMaximumLength_ShouldSucceed()
        {
            // Arrange
            var nameValue = new string('A', 200); // 200 characters (maximum)

            // Act
            var result = Name.Create(nameValue);

            // Assert
            result.IsSuccess.ShouldBeTrue();
            result.Value.Value.ShouldBe(nameValue);
        }

        [Fact]
        public void Create_WithWhitespace_ShouldTrim()
        {
            // Arrange
            var nameValue = "  Fazenda Central  ";

            // Act
            var result = Name.Create(nameValue);

            // Assert
            result.IsSuccess.ShouldBeTrue();
            result.Value.Value.ShouldBe("Fazenda Central");
        }

        #endregion

        #region Create - Invalid Cases

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("   ")]
        [InlineData(null)]
        public void Create_WithEmptyOrNullName_ShouldReturnRequiredError(string? nameValue)
        {
            // Act
            var result = Name.Create(nameValue!);

            // Assert
            result.IsSuccess.ShouldBeFalse();
            result.ValidationErrors.ShouldContain(e => e.Identifier == "Name.Required");
        }

        [Fact]
        public void Create_WithTooShortName_ShouldReturnTooShortError()
        {
            // Arrange
            var nameValue = "A"; // 1 character (below minimum of 2)

            // Act
            var result = Name.Create(nameValue);

            // Assert
            result.IsSuccess.ShouldBeFalse();
            result.ValidationErrors.ShouldContain(e => e.Identifier == "Name.TooShort");
        }

        [Fact]
        public void Create_WithTooLongName_ShouldReturnTooLongError()
        {
            // Arrange
            var nameValue = new string('A', 201); // 201 characters (above maximum of 200)

            // Act
            var result = Name.Create(nameValue);

            // Assert
            result.IsSuccess.ShouldBeFalse();
            result.ValidationErrors.ShouldContain(e => e.Identifier == "Name.TooLong");
        }

        [Theory]
        [InlineData("Farm@123")]
        [InlineData("Plot#1")]
        [InlineData("Name!Test")]
        [InlineData("Field$Area")]
        public void Create_WithInvalidCharacters_ShouldReturnInvalidFormatError(string nameValue)
        {
            // Act
            var result = Name.Create(nameValue);

            // Assert
            result.IsSuccess.ShouldBeFalse();
            result.ValidationErrors.ShouldContain(e => e.Identifier == "Name.InvalidFormat");
        }

        #endregion

        #region FromDb

        [Fact]
        public void FromDb_WithValidValue_ShouldSucceed()
        {
            // Arrange
            var nameValue = "Stored Farm Name";

            // Act
            var result = Name.FromDb(nameValue);

            // Assert
            result.IsSuccess.ShouldBeTrue();
            result.Value.Value.ShouldBe(nameValue);
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(null)]
        public void FromDb_WithEmptyOrNull_ShouldReturnRequiredError(string? nameValue)
        {
            // Act
            var result = Name.FromDb(nameValue!);

            // Assert
            result.IsSuccess.ShouldBeFalse();
            result.ValidationErrors.ShouldContain(e => e.Identifier == "Name.Required");
        }

        #endregion

        #region Implicit Conversion and ToString

        [Fact]
        public void ImplicitConversion_ShouldReturnValue()
        {
            // Arrange
            var nameValue = "Test Farm";
            var name = Name.Create(nameValue).Value;

            // Act
            string result = name;

            // Assert
            result.ShouldBe(nameValue);
        }

        [Fact]
        public void ToString_ShouldReturnValue()
        {
            // Arrange
            var nameValue = "Test Farm";
            var name = Name.Create(nameValue).Value;

            // Act
            var result = name.ToString();

            // Assert
            result.ShouldBe(nameValue);
        }

        #endregion
    }
}
