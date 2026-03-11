namespace TC.Agro.Farm.Application.UseCases.CropCycles.Complete
{
    /// <summary>
    /// Validates the CompleteCropCycleCommand.
    /// </summary>
    public sealed class CompleteCropCycleCommandValidator : Validator<CompleteCropCycleCommand>
    {
        private static readonly IReadOnlyCollection<string> TerminalStatuses = [CropCycleStatus.Harvested, CropCycleStatus.Cancelled];

        public CompleteCropCycleCommandValidator()
        {
            RuleFor(x => x.CropCycleId)
                .NotEmpty()
                .WithMessage("Crop cycle ID is required.")
                .WithErrorCode($"{nameof(CompleteCropCycleCommand.CropCycleId)}.Required");

            RuleFor(x => x.EndedAt)
                .NotEmpty()
                .WithMessage("Ended at date is required.")
                .WithErrorCode($"{nameof(CompleteCropCycleCommand.EndedAt)}.Required");

            RuleFor(x => x.FinalStatus)
                .NotEmpty()
                .WithMessage("Final status is required.")
                .WithErrorCode($"{nameof(CompleteCropCycleCommand.FinalStatus)}.Required")
                .Must(status => TerminalStatuses.Contains(status))
                .WithMessage($"Final status must be a terminal status: Harvested or Cancelled.")
                .WithErrorCode($"{nameof(CompleteCropCycleCommand.FinalStatus)}.Invalid");

            RuleFor(x => x.Notes)
                .MaximumLength(1000)
                .WithMessage("Notes must not exceed 1000 characters.")
                .WithErrorCode($"{nameof(CompleteCropCycleCommand.Notes)}.MaximumLength")
                .When(x => x.Notes is not null);
        }
    }
}
