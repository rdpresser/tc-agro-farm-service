namespace TC.Agro.Farm.Application.UseCases.CropTypes.Options
{
    public sealed class ListCropTypeOptionsQueryValidator : Validator<ListCropTypeOptionsQuery>
    {
        public ListCropTypeOptionsQueryValidator()
        {
            RuleFor(x => x.Limit)
                .InclusiveBetween(1, 500)
                .WithMessage("Limit must be between 1 and 500.")
                .WithErrorCode($"{nameof(ListCropTypeOptionsQuery.Limit)}.OutOfRange");
        }
    }
}
