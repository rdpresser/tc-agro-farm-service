using TC.Agro.Farm.Domain.ValueObjects;

namespace TC.Agro.Farm.Tests.Domain.ValueObjects
{
    public class LocationTests
    {
        #region Create - Valid Cases

        [Fact]
        public void Create_WithRequiredFieldsOnly_ShouldSucceed()
        {
            // Arrange
            var address = "Rua Principal, 123";
            var city = "São Paulo";
            var state = "SP";
            var country = "Brazil";

            // Act
            var result = Location.Create(address, city, state, country);

            // Assert
            result.IsSuccess.ShouldBeTrue();
            result.Value.Address.ShouldBe(address);
            result.Value.City.ShouldBe(city);
            result.Value.State.ShouldBe(state);
            result.Value.Country.ShouldBe(country);
            result.Value.Latitude.ShouldBeNull();
            result.Value.Longitude.ShouldBeNull();
        }

        [Fact]
        public void Create_WithCoordinates_ShouldSucceed()
        {
            // Arrange
            var address = "Avenida Brasil, 456";
            var city = "Rio de Janeiro";
            var state = "RJ";
            var country = "Brazil";
            var latitude = -22.9068;
            var longitude = -43.1729;

            // Act
            var result = Location.Create(address, city, state, country, latitude, longitude);

            // Assert
            result.IsSuccess.ShouldBeTrue();
            result.Value.Address.ShouldBe(address);
            result.Value.City.ShouldBe(city);
            result.Value.State.ShouldBe(state);
            result.Value.Country.ShouldBe(country);
            result.Value.Latitude.ShouldBe(latitude);
            result.Value.Longitude.ShouldBe(longitude);
        }

        [Fact]
        public void Create_WithWhitespace_ShouldTrim()
        {
            // Arrange
            var address = "  Rua Central  ";
            var city = "  Campinas  ";
            var state = "  SP  ";
            var country = "  Brazil  ";

            // Act
            var result = Location.Create(address, city, state, country);

            // Assert
            result.IsSuccess.ShouldBeTrue();
            result.Value.Address.ShouldBe("Rua Central");
            result.Value.City.ShouldBe("Campinas");
            result.Value.State.ShouldBe("SP");
            result.Value.Country.ShouldBe("Brazil");
        }

        [Theory]
        [InlineData(-90)]
        [InlineData(0)]
        [InlineData(90)]
        public void Create_WithValidLatitude_ShouldSucceed(double latitude)
        {
            // Act
            var result = Location.Create("Address", "City", "State", "Country", latitude, 0);

            // Assert
            result.IsSuccess.ShouldBeTrue();
            result.Value.Latitude.ShouldBe(latitude);
        }

        [Theory]
        [InlineData(-180)]
        [InlineData(0)]
        [InlineData(180)]
        public void Create_WithValidLongitude_ShouldSucceed(double longitude)
        {
            // Act
            var result = Location.Create("Address", "City", "State", "Country", 0, longitude);

            // Assert
            result.IsSuccess.ShouldBeTrue();
            result.Value.Longitude.ShouldBe(longitude);
        }

        #endregion

        #region Create - Invalid Cases - Required Fields

        [Theory]
        [InlineData("", "City", "State", "Country")]
        [InlineData(" ", "City", "State", "Country")]
        [InlineData(null, "City", "State", "Country")]
        public void Create_WithEmptyOrNullAddress_ShouldReturnAddressRequiredError(
            string? address, string city, string state, string country)
        {
            // Act
            var result = Location.Create(address!, city, state, country);

            // Assert
            result.IsSuccess.ShouldBeFalse();
            result.ValidationErrors.ShouldContain(e => e.Identifier == "Location.AddressRequired");
        }

        [Theory]
        [InlineData("Address", "", "State", "Country")]
        [InlineData("Address", " ", "State", "Country")]
        [InlineData("Address", null, "State", "Country")]
        public void Create_WithEmptyOrNullCity_ShouldReturnCityRequiredError(
            string address, string? city, string state, string country)
        {
            // Act
            var result = Location.Create(address, city!, state, country);

            // Assert
            result.IsSuccess.ShouldBeFalse();
            result.ValidationErrors.ShouldContain(e => e.Identifier == "Location.CityRequired");
        }

        [Theory]
        [InlineData("Address", "City", "", "Country")]
        [InlineData("Address", "City", " ", "Country")]
        [InlineData("Address", "City", null, "Country")]
        public void Create_WithEmptyOrNullState_ShouldReturnStateRequiredError(
            string address, string city, string? state, string country)
        {
            // Act
            var result = Location.Create(address, city, state!, country);

            // Assert
            result.IsSuccess.ShouldBeFalse();
            result.ValidationErrors.ShouldContain(e => e.Identifier == "Location.StateRequired");
        }

        [Theory]
        [InlineData("Address", "City", "State", "")]
        [InlineData("Address", "City", "State", " ")]
        [InlineData("Address", "City", "State", null)]
        public void Create_WithEmptyOrNullCountry_ShouldReturnCountryRequiredError(
            string address, string city, string state, string? country)
        {
            // Act
            var result = Location.Create(address, city, state, country!);

            // Assert
            result.IsSuccess.ShouldBeFalse();
            result.ValidationErrors.ShouldContain(e => e.Identifier == "Location.CountryRequired");
        }

        [Fact]
        public void Create_WithAllEmptyFields_ShouldReturnAllRequiredErrors()
        {
            // Act
            var result = Location.Create("", "", "", "");

            // Assert
            result.IsSuccess.ShouldBeFalse();
            result.ValidationErrors.ShouldContain(e => e.Identifier == "Location.AddressRequired");
            result.ValidationErrors.ShouldContain(e => e.Identifier == "Location.CityRequired");
            result.ValidationErrors.ShouldContain(e => e.Identifier == "Location.StateRequired");
            result.ValidationErrors.ShouldContain(e => e.Identifier == "Location.CountryRequired");
        }

        #endregion

        #region Create - Invalid Cases - Length Validation

        [Fact]
        public void Create_WithTooLongAddress_ShouldReturnAddressTooLongError()
        {
            // Arrange
            var address = new string('A', 501); // 501 characters (above maximum of 500)

            // Act
            var result = Location.Create(address, "City", "State", "Country");

            // Assert
            result.IsSuccess.ShouldBeFalse();
            result.ValidationErrors.ShouldContain(e => e.Identifier == "Location.AddressTooLong");
        }

        [Fact]
        public void Create_WithTooLongCity_ShouldReturnCityTooLongError()
        {
            // Arrange
            var city = new string('C', 101); // 101 characters (above maximum of 100)

            // Act
            var result = Location.Create("Address", city, "State", "Country");

            // Assert
            result.IsSuccess.ShouldBeFalse();
            result.ValidationErrors.ShouldContain(e => e.Identifier == "Location.CityTooLong");
        }

        [Fact]
        public void Create_WithTooLongState_ShouldReturnStateTooLongError()
        {
            // Arrange
            var state = new string('S', 101); // 101 characters (above maximum of 100)

            // Act
            var result = Location.Create("Address", "City", state, "Country");

            // Assert
            result.IsSuccess.ShouldBeFalse();
            result.ValidationErrors.ShouldContain(e => e.Identifier == "Location.StateTooLong");
        }

        [Fact]
        public void Create_WithTooLongCountry_ShouldReturnCountryTooLongError()
        {
            // Arrange
            var country = new string('C', 101); // 101 characters (above maximum of 100)

            // Act
            var result = Location.Create("Address", "City", "State", country);

            // Assert
            result.IsSuccess.ShouldBeFalse();
            result.ValidationErrors.ShouldContain(e => e.Identifier == "Location.CountryTooLong");
        }

        #endregion

        #region Create - Invalid Cases - Coordinates

        [Theory]
        [InlineData(-91)]
        [InlineData(91)]
        [InlineData(-100)]
        [InlineData(100)]
        public void Create_WithInvalidLatitude_ShouldReturnInvalidLatitudeError(double latitude)
        {
            // Act
            var result = Location.Create("Address", "City", "State", "Country", latitude, 0);

            // Assert
            result.IsSuccess.ShouldBeFalse();
            result.ValidationErrors.ShouldContain(e => e.Identifier == "Location.InvalidLatitude");
        }

        [Theory]
        [InlineData(-181)]
        [InlineData(181)]
        [InlineData(-200)]
        [InlineData(200)]
        public void Create_WithInvalidLongitude_ShouldReturnInvalidLongitudeError(double longitude)
        {
            // Act
            var result = Location.Create("Address", "City", "State", "Country", 0, longitude);

            // Assert
            result.IsSuccess.ShouldBeFalse();
            result.ValidationErrors.ShouldContain(e => e.Identifier == "Location.InvalidLongitude");
        }

        [Fact]
        public void Create_WithBothInvalidCoordinates_ShouldReturnBothErrors()
        {
            // Act
            var result = Location.Create("Address", "City", "State", "Country", -100, 200);

            // Assert
            result.IsSuccess.ShouldBeFalse();
            result.ValidationErrors.ShouldContain(e => e.Identifier == "Location.InvalidLatitude");
            result.ValidationErrors.ShouldContain(e => e.Identifier == "Location.InvalidLongitude");
        }

        #endregion

        #region FromDb

        [Fact]
        public void FromDb_WithValidValues_ShouldSucceed()
        {
            // Arrange
            var address = "Stored Address";
            var city = "Stored City";
            var state = "Stored State";
            var country = "Stored Country";
            var latitude = -23.5505;
            var longitude = -46.6333;

            // Act
            var result = Location.FromDb(address, city, state, country, latitude, longitude);

            // Assert
            result.IsSuccess.ShouldBeTrue();
            result.Value.Address.ShouldBe(address);
            result.Value.City.ShouldBe(city);
            result.Value.State.ShouldBe(state);
            result.Value.Country.ShouldBe(country);
            result.Value.Latitude.ShouldBe(latitude);
            result.Value.Longitude.ShouldBe(longitude);
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(null)]
        public void FromDb_WithEmptyOrNullAddress_ShouldReturnAddressRequiredError(string? address)
        {
            // Act
            var result = Location.FromDb(address!, "City", "State", "Country", null, null);

            // Assert
            result.IsSuccess.ShouldBeFalse();
            result.ValidationErrors.ShouldContain(e => e.Identifier == "Location.AddressRequired");
        }

        #endregion

        #region ToString

        [Fact]
        public void ToString_ShouldFormatCorrectly()
        {
            // Arrange
            var location = Location.Create("Rua A, 100", "São Paulo", "SP", "Brazil").Value;

            // Act
            var result = location.ToString();

            // Assert
            result.ShouldBe("Rua A, 100, São Paulo, SP, Brazil");
        }

        #endregion
    }
}
