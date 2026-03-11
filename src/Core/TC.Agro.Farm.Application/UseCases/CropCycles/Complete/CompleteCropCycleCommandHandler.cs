namespace TC.Agro.Farm.Application.UseCases.CropCycles.Complete
{
    /// <summary>
    /// Handles the CompleteCropCycleCommand.
    ///
    /// Flow:
    /// 1. Load the crop cycle aggregate from repository.
    /// 2. Verify the caller is owner or admin.
    /// 3. Apply aggregate.Complete() (domain responsibility).
    /// 4. Skip Repository.Add() — aggregate already tracked by EF Core.
    /// 5. Commit via Outbox.
    /// </summary>
    internal sealed class CompleteCropCycleCommandHandler
        : BaseCommandHandler<CompleteCropCycleCommand, CompleteCropCycleResponse, CropCycleAggregate, ICropCycleAggregateRepository>
    {
        private readonly ILogger<CompleteCropCycleCommandHandler> _logger;

        public CompleteCropCycleCommandHandler(
            ICropCycleAggregateRepository repository,
            IUserContext userContext,
            ITransactionalOutbox outbox,
            ILogger<CompleteCropCycleCommandHandler> logger)
            : base(repository, userContext, outbox, logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        protected override async Task<Result<CropCycleAggregate>> MapAsync(
            CompleteCropCycleCommand command,
            CancellationToken ct)
        {
            var cropCycle = await Repository.GetByIdAsync(command.CropCycleId, ct).ConfigureAwait(false);
            if (cropCycle is null)
            {
                _logger.LogWarning("CompleteCropCycle: Cycle {CycleId} not found", command.CropCycleId);
                return Result<CropCycleAggregate>.Invalid(FarmDomainErrors.CropCycleNotFound);
            }

            if (!UserContext.IsAdmin && cropCycle.OwnerId != UserContext.Id)
            {
                _logger.LogWarning(
                    "User {UserId} attempted to complete crop cycle {CycleId} owned by {OwnerId}",
                    UserContext.Id,
                    command.CropCycleId,
                    cropCycle.OwnerId);
                return Result<CropCycleAggregate>.Unauthorized("You are not authorized to complete this crop cycle.");
            }

            var completeResult = cropCycle.Complete(command.EndedAt, command.Notes, command.FinalStatus);
            if (!completeResult.IsSuccess)
            {
                return Result<CropCycleAggregate>.Invalid(completeResult.ValidationErrors);
            }

            return Result.Success(cropCycle);
        }

        protected override Task PersistAsync(CropCycleAggregate aggregate, CancellationToken ct)
            => Task.CompletedTask; // EF change tracker handles the update

        protected override Task<Result> ValidateAsync(CropCycleAggregate aggregate, CancellationToken ct)
            => Task.FromResult(Result.Success());

        protected override Task<CompleteCropCycleResponse> BuildResponseAsync(CropCycleAggregate aggregate, CancellationToken ct)
            => Task.FromResult(CompleteCropCycleMapper.FromAggregate(aggregate));
    }
}
