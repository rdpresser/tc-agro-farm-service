namespace TC.Agro.Farm.Application.UseCases.CropCycles.Start
{
    internal static class StartCropCycleMapper
    {
        public static StartCropCycleResponse FromAggregate(CropCycleAggregate aggregate)
        {
            return new StartCropCycleResponse(
                Id: aggregate.Id,
                PlotId: aggregate.PlotId,
                PropertyId: aggregate.PropertyId,
                CropTypeCatalogId: aggregate.CropTypeCatalogId,
                Status: aggregate.Status.Value,
                StartedAt: aggregate.StartedAt,
                ExpectedHarvestDate: aggregate.ExpectedHarvestDate,
                SelectedCropTypeSuggestionId: aggregate.SelectedCropTypeSuggestionId,
                Notes: aggregate.Notes,
                CreatedAt: aggregate.CreatedAt);
        }
    }
}
