namespace TC.Agro.Farm.Application.UseCases.Plots.Update
{
    internal sealed class UpdatePlotCommandHandler
        : BaseHandler<UpdatePlotCommand, UpdatePlotResponse>
    {
        private readonly IPlotAggregateRepository _repository;
        private readonly IUserContext _userContext;
        private readonly ITransactionalOutbox _outbox;
        private readonly ILogger<UpdatePlotCommandHandler> _logger;

        public UpdatePlotCommandHandler(
            IPlotAggregateRepository repository,
            IUserContext userContext,
            ITransactionalOutbox outbox,
            ILogger<UpdatePlotCommandHandler> logger)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
            _outbox = outbox ?? throw new ArgumentNullException(nameof(outbox));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public override async Task<Result<UpdatePlotResponse>> ExecuteAsync(
            UpdatePlotCommand command,
            CancellationToken ct = default)
        {
            _logger.LogInformation(
                "Updating plot {PlotId} by user {UserId}",
                command.PlotId,
                _userContext.Id);

            var aggregate = await _repository.GetByIdAsync(command.PlotId, ct).ConfigureAwait(false);
            if (aggregate is null)
            {
                _logger.LogWarning("Plot {PlotId} not found", command.PlotId);
                AddError(x => x.PlotId, "Plot not found.", FarmDomainErrors.PlotNotFound.ErrorCode);
                return BuildNotFoundResult();
            }

            if (aggregate.OwnerId != _userContext.Id && !_userContext.IsAdmin)
            {
                _logger.LogWarning(
                    "User {UserId} attempted to update plot {PlotId} owned by {OwnerId}",
                    _userContext.Id,
                    command.PlotId,
                    aggregate.OwnerId);
                AddError(x => x.PlotId, "You are not authorized to update this plot.", "Plot.NotAuthorized");
                return BuildNotAuthorizedResult();
            }

            var nameExists = await _repository
                .NameExistsForPropertyExcludingAsync(command.Name, aggregate.PropertyId, aggregate.Id, ct)
                .ConfigureAwait(false);

            if (nameExists)
            {
                AddError(x => x.Name,
                    $"A plot with name '{command.Name}' already exists for this property.",
                    "Name.Duplicate");
                return BuildValidationErrorResult();
            }

            var updateResult = aggregate.Update(
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

            if (!updateResult.IsSuccess)
            {
                AddErrors(updateResult.ValidationErrors);
                return BuildValidationErrorResult();
            }

            await _outbox.SaveChangesAsync(ct).ConfigureAwait(false);

            _logger.LogInformation("Plot {PlotId} updated successfully", aggregate.Id);

            return UpdatePlotMapper.FromAggregate(aggregate);
        }
    }
}
