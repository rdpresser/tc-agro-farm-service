namespace TC.Agro.Farm.Application.UseCases.CropCycles.Transition
{
    /// <summary>
    /// Handles the TransitionCropCycleCommand.
    ///
    /// Flow:
    /// 1. Load the crop cycle aggregate from repository.
    /// 2. Verify the caller is owner or admin.
    /// 3. Apply aggregate.TransitionTo() (domain responsibility).
    /// 4. Skip Repository.Add() — aggregate already tracked by EF Core.
    /// 5. Commit via Outbox.
    /// </summary>
    internal sealed class TransitionCropCycleCommandHandler
        : BaseCommandHandler<TransitionCropCycleCommand, TransitionCropCycleResponse, CropCycleAggregate, ICropCycleAggregateRepository>
    {
        private readonly ILogger<TransitionCropCycleCommandHandler> _logger;

        public TransitionCropCycleCommandHandler(
            ICropCycleAggregateRepository repository,
            IUserContext userContext,
            ITransactionalOutbox outbox,
            ILogger<TransitionCropCycleCommandHandler> logger)
            : base(repository, userContext, outbox, logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        protected override async Task<Result<CropCycleAggregate>> MapAsync(
            TransitionCropCycleCommand command,
            CancellationToken ct)
        {
            var cropCycle = await Repository.GetByIdAsync(command.CropCycleId, ct).ConfigureAwait(false);
            if (cropCycle is null)
            {
                _logger.LogWarning("TransitionCropCycle: Cycle {CycleId} not found", command.CropCycleId);
                return Result<CropCycleAggregate>.Invalid(FarmDomainErrors.CropCycleNotFound);
            }

            if (!UserContext.IsAdmin && cropCycle.OwnerId != UserContext.Id)
            {
                _logger.LogWarning(
                    "User {UserId} attempted to transition crop cycle {CycleId} owned by {OwnerId}",
                    UserContext.Id,
                    command.CropCycleId,
                    cropCycle.OwnerId);
                return Result<CropCycleAggregate>.Unauthorized("You are not authorized to transition this crop cycle.");
            }

            var transitionResult = cropCycle.TransitionTo(command.NewStatus, command.OccurredAt, command.Notes);
            if (!transitionResult.IsSuccess)
            {
                return Result<CropCycleAggregate>.Invalid(transitionResult.ValidationErrors);
            }

            return Result.Success(cropCycle);
        }

        protected override Task PersistAsync(CropCycleAggregate aggregate, CancellationToken ct)
            => Task.CompletedTask; // EF change tracker handles the update

        protected override Task<Result> ValidateAsync(CropCycleAggregate aggregate, CancellationToken ct)
            => Task.FromResult(Result.Success());

        protected override Task<TransitionCropCycleResponse> BuildResponseAsync(CropCycleAggregate aggregate, CancellationToken ct)
            => Task.FromResult(TransitionCropCycleMapper.FromAggregate(aggregate));
    }
}
