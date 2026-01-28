using TC.Agro.Farm.Domain.Aggregates;

namespace TC.Agro.Farm.Tests.Domain.Aggregates
{
    public class PropertyAggregateTests
    {
        #region Create - Valid Cases

        [Fact]
        public void Create_WithValidParameters_ShouldSucceed()
        {
            // Arrange
            var name = "Fazenda São João";
            var address = "Rua Principal, 123";
            var city = "Ribeirão Preto";
            var state = "SP";
            var country = "Brazil";
            var areaHectares = 150.5;
            var ownerId = Guid.NewGuid();

            // Act
            var result = PropertyAggregate.Create(name, address, city, state, country, areaHectares, ownerId);

            // Assert
            result.IsSuccess.ShouldBeTrue();
            result.Value.ShouldNotBeNull();
            result.Value.Id.ShouldNotBe(Guid.Empty);
            result.Value.Name.Value.ShouldBe(name);
            result.Value.Location.Address.ShouldBe(address);
            result.Value.Location.City.ShouldBe(city);
            result.Value.Location.State.ShouldBe(state);
            result.Value.Location.Country.ShouldBe(country);
            result.Value.AreaHectares.Hectares.ShouldBe(areaHectares);
            result.Value.OwnerId.ShouldBe(ownerId);
            result.Value.IsActive.ShouldBeTrue();
        }

        [Fact]
        public void Create_WithCoordinates_ShouldSucceed()
        {
            // Arrange
            var name = "Fazenda Vista Alegre";
            var address = "Estrada Rural, KM 15";
            var city = "Uberaba";
            var state = "MG";
            var country = "Brazil";
            var areaHectares = 500.0;
            var ownerId = Guid.NewGuid();
            var latitude = -19.7472;
            var longitude = -47.9381;

            // Act
            var result = PropertyAggregate.Create(name, address, city, state, country, areaHectares, ownerId, latitude, longitude);

            // Assert
            result.IsSuccess.ShouldBeTrue();
            result.Value.Location.Latitude.ShouldBe(latitude);
            result.Value.Location.Longitude.ShouldBe(longitude);
        }

        #endregion

        #region Create - Invalid Cases

        [Fact]
        public void Create_WithEmptyName_ShouldReturnValidationErrors()
        {
            // Arrange
            var name = "";
            var address = "Rua Principal, 123";
            var city = "Ribeirão Preto";
            var state = "SP";
            var country = "Brazil";
            var areaHectares = 150.5;
            var ownerId = Guid.NewGuid();

            // Act
            var result = PropertyAggregate.Create(name, address, city, state, country, areaHectares, ownerId);

            // Assert
            result.IsSuccess.ShouldBeFalse();
            result.ValidationErrors.ShouldContain(e => e.Identifier == "Name.Required");
        }

        [Fact]
        public void Create_WithEmptyOwnerId_ShouldReturnValidationErrors()
        {
            // Arrange
            var name = "Fazenda Test";
            var address = "Rua Principal, 123";
            var city = "Ribeirão Preto";
            var state = "SP";
            var country = "Brazil";
            var areaHectares = 150.5;
            var ownerId = Guid.Empty;

            // Act
            var result = PropertyAggregate.Create(name, address, city, state, country, areaHectares, ownerId);

            // Assert
            result.IsSuccess.ShouldBeFalse();
            result.ValidationErrors.ShouldContain(e => e.Identifier == "Property.OwnerIdRequired");
        }

        [Fact]
        public void Create_WithInvalidArea_ShouldReturnValidationErrors()
        {
            // Arrange
            var name = "Fazenda Test";
            var address = "Rua Principal, 123";
            var city = "Ribeirão Preto";
            var state = "SP";
            var country = "Brazil";
            var areaHectares = -10.0;
            var ownerId = Guid.NewGuid();

            // Act
            var result = PropertyAggregate.Create(name, address, city, state, country, areaHectares, ownerId);

            // Assert
            result.IsSuccess.ShouldBeFalse();
            result.ValidationErrors.ShouldContain(e => e.Identifier == "Area.InvalidValue");
        }

        [Fact]
        public void Create_WithMultipleInvalidFields_ShouldReturnAllValidationErrors()
        {
            // Arrange
            var name = ""; // Invalid
            var address = ""; // Invalid
            var city = ""; // Invalid
            var state = ""; // Invalid
            var country = ""; // Invalid
            var areaHectares = -10.0; // Invalid
            var ownerId = Guid.Empty; // Invalid

            // Act
            var result = PropertyAggregate.Create(name, address, city, state, country, areaHectares, ownerId);

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
            var property = CreateValidProperty();
            var newName = "Fazenda Atualizada";
            var newAddress = "Nova Rua, 456";
            var newCity = "Campinas";
            var newState = "SP";
            var newCountry = "Brazil";
            var newArea = 200.0;

            // Act
            var result = property.Update(newName, newAddress, newCity, newState, newCountry, newArea);

            // Assert
            result.IsSuccess.ShouldBeTrue();
            property.Name.Value.ShouldBe(newName);
            property.Location.Address.ShouldBe(newAddress);
            property.Location.City.ShouldBe(newCity);
            property.AreaHectares.Hectares.ShouldBe(newArea);
        }

        [Fact]
        public void Update_WithCoordinates_ShouldSucceed()
        {
            // Arrange
            var property = CreateValidProperty();
            var newLatitude = -22.9068;
            var newLongitude = -43.1729;

            // Act
            var result = property.Update(
                "Fazenda Atualizada",
                "Nova Rua, 456",
                "Rio de Janeiro",
                "RJ",
                "Brazil",
                200.0,
                newLatitude,
                newLongitude);

            // Assert
            result.IsSuccess.ShouldBeTrue();
            property.Location.Latitude.ShouldBe(newLatitude);
            property.Location.Longitude.ShouldBe(newLongitude);
        }

        #endregion

        #region Update - Invalid Cases

        [Fact]
        public void Update_WithEmptyName_ShouldReturnValidationErrors()
        {
            // Arrange
            var property = CreateValidProperty();

            // Act
            var result = property.Update("", "Address", "City", "State", "Country", 100.0);

            // Assert
            result.IsSuccess.ShouldBeFalse();
            result.ValidationErrors.ShouldContain(e => e.Identifier == "Name.Required");
        }

        [Fact]
        public void Update_WithInvalidArea_ShouldReturnValidationErrors()
        {
            // Arrange
            var property = CreateValidProperty();

            // Act
            var result = property.Update("Valid Name", "Address", "City", "State", "Country", -100.0);

            // Assert
            result.IsSuccess.ShouldBeFalse();
            result.ValidationErrors.ShouldContain(e => e.Identifier == "Area.InvalidValue");
        }

        #endregion

        #region Deactivate

        [Fact]
        public void Deactivate_WhenActive_ShouldSucceed()
        {
            // Arrange
            var property = CreateValidProperty();
            property.IsActive.ShouldBeTrue();

            // Act
            var result = property.Deactivate();

            // Assert
            result.IsSuccess.ShouldBeTrue();
            property.IsActive.ShouldBeFalse();
        }

        [Fact]
        public void Deactivate_WhenAlreadyDeactivated_ShouldReturnError()
        {
            // Arrange
            var property = CreateValidProperty();
            property.Deactivate();

            // Act
            var result = property.Deactivate();

            // Assert
            result.IsSuccess.ShouldBeFalse();
            result.ValidationErrors.ShouldContain(e => e.Identifier == "Property.AlreadyDeactivated");
        }

        #endregion

        #region Activate

        [Fact]
        public void Activate_WhenDeactivated_ShouldSucceed()
        {
            // Arrange
            var property = CreateValidProperty();
            property.Deactivate();
            property.IsActive.ShouldBeFalse();

            // Act
            var result = property.Activate();

            // Assert
            result.IsSuccess.ShouldBeTrue();
            property.IsActive.ShouldBeTrue();
        }

        [Fact]
        public void Activate_WhenAlreadyActive_ShouldReturnError()
        {
            // Arrange
            var property = CreateValidProperty();

            // Act
            var result = property.Activate();

            // Assert
            result.IsSuccess.ShouldBeFalse();
            result.ValidationErrors.ShouldContain(e => e.Identifier == "Property.AlreadyActivated");
        }

        #endregion

        #region Helper Methods

        private static PropertyAggregate CreateValidProperty()
        {
            var result = PropertyAggregate.Create(
                "Fazenda São João",
                "Rua Principal, 123",
                "Ribeirão Preto",
                "SP",
                "Brazil",
                150.5,
                Guid.NewGuid());

            return result.Value;
        }

        #endregion
    }
}
