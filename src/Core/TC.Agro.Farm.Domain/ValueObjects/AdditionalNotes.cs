namespace TC.Agro.Farm.Domain.ValueObjects
{
    /// <summary>
    /// Optional free-form notes for a plot (e.g. soil, irrigation, observations). Max 1000 characters.
    /// </summary>
    public sealed record AdditionalNotes
    {
        private const int MaxLength = 1000;

        public static readonly ValidationError TooLong = new(
            "AdditionalNotes.TooLong",
            $"Additional notes must not exceed {MaxLength} characters.");

        public string? Value { get; }

        private AdditionalNotes(string? value)
        {
            Value = value;
        }

        public static Result<AdditionalNotes?> Create(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return Result.Success<AdditionalNotes?>(null);

            var trimmed = value.Trim();
            if (trimmed.Length > MaxLength)
                return Result.Invalid(TooLong);

            return Result.Success<AdditionalNotes?>(new AdditionalNotes(trimmed));
        }

        /// <summary>
        /// Creates AdditionalNotes from database value. Accepts null; enforces max length when present.
        /// </summary>
        public static Result<AdditionalNotes?> FromDb(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return Result.Success<AdditionalNotes?>(null);

            if (value.Length > MaxLength)
                return Result.Invalid(TooLong);

            return Result.Success<AdditionalNotes?>(new AdditionalNotes(value));
        }
    }
}
