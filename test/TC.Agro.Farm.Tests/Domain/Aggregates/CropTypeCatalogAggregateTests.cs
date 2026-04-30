using TC.Agro.Farm.Domain.Aggregates;

namespace TC.Agro.Farm.Tests.Domain.Aggregates
{
    public class CropTypeCatalogAggregateTests
    {
        [Fact]
        public void Create_WithValidData_ShouldSucceed()
        {
            // Act
            var result = CropTypeCatalogAggregate.Create(
                cropTypeName: "Soy",
                isSystemDefined: true,
                description: "High-demand crop",
                recommendedIrrigationType: "Drip",
                typicalHarvestCycleMonths: 6);

            // Assert
            result.IsSuccess.ShouldBeTrue();
            result.Value.CropTypeName.Value.ShouldBe("Soy");
            result.Value.IsSystemDefined.ShouldBeTrue();
            result.Value.Description.ShouldBe("High-demand crop");
            result.Value.RecommendedIrrigationType.ShouldBe("Drip");
            result.Value.TypicalHarvestCycleMonths.ShouldBe(6);
            result.Value.IsActive.ShouldBeTrue();
        }

        [Fact]
        public void Create_WithEmptyCropTypeName_ShouldReturnValidationError()
        {
            // Act
            var result = CropTypeCatalogAggregate.Create(cropTypeName: string.Empty);

            // Assert
            result.IsSuccess.ShouldBeFalse();
            result.ValidationErrors.ShouldContain(x => x.Identifier == "CropType.Required");
        }

        [Fact]
        public void Create_WithInvalidHarvestCycle_ShouldReturnValidationError()
        {
            // Act
            var result = CropTypeCatalogAggregate.Create(cropTypeName: "Corn", typicalHarvestCycleMonths: 0);

            // Assert
            result.IsSuccess.ShouldBeFalse();
            result.ValidationErrors.ShouldContain(x => x.Identifier == "CropTypeCatalog.TypicalHarvestCycleMonths");
        }

        [Fact]
        public void Create_WithInvalidTemperatureRange_ShouldReturnValidationError()
        {
            // Act
            var result = CropTypeCatalogAggregate.Create(
                cropTypeName: "Rice",
                minTemperature: 35,
                maxTemperature: 20);

            // Assert
            result.IsSuccess.ShouldBeFalse();
            result.ValidationErrors.ShouldContain(x => x.Identifier == "CropTypeCatalog.TemperatureRange");
        }

        [Fact]
        public void Create_WithTenantScopeAndSuggestedImage_ShouldSucceed()
        {
            // Arrange
            var ownerId = Guid.NewGuid();

            // Act
            var result = CropTypeCatalogAggregate.Create(
                cropTypeName: "Dragon Fruit",
                isSystemDefined: false,
                ownerId: ownerId,
                suggestedImage: "dragon");

            // Assert
            result.IsSuccess.ShouldBeTrue();
            result.Value.OwnerId.ShouldBe(ownerId);
            result.Value.SuggestedImage.ShouldBe("dragon");
        }

        [Fact]
        public void Create_WithSystemDefinedCatalogAndOwnerId_ShouldReturnValidationError()
        {
            // Act
            var result = CropTypeCatalogAggregate.Create(
                cropTypeName: "Soy",
                isSystemDefined: true,
                ownerId: Guid.NewGuid());

            // Assert
            result.IsSuccess.ShouldBeFalse();
            result.ValidationErrors.ShouldContain(x => x.Identifier == "CropTypeCatalog.OwnerId");
        }

        [Fact]
        public void Create_WithTenantScopeAndMissingOwnerId_ShouldReturnValidationError()
        {
            // Act
            var result = CropTypeCatalogAggregate.Create(
                cropTypeName: "Soy",
                isSystemDefined: false,
                ownerId: null);

            // Assert
            result.IsSuccess.ShouldBeFalse();
            result.ValidationErrors.ShouldContain(x => x.Identifier == "CropTypeCatalog.OwnerId");
        }

        [Fact]
        public void UpdateMetadata_WithValidData_ShouldSucceed()
        {
            // Arrange
            var aggregate = CreateValidCatalog();

            // Act
            var result = aggregate.UpdateMetadata(
                description: "Updated description",
                recommendedIrrigationType: "Center Pivot",
                typicalHarvestCycleMonths: 8);

            // Assert
            result.IsSuccess.ShouldBeTrue();
            aggregate.Description.ShouldBe("Updated description");
            aggregate.RecommendedIrrigationType.ShouldBe("Center Pivot");
            aggregate.TypicalHarvestCycleMonths.ShouldBe(8);
        }

        [Fact]
        public void Deactivate_WhenAlreadyInactive_ShouldReturnValidationError()
        {
            // Arrange
            var aggregate = CreateValidCatalog();
            aggregate.Deactivate();

            // Act
            var result = aggregate.Deactivate();

            // Assert
            result.IsSuccess.ShouldBeFalse();
            result.ValidationErrors.ShouldContain(x => x.Identifier == "CropTypeCatalog.AlreadyDeactivated");
        }

        [Fact]
        public void Activate_WhenAlreadyActive_ShouldReturnValidationError()
        {
            // Arrange
            var aggregate = CreateValidCatalog();

            // Act
            var result = aggregate.Activate();

            // Assert
            result.IsSuccess.ShouldBeFalse();
            result.ValidationErrors.ShouldContain(x => x.Identifier == "CropTypeCatalog.AlreadyActivated");
        }

        [Fact]
        public void UpdateMetadata_WithSuggestedImageLongerThanTenCharacters_ShouldReturnValidationError()
        {
            // Arrange
            var aggregate = CreateValidCatalog();

            // Act
            var result = aggregate.UpdateMetadata(
                description: null,
                recommendedIrrigationType: null,
                typicalHarvestCycleMonths: null,
                suggestedImage: "image-too-long");

            // Assert
            result.IsSuccess.ShouldBeFalse();
            result.ValidationErrors.ShouldContain(x => x.Identifier == "CropTypeCatalog.SuggestedImage");
        }

        private static CropTypeCatalogAggregate CreateValidCatalog()
        {
            var result = CropTypeCatalogAggregate.Create(
                cropTypeName: "Wheat",
                isSystemDefined: false,
                ownerId: Guid.NewGuid());
            result.IsSuccess.ShouldBeTrue();
            return result.Value;
        }
    }
}
