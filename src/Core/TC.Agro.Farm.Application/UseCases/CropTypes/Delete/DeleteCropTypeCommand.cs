namespace TC.Agro.Farm.Application.UseCases.CropTypes.Delete
{
    public sealed record DeleteCropTypeCommand(
        Guid CropTypeId) : IBaseCommand<DeleteCropTypeResponse>, IInvalidateCache
    {
        public IReadOnlyCollection<string> CacheTags =>
        [
            CacheTagCatalog.CropTypes,
            CacheTagCatalog.CropTypeList,
            CacheTagCatalog.CropTypeById
        ];
    }
}
