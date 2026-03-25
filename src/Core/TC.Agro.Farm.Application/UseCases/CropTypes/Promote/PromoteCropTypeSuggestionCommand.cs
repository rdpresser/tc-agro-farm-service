namespace TC.Agro.Farm.Application.UseCases.CropTypes.Promote
{
    public sealed record PromoteCropTypeSuggestionCommand(
        Guid SuggestionId) : IBaseCommand<PromoteCropTypeSuggestionResponse>, IInvalidateCache
    {
        public IReadOnlyCollection<string> CacheTags =>
        [
            CacheTagCatalog.CropTypes,
            CacheTagCatalog.CropTypeList,
            CacheTagCatalog.CropTypeById
        ];
    }
}