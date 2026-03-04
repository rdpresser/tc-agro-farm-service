namespace TC.Agro.Farm.Application.UseCases.Plots.Create
{
    public static class CreatePlotMapper
    {
        public static Result<PlotAggregate> ToAggregate(CreatePlotCommand command, Guid ownerId)
        {
            return PlotAggregate.Create(
                command.PropertyId,
                ownerId,
                command.Name,
                command.CropType,
                command.AreaHectares,
                command.PlantingDate,
                command.ExpectedHarvestDate,
                command.IrrigationType,
                command.AdditionalNotes,
                command.Latitude,
                command.Longitude,
                command.BoundaryGeoJson);
        }

        public static CreatePlotResponse FromAggregate(PlotAggregate aggregate)
        {
            return new CreatePlotResponse(
                Id: aggregate.Id,
                PropertyId: aggregate.PropertyId,
                Name: aggregate.Name.Value,
                CropType: aggregate.CropType.Value,
                AreaHectares: aggregate.AreaHectares.Hectares,
                Latitude: aggregate.Latitude,
                Longitude: aggregate.Longitude,
                PlantingDate: aggregate.PlantingDate,
                ExpectedHarvestDate: aggregate.ExpectedHarvestDate,
                IrrigationType: aggregate.IrrigationType.Value,
                AdditionalNotes: aggregate.AdditionalNotes?.Value,
                IsActive: aggregate.IsActive,
                CreatedAt: aggregate.CreatedAt);
        }

        public static PlotCreatedIntegrationEvent ToIntegrationEvent(PlotCreatedDomainEvent domainEvent)
            => new(
                domainEvent.AggregateId,
                domainEvent.PropertyId,
                domainEvent.Name,
                domainEvent.CropType,
                domainEvent.AreaHectares,
                domainEvent.PlantingDate,
                domainEvent.ExpectedHarvestDate,
                domainEvent.IrrigationType,
                domainEvent.AdditionalNotes ?? string.Empty,
                domainEvent.OccurredOn);
    }
}
