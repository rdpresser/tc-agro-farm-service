namespace TC.Agro.Farm.Application.UseCases.CropCycles.Transition
{
    /// <summary>
    /// Validates the TransitionCropCycleCommand.
    /// </summary>
    public sealed class TransitionCropCycleCommandValidator : Validator<TransitionCropCycleCommand>
    {
        public TransitionCropCycleCommandValidator()
        {
            RuleFor(x => x.CropCycleId)
                .NotEmpty()
                .WithMessage("Crop cycle ID is required.")
                .WithErrorCode($"{nameof(TransitionCropCycleCommand.CropCycleId)}.Required");

            RuleFor(x => x.NewStatus)
                .NotEmpty()
                .WithMessage("New status is required.")
                .WithErrorCode($"{nameof(TransitionCropCycleCommand.NewStatus)}.Required")
                .Must(status => CropCycleStatus.Create(status).IsSuccess && CropCycleStatus.GetActiveStatuses().Contains(status))
                .WithMessage("New status must be an active cycle status: Planned, Planted, Growing, or Harvesting.")
                .WithErrorCode($"{nameof(TransitionCropCycleCommand.NewStatus)}.Invalid");

            RuleFor(x => x.OccurredAt)
                .NotEmpty()
                .WithMessage("OccurredAt date is required.")
                .WithErrorCode($"{nameof(TransitionCropCycleCommand.OccurredAt)}.Required");

            RuleFor(x => x.Notes)
                .MaximumLength(1000)
                .WithMessage("Notes must not exceed 1000 characters.")
                .WithErrorCode($"{nameof(TransitionCropCycleCommand.Notes)}.MaximumLength")
                .When(x => x.Notes is not null);
        }
    }
}
