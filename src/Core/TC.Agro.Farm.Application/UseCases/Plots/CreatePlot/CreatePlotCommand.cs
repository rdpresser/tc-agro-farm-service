namespace TC.Agro.Farm.Application.UseCases.Plots.CreatePlot
{
    /// <summary>
    /// Command to create a new plot within a property.
    /// </summary>
    public sealed record CreatePlotCommand(
        Guid PropertyId,
        string Name,
        string CropType,
        double AreaHectares) : IBaseCommand<CreatePlotResponse>;
}
