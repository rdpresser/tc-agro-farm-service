namespace TC.Agro.Farm.Application.UseCases.Sensors.RegisterSensor
{
    public sealed class RegisterSensorCommandValidator : Validator<RegisterSensorCommand>
    {
        private static readonly string[] ValidSensorTypes =
            ["Temperature", "Humidity", "SoilMoisture", "Rainfall", "WindSpeed", "SolarRadiation"];

        public RegisterSensorCommandValidator()
        {
            #region PlotId | Validation Rules
            RuleFor(x => x.PlotId)
                .NotEmpty()
                    .WithMessage("Plot Id is required.")
                    .WithErrorCode($"{nameof(RegisterSensorCommand.PlotId)}.Required");
            #endregion

            #region Type | Validation Rules
            RuleFor(x => x.Type)
                .NotEmpty()
                    .WithMessage("Sensor type is required.")
                    .WithErrorCode($"{nameof(RegisterSensorCommand.Type)}.Required")
                .Must(type => ValidSensorTypes.Contains(type))
                    .WithMessage($"Sensor type must be one of: {string.Join(", ", ValidSensorTypes)}.")
                    .WithErrorCode($"{nameof(RegisterSensorCommand.Type)}.InvalidType");
            #endregion

            #region Label | Validation Rules (Optional)
            RuleFor(x => x.Label)
                .MinimumLength(3)
                    .When(x => !string.IsNullOrWhiteSpace(x.Label))
                    .WithMessage("Label must be at least 3 characters long.")
                    .WithErrorCode($"{nameof(RegisterSensorCommand.Label)}.MinimumLength")
                .MaximumLength(100)
                    .When(x => !string.IsNullOrWhiteSpace(x.Label))
                    .WithMessage("Label must not exceed 100 characters.")
                    .WithErrorCode($"{nameof(RegisterSensorCommand.Label)}.MaximumLength");
            #endregion
        }
    }
}
