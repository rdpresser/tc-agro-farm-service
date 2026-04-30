namespace TC.Agro.Farm.Application.UseCases.CropTypes.Regenerate
{
    public sealed class RegeneratePropertyCropTypesCommandValidator : Validator<RegeneratePropertyCropTypesCommand>
    {
        public RegeneratePropertyCropTypesCommandValidator()
        {
            RuleFor(x => x.PropertyId)
                .NotEmpty()
                .WithMessage("PropertyId is required.")
                .WithErrorCode($"{nameof(RegeneratePropertyCropTypesCommand.PropertyId)}.Required");
        }
    }
}
