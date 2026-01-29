using TC.Agro.Farm.Domain.ValueObjects;

namespace TC.Agro.Farm.Tests.Domain.ValueObjects
{
    public class CropTypeTests
    {
        #region Create - Valid Cases

        [Theory]
        [InlineData("Soy")]
        [InlineData("Corn")]
        [InlineData("Wheat")]
        [InlineData("Coffee")]
        [InlineData("Sugarcane")]
        [InlineData("Rice")]
        [InlineData("Beans")]
        [InlineData("Pasture")]
        public void Create_WithCommonCropType_ShouldSucceed(string cropTypeValue)
        {
            // Act
            var result = CropType.Create(cropTypeValue);

            // Assert
            result.IsSuccess.ShouldBeTrue();
            result.Value.Value.ShouldBe(cropTypeValue);
        }

        [Fact]
        public void Create_WithCustomCropType_ShouldSucceed()
        {
            // Arrange
            var cropTypeValue = "Quinoa";

            // Act
            var result = CropType.Create(cropTypeValue);

            // Assert
            result.IsSuccess.ShouldBeTrue();
            result.Value.Value.ShouldBe(cropTypeValue);
        }

        [Fact]
        public void Create_WithHyphenatedCropType_ShouldSucceed()
        {
            // Arrange
            var cropTypeValue = "Sugar-Beet";

            // Act
            var result = CropType.Create(cropTypeValue);

            // Assert
            result.IsSuccess.ShouldBeTrue();
            result.Value.Value.ShouldBe(cropTypeValue);
        }

        [Fact]
        public void Create_WithWhitespace_ShouldTrim()
        {
            // Arrange
            var cropTypeValue = "  Corn  ";

            // Act
            var result = CropType.Create(cropTypeValue);

            // Assert
            result.IsSuccess.ShouldBeTrue();
            result.Value.Value.ShouldBe("Corn");
        }

        [Fact]
        public void Create_WithAccentedCharacters_ShouldSucceed()
        {
            // Arrange
            var cropTypeValue = "Café";

            // Act
            var result = CropType.Create(cropTypeValue);

            // Assert
            result.IsSuccess.ShouldBeTrue();
            result.Value.Value.ShouldBe(cropTypeValue);
        }

        #endregion

        #region Create - Invalid Cases

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("   ")]
        [InlineData(null)]
        public void Create_WithEmptyOrNullCropType_ShouldReturnRequiredError(string? cropTypeValue)
        {
            // Act
            var result = CropType.Create(cropTypeValue!);

            // Assert
            result.IsSuccess.ShouldBeFalse();
            result.ValidationErrors.ShouldContain(e => e.Identifier == "CropType.Required");
        }

        [Fact]
        public void Create_WithTooLongCropType_ShouldReturnTooLongError()
        {
            // Arrange
            var cropTypeValue = new string('A', 101); // 101 characters (above maximum of 100)

            // Act
            var result = CropType.Create(cropTypeValue);

            // Assert
            result.IsSuccess.ShouldBeFalse();
            result.ValidationErrors.ShouldContain(e => e.Identifier == "CropType.TooLong");
        }

        [Theory]
        [InlineData("Crop@Type")]
        [InlineData("Corn#1")]
        [InlineData("Wheat!")]
        [InlineData("Rice$")]
        public void Create_WithInvalidCharacters_ShouldReturnInvalidValueError(string cropTypeValue)
        {
            // Act
            var result = CropType.Create(cropTypeValue);

            // Assert
            result.IsSuccess.ShouldBeFalse();
            result.ValidationErrors.ShouldContain(e => e.Identifier == "CropType.InvalidValue");
        }

        #endregion

        #region FromDb

        [Fact]
        public void FromDb_WithValidValue_ShouldSucceed()
        {
            // Arrange
            var cropTypeValue = "Stored Crop";

            // Act
            var result = CropType.FromDb(cropTypeValue);

            // Assert
            result.IsSuccess.ShouldBeTrue();
            result.Value.Value.ShouldBe(cropTypeValue);
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(null)]
        public void FromDb_WithEmptyOrNull_ShouldReturnRequiredError(string? cropTypeValue)
        {
            // Act
            var result = CropType.FromDb(cropTypeValue!);

            // Assert
            result.IsSuccess.ShouldBeFalse();
            result.ValidationErrors.ShouldContain(e => e.Identifier == "CropType.Required");
        }

        #endregion

        #region Common Crop Types

        [Fact]
        public void CommonCropTypes_ShouldContainExpectedValues()
        {
            // Assert
            CropType.CommonCropTypes.ShouldContain("Soy");
            CropType.CommonCropTypes.ShouldContain("Corn");
            CropType.CommonCropTypes.ShouldContain("Wheat");
            CropType.CommonCropTypes.ShouldContain("Cotton");
            CropType.CommonCropTypes.ShouldContain("Coffee");
            CropType.CommonCropTypes.ShouldContain("Sugarcane");
            CropType.CommonCropTypes.ShouldContain("Rice");
            CropType.CommonCropTypes.ShouldContain("Beans");
            CropType.CommonCropTypes.ShouldContain("Pasture");
            CropType.CommonCropTypes.ShouldContain("Other");
        }

        #endregion

        #region Implicit Conversion and ToString

        [Fact]
        public void ImplicitConversion_ShouldReturnValue()
        {
            // Arrange
            var cropTypeValue = "Corn";
            var cropType = CropType.Create(cropTypeValue).Value;

            // Act
            string result = cropType;

            // Assert
            result.ShouldBe(cropTypeValue);
        }

        [Fact]
        public void ToString_ShouldReturnValue()
        {
            // Arrange
            var cropTypeValue = "Wheat";
            var cropType = CropType.Create(cropTypeValue).Value;

            // Act
            var result = cropType.ToString();

            // Assert
            result.ShouldBe(cropTypeValue);
        }

        #endregion
    }
}
