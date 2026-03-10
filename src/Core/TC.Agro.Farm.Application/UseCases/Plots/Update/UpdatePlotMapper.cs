namespace TC.Agro.Farm.Application.UseCases.Plots.Update
{
    internal static class UpdatePlotMapper
    {
        public static UpdatePlotResponse FromAggregate(PlotAggregate aggregate)
        {
            return new UpdatePlotResponse(
                PlotId: aggregate.Id,
                PropertyId: aggregate.PropertyId,
                Name: aggregate.Name.Value,
                CropType: aggregate.CropTypeDisplayName,
                AreaHectares: aggregate.AreaHectares.Hectares,
                Latitude: aggregate.Latitude,
                Longitude: aggregate.Longitude,
                PlantingDate: aggregate.PlantingDate,
                ExpectedHarvestDate: aggregate.ExpectedHarvestDate,
                IrrigationType: aggregate.IrrigationType.Value,
                AdditionalNotes: aggregate.AdditionalNotes?.Value,
                UpdatedAt: aggregate.UpdatedAt,
                CropTypeCatalogId: aggregate.CropTypeCatalogId,
                SelectedCropTypeSuggestionId: aggregate.SelectedCropTypeSuggestionId);
        }
    }
}
