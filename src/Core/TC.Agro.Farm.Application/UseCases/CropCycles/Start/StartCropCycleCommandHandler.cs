namespace TC.Agro.Farm.Application.UseCases.CropCycles.Start
{
    /// <summary>
    /// Handles the StartCropCycleCommand.
    ///
    /// Flow:
    /// 1. Resolve effective ownerId (Admin can specify; Producer uses authenticated identity).
    /// 2. Load the target plot and verify authorization.
    /// 3. Ensure no other active crop cycle exists for the same plot.
    /// 4. Create the crop cycle aggregate via CropCycleAggregate.Start().
    /// 5. Persist via repository and commit via Outbox.
    /// </summary>
    internal sealed class StartCropCycleCommandHandler
        : BaseCommandHandler<StartCropCycleCommand, StartCropCycleResponse, CropCycleAggregate, ICropCycleAggregateRepository>
    {
        private readonly IPlotAggregateRepository _plotRepository;
        private readonly ILogger<StartCropCycleCommandHandler> _logger;

        public StartCropCycleCommandHandler(
            ICropCycleAggregateRepository repository,
            IPlotAggregateRepository plotRepository,
            IUserContext userContext,
            ITransactionalOutbox outbox,
            ILogger<StartCropCycleCommandHandler> logger)
            : base(repository, userContext, outbox, logger)
        {
            _plotRepository = plotRepository ?? throw new ArgumentNullException(nameof(plotRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        protected override async Task<Result<CropCycleAggregate>> MapAsync(
            StartCropCycleCommand command,
            CancellationToken ct)
        {
            var ownerIdResult = ResolveEffectiveOwnerId(command.OwnerId);
            if (!ownerIdResult.IsSuccess)
            {
                return Result<CropCycleAggregate>.Invalid(ownerIdResult.ValidationErrors);
            }

            var effectiveOwnerId = ownerIdResult.Value;

            var plot = await _plotRepository.GetByIdAsync(command.PlotId, ct).ConfigureAwait(false);
            if (plot is null)
            {
                _logger.LogWarning(
                    "StartCropCycle: Plot {PlotId} not found",
                    command.PlotId);
                return Result<CropCycleAggregate>.Invalid(FarmDomainErrors.PlotNotFound);
            }

            if (!UserContext.IsAdmin && plot.OwnerId != effectiveOwnerId)
            {
                _logger.LogWarning(
                    "User {UserId} attempted to start cycle on plot {PlotId} owned by {OwnerId}",
                    UserContext.Id,
                    command.PlotId,
                    plot.OwnerId);
                return Result<CropCycleAggregate>.Unauthorized("You are not authorized to start a crop cycle for this plot.");
            }

            var hasActive = await Repository
                .HasActiveCyclesByPlotAsync(command.PlotId, cancellationToken: ct)
                .ConfigureAwait(false);

            if (hasActive)
            {
                _logger.LogWarning(
                    "StartCropCycle: Plot {PlotId} already has an active crop cycle",
                    command.PlotId);
                return Result<CropCycleAggregate>.Conflict(FarmDomainErrors.CropCycleAlreadyActiveForPlot.ErrorMessage);
            }

            return CropCycleAggregate.Start(
                plotId: command.PlotId,
                propertyId: plot.PropertyId,
                ownerId: effectiveOwnerId,
                cropTypeCatalogId: command.CropTypeCatalogId,
                startedAt: command.StartedAt,
                expectedHarvestDate: command.ExpectedHarvestDate,
                selectedCropTypeSuggestionId: command.SelectedCropTypeSuggestionId,
                status: command.Status,
                notes: command.Notes);
        }

        protected override Task<Result> ValidateAsync(CropCycleAggregate aggregate, CancellationToken ct)
            => Task.FromResult(Result.Success());

        protected override Task<StartCropCycleResponse> BuildResponseAsync(CropCycleAggregate aggregate, CancellationToken ct)
            => Task.FromResult(StartCropCycleMapper.FromAggregate(aggregate));

        private Result<Guid> ResolveEffectiveOwnerId(Guid? requestedOwnerId)
        {
            if (UserContext.IsAdmin)
            {
                if (!requestedOwnerId.HasValue || requestedOwnerId.Value == Guid.Empty)
                {
                    return Result<Guid>.Invalid(new ValidationError(
                        nameof(StartCropCycleCommand.OwnerId),
                        "OwnerId is required when starting a crop cycle on behalf as Admin."));
                }

                return Result.Success(requestedOwnerId.Value);
            }

            return Result.Success(UserContext.Id);
        }
    }
}
