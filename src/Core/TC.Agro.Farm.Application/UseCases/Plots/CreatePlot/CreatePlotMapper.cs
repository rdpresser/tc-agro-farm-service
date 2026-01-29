namespace TC.Agro.Farm.Application.UseCases.Plots.CreatePlot
{
    public static class CreatePlotMapper
    {
        public static Result<PlotAggregate> ToAggregate(CreatePlotCommand command)
        {
            return PlotAggregate.Create(
                command.PropertyId,
                command.Name,
                command.CropType,
                command.AreaHectares);
        }

        public static CreatePlotResponse FromAggregate(PlotAggregate aggregate)
        {
            return new CreatePlotResponse(
                Id: aggregate.Id,
                PropertyId: aggregate.PropertyId,
                Name: aggregate.Name.Value,
                CropType: aggregate.CropType.Value,
                AreaHectares: aggregate.AreaHectares.Hectares,
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
                domainEvent.OccurredOn);
    }
}
