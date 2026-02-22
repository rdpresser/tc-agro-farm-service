namespace TC.Agro.Farm.Application.UseCases.Sensors.Deactivate
{
    /// <summary>
    /// Validator for DeactivateSensorCommand.
    /// Ensures sensor ID is valid.
    /// </summary>
    public sealed class DeactivateSensorCommandValidator : Validator<DeactivateSensorCommand>
    {
        public DeactivateSensorCommandValidator()
        {
            RuleFor(x => x.SensorId)
                .NotEmpty()
                .WithMessage("Sensor ID is required.")
                .WithErrorCode($"{nameof(DeactivateSensorCommand.SensorId)}.Required");

            RuleFor(x => x.Reason)
                .MaximumLength(500)
                .WithMessage("Reason must not exceed 500 characters.")
                .WithErrorCode($"{nameof(DeactivateSensorCommand.Reason)}.MaximumLength");
        }
    }
}
