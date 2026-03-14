namespace TC.Agro.Farm.Application.UseCases.CropCycles.Transition
{
    internal static class TransitionCropCycleMapper
    {
        public static TransitionCropCycleResponse FromAggregate(CropCycleAggregate aggregate)
        {
            return new TransitionCropCycleResponse(
                Id: aggregate.Id,
                PlotId: aggregate.PlotId,
                PropertyId: aggregate.PropertyId,
                Status: aggregate.Status.Value,
                EndedAt: aggregate.EndedAt,
                Notes: aggregate.Notes,
                UpdatedAt: aggregate.UpdatedAt ?? aggregate.CreatedAt);
        }
    }
}
