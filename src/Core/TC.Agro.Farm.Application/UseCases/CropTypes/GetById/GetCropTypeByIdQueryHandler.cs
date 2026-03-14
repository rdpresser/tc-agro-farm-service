namespace TC.Agro.Farm.Application.UseCases.CropTypes.GetById
{
    internal sealed class GetCropTypeByIdQueryHandler : BaseQueryHandler<GetCropTypeByIdQuery, GetCropTypeByIdResponse>
    {
        private readonly ICropTypeCatalogReadStore _readStore;
        private readonly ILogger<GetCropTypeByIdQueryHandler> _logger;

        public GetCropTypeByIdQueryHandler(
            ICropTypeCatalogReadStore readStore,
            IUserContext userContext,
            ILogger<GetCropTypeByIdQueryHandler> logger)
        {
            _readStore = readStore ?? throw new ArgumentNullException(nameof(readStore));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public override async Task<Result<GetCropTypeByIdResponse>> ExecuteAsync(
            GetCropTypeByIdQuery query,
            CancellationToken ct = default)
        {
            _logger.LogDebug("Getting crop type entry {CropTypeId}", query.Id);

            var cropType = await _readStore
                .GetByIdAsync(query.Id, query.IncludeInactive, ct)
                .ConfigureAwait(false);

            if (cropType is null)
            {
                _logger.LogWarning("Crop type entry {CropTypeId} not found", query.Id);
                AddError(x => x.Id, "Crop type not found.", FarmDomainErrors.CropTypeCatalogNotFound.ErrorCode);
                return BuildNotFoundResult();
            }

            return Result.Success(cropType);
        }
    }
}
