namespace TC.Agro.Farm.Domain.ValueObjects
{
    /// <summary>
    /// Represents the type of sensor (what it measures).
    /// </summary>
    public sealed record SensorType
    {
        public static readonly ValidationError Required = new("SensorType.Required", "Sensor type is required.");
        public static readonly ValidationError InvalidValue = new("SensorType.InvalidValue", "Invalid sensor type value.");

        // Valid sensor types
        public const string Temperature = "Temperature";
        public const string Humidity = "Humidity";
        public const string SoilMoisture = "SoilMoisture";
        public const string Rainfall = "Rainfall";
        public const string WindSpeed = "WindSpeed";
        public const string SolarRadiation = "SolarRadiation";
        public const string Ph = "Ph";

        private static readonly HashSet<string> ValidTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            Temperature,
            Humidity,
            SoilMoisture,
            Rainfall,
            WindSpeed,
            SolarRadiation,
            Ph
        };

        public string Value { get; }

        private SensorType(string value)
        {
            Value = value;
        }

        public static Result<SensorType> Create(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return Result.Invalid(Required);
            }

            var trimmedValue = value.Trim();

            if (!ValidTypes.Contains(trimmedValue))
            {
                return Result.Invalid(InvalidValue);
            }

            // Normalize to proper casing
            string normalizedValue = ValidTypes.First(t => t.Equals(trimmedValue, StringComparison.OrdinalIgnoreCase));

            return Result.Success(new SensorType(normalizedValue));
        }

        /// <summary>
        /// Creates a SensorType from database value without validation.
        /// </summary>
        public static Result<SensorType> FromDb(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return Result.Invalid(Required);
            }

            return Result.Success(new SensorType(value));
        }

        public static IReadOnlyCollection<string> GetValidTypes() => ValidTypes.ToList().AsReadOnly();

        public static implicit operator string(SensorType sensorType) => sensorType.Value;

        public override string ToString() => Value;
    }
}
