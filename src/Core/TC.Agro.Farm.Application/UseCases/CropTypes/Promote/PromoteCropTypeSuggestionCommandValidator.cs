namespace TC.Agro.Farm.Application.UseCases.CropTypes.Promote
{
    public sealed class PromoteCropTypeSuggestionCommandValidator : Validator<PromoteCropTypeSuggestionCommand>
    {
        public PromoteCropTypeSuggestionCommandValidator()
        {
            RuleFor(x => x.SuggestionId)
                .NotEmpty()
                .WithMessage("SuggestionId is required.")
                .WithErrorCode($"{nameof(PromoteCropTypeSuggestionCommand.SuggestionId)}.Required");
        }
    }
}