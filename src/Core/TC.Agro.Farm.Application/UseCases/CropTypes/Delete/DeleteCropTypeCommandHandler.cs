namespace TC.Agro.Farm.Application.UseCases.CropTypes.Delete
{
    internal sealed class DeleteCropTypeCommandHandler
        : BaseHandler<DeleteCropTypeCommand, DeleteCropTypeResponse>
    {
        private readonly ICropTypeSuggestionRepository _repository;
        private readonly IUserContext _userContext;
        private readonly ITransactionalOutbox _outbox;

        public DeleteCropTypeCommandHandler(
            ICropTypeSuggestionRepository repository,
            IUserContext userContext,
            ITransactionalOutbox outbox)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
            _outbox = outbox ?? throw new ArgumentNullException(nameof(outbox));
        }

        public override async Task<Result<DeleteCropTypeResponse>> ExecuteAsync(
            DeleteCropTypeCommand command,
            CancellationToken ct = default)
        {
            var aggregate = await _repository
                .GetByIdAsync(command.CropTypeId, ct)
                .ConfigureAwait(false);

            if (aggregate is null)
            {
                AddError(x => x.CropTypeId, "Crop type suggestion not found.", FarmDomainErrors.CropTypeSuggestionNotFound.ErrorCode);
                return BuildNotFoundResult();
            }

            if (aggregate.OwnerId != _userContext.Id && !_userContext.IsAdmin)
            {
                AddError(x => x.CropTypeId, "You are not authorized to delete this crop type suggestion.", "CropTypeSuggestion.NotAuthorized");
                return BuildNotAuthorizedResult();
            }

            var deactivationResult = aggregate.Deactivate();
            if (!deactivationResult.IsSuccess)
            {
                AddErrors(deactivationResult.ValidationErrors);
                return BuildValidationErrorResult();
            }

            await _outbox.SaveChangesAsync(ct).ConfigureAwait(false);

            return new DeleteCropTypeResponse(
                aggregate.Id,
                aggregate.UpdatedAt ?? DateTimeOffset.UtcNow);
        }
    }
}
