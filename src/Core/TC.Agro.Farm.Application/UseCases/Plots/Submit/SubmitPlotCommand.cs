namespace TC.Agro.Farm.Application.UseCases.Plots.Submit
{
    public sealed record SubmitPlotCommand : IBaseCommand<SubmitPlotResponse>, IInvalidateCache
    {
        public Guid PlotId { get; init; }
        public Guid PropertyId { get; init; }
        public string Name { get; init; } = string.Empty;
        public string CropType { get; init; } = string.Empty;
        public double AreaHectares { get; init; }
        public double? Latitude { get; init; }
        public double? Longitude { get; init; }
        public string? BoundaryGeoJson { get; init; }
        public DateTimeOffset PlantingDate { get; init; }
        public DateTimeOffset ExpectedHarvestDate { get; init; }
        public string IrrigationType { get; init; } = string.Empty;
        public string? AdditionalNotes { get; init; }
        public Guid? OwnerId { get; init; }
        public Guid? CropTypeCatalogId { get; init; }
        public Guid? SelectedCropTypeSuggestionId { get; init; }

        public bool IsUpdate => PlotId != Guid.Empty;

        public IReadOnlyCollection<string> CacheTags =>
        [
            CacheTagCatalog.Plots,
            CacheTagCatalog.PlotList,
            CacheTagCatalog.PlotById
        ];
    }
}