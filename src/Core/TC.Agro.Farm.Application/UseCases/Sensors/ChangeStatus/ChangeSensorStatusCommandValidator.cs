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
                .WithMessage("Sensor ID is required.");

            RuleFor(x => x.NewStatus)
                .NotEmpty()
                .WithMessage("New status is required.")
                .Must(status => ValidStatuses.Contains(status, StringComparer.OrdinalIgnoreCase))
                .WithMessage($"New status must be one of: {string.Join(", ", ValidStatuses)}");

            RuleFor(x => x.Reason)
                .MaximumLength(500)
                .WithMessage("Reason must not exceed 500 characters.");
        }
    }
}
