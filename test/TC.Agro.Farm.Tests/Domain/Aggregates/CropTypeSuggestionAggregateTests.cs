using TC.Agro.Farm.Domain.Aggregates;

namespace TC.Agro.Farm.Tests.Domain.Aggregates
{
    public class CropTypeSuggestionAggregateTests
    {
        [Fact]
        public void CreateManual_WithSuggestedImage_ShouldSucceed()
        {
            var result = CropTypeSuggestionAggregate.CreateManual(
                propertyId: Guid.NewGuid(),
                ownerId: Guid.NewGuid(),
                cropType: "Soy",
                plantingWindow: "September to November",
                harvestCycleMonths: 5,
                suggestedIrrigationType: "Center Pivot",
                minSoilMoisture: 30,
                maxTemperature: 35,
                minHumidity: 45,
                notes: "Manual recommendation",
                suggestedImage: "soy-icon");

            result.IsSuccess.ShouldBeTrue();
            result.Value.Source.ShouldBe(CropTypeSuggestionAggregate.ManualSource);
            result.Value.SuggestedImage.ShouldBe("soy-icon");
        }

        [Fact]
        public void CreateManual_WithSuggestedImageLongerThanTenCharacters_ShouldReturnValidationError()
        {
            var result = CropTypeSuggestionAggregate.CreateManual(
                propertyId: Guid.NewGuid(),
                ownerId: Guid.NewGuid(),
                cropType: "Soy",
                plantingWindow: null,
                harvestCycleMonths: 5,
                suggestedIrrigationType: null,
                minSoilMoisture: null,
                maxTemperature: null,
                minHumidity: null,
                notes: null,
                suggestedImage: "image-too-long");

            result.IsSuccess.ShouldBeFalse();
            result.ValidationErrors.ShouldContain(error => error.Identifier == "CropTypeSuggestion.SuggestedImage");
        }

        [Fact]
        public void CreateAi_WithSuggestedImage_ShouldSucceed()
        {
            var result = CropTypeSuggestionAggregate.CreateAi(
                propertyId: Guid.NewGuid(),
                ownerId: Guid.NewGuid(),
                cropType: "Corn",
                confidenceScore: 92,
                plantingWindow: "October to December",
                harvestCycleMonths: 4,
                suggestedIrrigationType: "Sprinkler",
                minSoilMoisture: 25,
                maxTemperature: 34,
                minHumidity: 40,
                notes: "AI recommendation",
                model: "mock-openai",
                generatedAt: DateTimeOffset.UtcNow,
                suggestedImage: "corn-icon");

            result.IsSuccess.ShouldBeTrue();
            result.Value.Source.ShouldBe(CropTypeSuggestionAggregate.AiSource);
            result.Value.SuggestedImage.ShouldBe("corn-icon");
        }
    }
}
