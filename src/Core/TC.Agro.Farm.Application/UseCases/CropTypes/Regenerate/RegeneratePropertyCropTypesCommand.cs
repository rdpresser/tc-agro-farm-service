namespace TC.Agro.Farm.Application.UseCases.CropTypes.Regenerate
{
    public sealed record RegeneratePropertyCropTypesCommand(
        Guid PropertyId) : IBaseCommand<RegeneratePropertyCropTypesResponse>, IInvalidateCache
    {
        public IReadOnlyCollection<string> CacheTags =>
        [
            CacheTagCatalog.CropTypes,
            CacheTagCatalog.CropTypeList,
            CacheTagCatalog.CropTypeById
        ];
    }
}
