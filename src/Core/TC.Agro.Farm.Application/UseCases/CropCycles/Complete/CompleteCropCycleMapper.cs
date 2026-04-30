namespace TC.Agro.Farm.Application.UseCases.CropCycles.Complete
{
    internal static class CompleteCropCycleMapper
    {
        public static CompleteCropCycleResponse FromAggregate(CropCycleAggregate aggregate)
        {
            return new CompleteCropCycleResponse(
                Id: aggregate.Id,
                PlotId: aggregate.PlotId,
                PropertyId: aggregate.PropertyId,
                Status: aggregate.Status.Value,
                StartedAt: aggregate.StartedAt,
                EndedAt: aggregate.EndedAt!.Value,
                Notes: aggregate.Notes,
                UpdatedAt: aggregate.UpdatedAt ?? aggregate.CreatedAt);
        }
    }
}
