namespace TC.Agro.Farm.Domain.ValueObjects
{
    /// <summary>
    /// Represents a property or plot name.
    /// </summary>
    public sealed record Name
    {
        private const int MinLength = 2;
        private const int MaxLength = 200;

        public static readonly ValidationError Required = new("Name.Required", "Name is required.");
        public static readonly ValidationError TooShort = new("Name.TooShort", $"Name must be at least {MinLength} characters.");
        public static readonly ValidationError TooLong = new("Name.TooLong", $"Name cannot exceed {MaxLength} characters.");
        public static readonly ValidationError InvalidFormat = new("Name.InvalidFormat", "Name contains invalid characters.");

        private static readonly Regex ValidNameRegex = new(
            @"^[a-zA-ZÀ-ÿ0-9\s\-\.\,\']+$",
            RegexOptions.Compiled);

        public string Value { get; }

        private Name(string value)
        {
            Value = value;
        }

        public static Result<Name> Create(string value)
        {
            var errors = new List<ValidationError>();

            if (string.IsNullOrWhiteSpace(value))
            {
                errors.Add(Required);
            }
            else
            {
                string trimmed = value.Trim();

                if (trimmed.Length < MinLength)
                {
                    errors.Add(TooShort);
                }

                if (trimmed.Length > MaxLength)
                {
                    errors.Add(TooLong);
                }

                if (!ValidNameRegex.IsMatch(trimmed))
                {
                    errors.Add(InvalidFormat);
                }
            }

            if (errors.Count > 0)
            {
                return Result.Invalid(errors.ToArray());
            }

            return Result.Success(new Name(value.Trim()));
        }

        /// <summary>
        /// Creates a Name from database value without validation.
        /// </summary>
        public static Result<Name> FromDb(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return Result.Invalid(Required);
            }

            return Result.Success(new Name(value));
        }

        public static implicit operator string(Name name) => name.Value;

        public override string ToString() => Value;
    }
}
