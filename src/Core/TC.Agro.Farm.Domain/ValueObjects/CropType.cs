namespace TC.Agro.Farm.Domain.ValueObjects
{
    /// <summary>
    /// Represents a crop type for a plot.
    /// </summary>
    public sealed record CropType
    {
        private const int MaxLength = 100;

        public static readonly ValidationError Required = new("CropType.Required", "Crop type is required.");
        public static readonly ValidationError TooLong = new("CropType.TooLong", $"Crop type cannot exceed {MaxLength} characters.");
        public static readonly ValidationError InvalidValue = new("CropType.InvalidValue", "Crop type contains invalid characters.");

        // Common crop types for validation suggestions
        public static readonly string[] CommonCropTypes =
        [
            "Soy",
            "Corn",
            "Wheat",
            "Cotton",
            "Coffee",
            "Sugarcane",
            "Rice",
            "Beans",
            "Potato",
            "Tomato",
            "Lettuce",
            "Carrot",
            "Onion",
            "Orange",
            "Grape",
            "Apple",
            "Banana",
            "Mango",
            "Pasture",
            "Other"
        ];

        private static readonly Regex ValidCropTypeRegex = new(
            @"^[a-zA-ZÀ-ÿ0-9\s\-]+$",
            RegexOptions.Compiled);

        public string Value { get; }

        private CropType(string value)
        {
            Value = value;
        }

        public static Result<CropType> Create(string value)
        {
            var errors = new List<ValidationError>();

            if (string.IsNullOrWhiteSpace(value))
            {
                errors.Add(Required);
            }
            else
            {
                var trimmedValue = value.Trim();

                if (trimmedValue.Length > MaxLength)
                {
                    errors.Add(TooLong);
                }

                if (!ValidCropTypeRegex.IsMatch(trimmedValue))
                {
                    errors.Add(InvalidValue);
                }

                if (errors.Count > 0)
                {
                    return Result.Invalid(errors.ToArray());
                }

                return Result.Success(new CropType(trimmedValue));
            }

            return Result.Invalid(errors.ToArray());
        }

        /// <summary>
        /// Creates a CropType from database value without validation.
        /// </summary>
        public static Result<CropType> FromDb(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return Result.Invalid(Required);
            }

            return Result.Success(new CropType(value));
        }

        public static implicit operator string(CropType cropType) => cropType.Value;

        public override string ToString() => Value;
    }
}
