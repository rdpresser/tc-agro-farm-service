namespace TC.Agro.Farm.Domain.ValueObjects
{
    /// <summary>
    /// Represents an area measurement in hectares.
    /// </summary>
    public sealed record Area
    {
        private const double MinValue = 0.01; // Minimum 0.01 hectare (100 m²)
        private const double MaxValue = 1_000_000; // Maximum 1 million hectares

        public static readonly ValidationError TooSmall = new("Area.TooSmall", $"Area must be at least {MinValue} hectares.");
        public static readonly ValidationError TooLarge = new("Area.TooLarge", $"Area cannot exceed {MaxValue:N0} hectares.");
        public static readonly ValidationError InvalidValue = new("Area.InvalidValue", "Area must be a positive number.");

        public double Hectares { get; }

        private Area(double hectares)
        {
            Hectares = hectares;
        }

        public static Result<Area> Create(double hectares)
        {
            if (double.IsNaN(hectares) || double.IsInfinity(hectares))
            {
                return Result.Invalid(InvalidValue);
            }

            if (hectares <= 0)
            {
                return Result.Invalid(InvalidValue);
            }

            if (hectares < MinValue)
            {
                return Result.Invalid(TooSmall);
            }

            if (hectares > MaxValue)
            {
                return Result.Invalid(TooLarge);
            }

            return Result.Success(new Area(Math.Round(hectares, 4)));
        }

        /// <summary>
        /// Creates an Area from database value without validation.
        /// </summary>
        public static Result<Area> FromDb(double hectares)
        {
            if (hectares <= 0)
            {
                return Result.Invalid(InvalidValue);
            }

            return Result.Success(new Area(hectares));
        }

        /// <summary>
        /// Converts hectares to square meters.
        /// </summary>
        public double ToSquareMeters() => Hectares * 10_000;

        /// <summary>
        /// Converts hectares to acres.
        /// </summary>
        public double ToAcres() => Hectares * 2.47105;

        public static implicit operator double(Area area) => area.Hectares;

        public override string ToString() => $"{Hectares:N2} ha";
    }
}
