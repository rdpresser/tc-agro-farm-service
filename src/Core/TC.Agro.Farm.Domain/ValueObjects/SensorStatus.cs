namespace TC.Agro.Farm.Domain.ValueObjects
{
    /// <summary>
    /// Represents the operational status of a sensor.
    /// </summary>
    public sealed record SensorStatus
    {
        public static readonly ValidationError Required = new("SensorStatus.Required", "Sensor status is required.");
        public static readonly ValidationError InvalidValue = new("SensorStatus.InvalidValue", "Invalid sensor status value.");

        // Valid sensor statuses
        public const string Active = "Active";
        public const string Inactive = "Inactive";
        public const string Maintenance = "Maintenance";
        public const string Faulty = "Faulty";

        private static readonly HashSet<string> ValidStatuses = new(StringComparer.OrdinalIgnoreCase)
        {
            Active,
            Inactive,
            Maintenance,
            Faulty
        };

        public string Value { get; }

        private SensorStatus(string value)
        {
            Value = value;
        }

        public static Result<SensorStatus> Create(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return Result.Invalid(Required);
            }

            if (!ValidStatuses.Contains(value))
            {
                return Result.Invalid(InvalidValue);
            }

            // Normalize to proper casing
            string normalizedValue = ValidStatuses.First(s => s.Equals(value, StringComparison.OrdinalIgnoreCase));

            return Result.Success(new SensorStatus(normalizedValue));
        }

        /// <summary>
        /// Creates a SensorStatus from database value without validation.
        /// </summary>
        public static Result<SensorStatus> FromDb(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return Result.Invalid(Required);
            }

            return Result.Success(new SensorStatus(value));
        }

        /// <summary>
        /// Creates an Active status.
        /// </summary>
        public static SensorStatus CreateActive() => new(Active);

        /// <summary>
        /// Creates an Inactive status.
        /// </summary>
        public static SensorStatus CreateInactive() => new(Inactive);

        public bool IsActive => Value == Active;
        public bool IsInactive => Value == Inactive;
        public bool IsMaintenance => Value == Maintenance;
        public bool IsFaulty => Value == Faulty;

        public static IReadOnlyCollection<string> GetValidStatuses() => ValidStatuses;

        public static implicit operator string(SensorStatus status) => status.Value;

        public override string ToString() => Value;
    }
}
