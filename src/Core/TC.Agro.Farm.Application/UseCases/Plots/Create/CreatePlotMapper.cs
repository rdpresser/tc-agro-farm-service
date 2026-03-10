namespace TC.Agro.Farm.Application.UseCases.Plots.Create
{
    public static class CreatePlotMapper
    {
        public static Result<PlotAggregate> ToAggregate(
            CreatePlotCommand command,
            Guid ownerId,
            string resolvedCropType,
            Guid cropTypeCatalogId,
            Guid? selectedCropTypeSuggestionId)
        {
            return PlotAggregate.Create(
                command.PropertyId,
                ownerId,
                command.Name,
                resolvedCropType,
                command.AreaHectares,
                command.PlantingDate,
                command.ExpectedHarvestDate,
                command.IrrigationType,
                command.AdditionalNotes,
                command.Latitude,
                command.Longitude,
                command.BoundaryGeoJson,
                cropTypeCatalogId,
                selectedCropTypeSuggestionId);
        }

        public static CreatePlotResponse FromAggregate(PlotAggregate aggregate)
        {
            return new CreatePlotResponse(
                Id: aggregate.Id,
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
                IsActive: aggregate.IsActive,
                CreatedAt: aggregate.CreatedAt,
                CropTypeCatalogId: aggregate.CropTypeCatalogId,
                SelectedCropTypeSuggestionId: aggregate.SelectedCropTypeSuggestionId);
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
                domainEvent.OccurredOn,
                domainEvent.CropTypeCatalogId,
                domainEvent.SelectedCropTypeSuggestionId);
    }
}
