namespace TC.Agro.Farm.Domain.ValueObjects
{
    public sealed record IrrigationType
    {
        public static readonly ValidationError Required = new("IrrigationType.Required", "Irrigation type is required.");
        public static readonly ValidationError InvalidValue = new("IrrigationType.InvalidValue", "Invalid irrigation type value.");

        public const string DripIrrigation = "Drip Irrigation";
        public const string Sprinkler = "Sprinkler";
        public const string CenterPivot = "Center Pivot";
        public const string FloodFurrow = "Flood/Furrow";
        public const string Rainfed = "Rainfed (No Irrigation)";

        public static readonly string[] ValidTypes =
        [
            DripIrrigation,
            Sprinkler,
            CenterPivot,
            FloodFurrow,
            Rainfed
        ];

        public string Value { get; }

        private IrrigationType(string value)
        {
            Value = value;
        }

        public static Result<IrrigationType> Create(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return Result.Invalid(Required);

            if (!ValidTypes.Contains(value.Trim()))
                return Result.Invalid(InvalidValue);

            return Result.Success(new IrrigationType(value.Trim()));
        }

        public static Result<IrrigationType> FromDb(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return Result.Invalid(Required);

            return Result.Success(new IrrigationType(value));
        }
    }
}
