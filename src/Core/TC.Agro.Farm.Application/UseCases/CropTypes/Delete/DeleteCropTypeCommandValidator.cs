namespace TC.Agro.Farm.Application.UseCases.CropTypes.Delete
{
    public sealed class DeleteCropTypeCommandValidator : Validator<DeleteCropTypeCommand>
    {
        public DeleteCropTypeCommandValidator()
        {
            RuleFor(x => x.CropTypeId)
                .NotEmpty()
                .WithMessage("CropTypeId is required.")
                .WithErrorCode($"{nameof(DeleteCropTypeCommand.CropTypeId)}.Required");
        }
    }
}
