using TC.Agro.Farm.Domain.ValueObjects;

namespace TC.Agro.Farm.Tests.Domain.ValueObjects
{
    public class CropCycleStatusTests
    {
        [Theory]
        [InlineData("planned", CropCycleStatus.Planned)]
        [InlineData("Planted", CropCycleStatus.Planted)]
        [InlineData("GROWING", CropCycleStatus.Growing)]
        [InlineData("Harvesting", CropCycleStatus.Harvesting)]
        [InlineData("Harvested", CropCycleStatus.Harvested)]
        [InlineData("Cancelled", CropCycleStatus.Cancelled)]
        public void Create_WithKnownStatuses_ShouldNormalizeValue(string input, string expected)
        {
            var result = CropCycleStatus.Create(input);

            result.IsSuccess.ShouldBeTrue();
            result.Value.Value.ShouldBe(expected);
        }

        [Fact]
        public void Create_WithUnknownStatus_ShouldReturnValidationError()
        {
            var result = CropCycleStatus.Create("Dormant");

            result.IsSuccess.ShouldBeFalse();
            result.ValidationErrors.ShouldContain(error => error.Identifier == CropCycleStatus.InvalidValue.Identifier);
        }

        [Fact]
        public void GetActiveStatuses_ShouldReturnOnlyActiveLifecycleStatuses()
        {
            var statuses = CropCycleStatus.GetActiveStatuses();

            statuses.ShouldContain(CropCycleStatus.Planned);
            statuses.ShouldContain(CropCycleStatus.Planted);
            statuses.ShouldContain(CropCycleStatus.Growing);
            statuses.ShouldContain(CropCycleStatus.Harvesting);
            statuses.ShouldNotContain(CropCycleStatus.Harvested);
            statuses.ShouldNotContain(CropCycleStatus.Cancelled);
        }
    }
}
