namespace TC.Agro.Farm.Domain.ValueObjects
{
    /// <summary>
    /// Represents the lifecycle status of a crop cycle.
    /// </summary>
    public sealed record CropCycleStatus
    {
        public static readonly ValidationError Required = new("CropCycleStatus.Required", "Crop cycle status is required.");
        public static readonly ValidationError InvalidValue = new("CropCycleStatus.InvalidValue", "Invalid crop cycle status value.");

        public const string Planned = "Planned";
        public const string Planted = "Planted";
        public const string Growing = "Growing";
        public const string Harvesting = "Harvesting";
        public const string Harvested = "Harvested";
        public const string Cancelled = "Cancelled";

        private static readonly HashSet<string> ValidStatuses = new(StringComparer.OrdinalIgnoreCase)
        {
            Planned,
            Planted,
            Growing,
            Harvesting,
            Harvested,
            Cancelled
        };

        private static readonly HashSet<string> ActiveStatuses = new(StringComparer.OrdinalIgnoreCase)
        {
            Planned,
            Planted,
            Growing,
            Harvesting
        };

        public string Value { get; }

        private CropCycleStatus(string value)
        {
            Value = value;
        }

        public static Result<CropCycleStatus> Create(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return Result.Invalid(Required);
            }

            var trimmedValue = value.Trim();

            if (!ValidStatuses.Contains(trimmedValue))
            {
                return Result.Invalid(InvalidValue);
            }

            var normalizedValue = ValidStatuses.First(status =>
                status.Equals(trimmedValue, StringComparison.OrdinalIgnoreCase));

            return Result.Success(new CropCycleStatus(normalizedValue));
        }

        public static Result<CropCycleStatus> FromDb(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return Result.Invalid(Required);
            }

            return Result.Success(new CropCycleStatus(value));
        }

        public bool IsActiveCycle => ActiveStatuses.Contains(Value);

        public static IReadOnlyCollection<string> GetActiveStatuses()
            => ActiveStatuses.ToList().AsReadOnly();

        public static implicit operator string(CropCycleStatus status) => status.Value;

        public override string ToString() => Value;
    }
}
