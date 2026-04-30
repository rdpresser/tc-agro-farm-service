namespace TC.Agro.Farm.Application.UseCases.CropTypes.Delete
{
    internal sealed class DeleteCropTypeCommandHandler
        : BaseHandler<DeleteCropTypeCommand, DeleteCropTypeResponse>
    {
        private readonly ICropTypeCatalogRepository _repository;
        private readonly IUserContext _userContext;
        private readonly ITransactionalOutbox _outbox;

        public DeleteCropTypeCommandHandler(
            ICropTypeCatalogRepository repository,
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
            var aggregate = _userContext.IsAdmin
                ? await _repository.GetByIdAsync(command.CropTypeId, ct).ConfigureAwait(false)
                : await _repository.GetByIdScopedAsync(command.CropTypeId, _userContext.Id, includeInactive: true, cancellationToken: ct).ConfigureAwait(false);

            if (aggregate is null)
            {
                AddError(x => x.CropTypeId, "Crop type catalog entry not found.", FarmDomainErrors.CropTypeCatalogNotFound.ErrorCode);
                return BuildNotFoundResult();
            }

            if (aggregate.OwnerId != _userContext.Id && !_userContext.IsAdmin)
            {
                AddError(x => x.CropTypeId, "You are not authorized to delete this crop type catalog entry.", "CropTypeCatalog.NotAuthorized");
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
