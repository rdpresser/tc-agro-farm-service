using TC.Agro.SharedKernel.Extensions;

namespace TC.Agro.Farm.Application.UseCases.Sensors.ChangeStatus
{
    /// <summary>
    /// Validator for ChangeSensorStatusCommand.
    /// Ensures sensor ID and new status are valid.
    /// </summary>
    public sealed class ChangeSensorStatusCommandValidator : Validator<ChangeSensorStatusCommand>
    {
        private static readonly string[] ValidStatuses = { "Active", "Inactive", "Maintenance", "Faulty" };

        public ChangeSensorStatusCommandValidator()
        {
            RuleFor(x => x.SensorId)
                .NotEmpty()
                .WithMessage("Sensor ID is required.")
                .WithErrorCode($"{nameof(ChangeSensorStatusCommand.SensorId)}.Required");

            RuleFor(x => x.NewStatus)
                .NotEmpty()
                .WithMessage("New status is required.")
                .WithErrorCode($"{nameof(ChangeSensorStatusCommand.NewStatus)}.Required")
                .Must(status => ValidStatuses.Contains(status, StringComparer.OrdinalIgnoreCase))
                .WithMessage($"New status must be one of: {ValidStatuses.JoinWithQuotes()}")
                .WithErrorCode($"{nameof(ChangeSensorStatusCommand.NewStatus)}.Invalid");

            RuleFor(x => x.Reason)
                .MaximumLength(500)
                .WithMessage("Reason must not exceed 500 characters.")
                .WithErrorCode($"{nameof(ChangeSensorStatusCommand.Reason)}.MaximumLength");
        }
    }
}
