namespace TC.Agro.Farm.Domain.ValueObjects
{
    /// <summary>
    /// Represents a geographic location with address or coordinates.
    /// </summary>
    public sealed record Location
    {
        private const int MaxAddressLength = 500;
        private const int MaxCityLength = 100;
        private const int MaxStateLength = 100;
        private const int MaxCountryLength = 100;

        public static readonly ValidationError AddressRequired = new("Location.AddressRequired", "Address is required.");
        public static readonly ValidationError AddressTooLong = new("Location.AddressTooLong", $"Address cannot exceed {MaxAddressLength} characters.");
        public static readonly ValidationError CityRequired = new("Location.CityRequired", "City is required.");
        public static readonly ValidationError CityTooLong = new("Location.CityTooLong", $"City cannot exceed {MaxCityLength} characters.");
        public static readonly ValidationError StateRequired = new("Location.StateRequired", "State is required.");
        public static readonly ValidationError StateTooLong = new("Location.StateTooLong", $"State cannot exceed {MaxStateLength} characters.");
        public static readonly ValidationError CountryRequired = new("Location.CountryRequired", "Country is required.");
        public static readonly ValidationError CountryTooLong = new("Location.CountryTooLong", $"Country cannot exceed {MaxCountryLength} characters.");
        public static readonly ValidationError InvalidLatitude = new("Location.InvalidLatitude", "Latitude must be between -90 and 90.");
        public static readonly ValidationError InvalidLongitude = new("Location.InvalidLongitude", "Longitude must be between -180 and 180.");

        public string Address { get; }
        public string City { get; }
        public string State { get; }
        public string Country { get; }
        public double? Latitude { get; }
        public double? Longitude { get; }

        private Location(string address, string city, string state, string country, double? latitude, double? longitude)
        {
            Address = address;
            City = city;
            State = state;
            Country = country;
            Latitude = latitude;
            Longitude = longitude;
        }

        public static Result<Location> Create(string address, string city, string state, string country, double? latitude = null, double? longitude = null)
        {
            var errors = new List<ValidationError>();

            var trimmedAddress = address?.Trim() ?? string.Empty;
            var trimmedCity = city?.Trim() ?? string.Empty;
            var trimmedState = state?.Trim() ?? string.Empty;
            var trimmedCountry = country?.Trim() ?? string.Empty;

            // Address validation
            if (string.IsNullOrWhiteSpace(trimmedAddress))
            {
                errors.Add(AddressRequired);
            }
            else if (trimmedAddress.Length > MaxAddressLength)
            {
                errors.Add(AddressTooLong);
            }

            // City validation
            if (string.IsNullOrWhiteSpace(trimmedCity))
            {
                errors.Add(CityRequired);
            }
            else if (trimmedCity.Length > MaxCityLength)
            {
                errors.Add(CityTooLong);
            }

            // State validation
            if (string.IsNullOrWhiteSpace(trimmedState))
            {
                errors.Add(StateRequired);
            }
            else if (trimmedState.Length > MaxStateLength)
            {
                errors.Add(StateTooLong);
            }

            // Country validation
            if (string.IsNullOrWhiteSpace(trimmedCountry))
            {
                errors.Add(CountryRequired);
            }
            else if (trimmedCountry.Length > MaxCountryLength)
            {
                errors.Add(CountryTooLong);
            }

            // Coordinate validation (optional but if provided must be valid)
            if (latitude.HasValue && (latitude.Value < -90 || latitude.Value > 90))
            {
                errors.Add(InvalidLatitude);
            }

            if (longitude.HasValue && (longitude.Value < -180 || longitude.Value > 180))
            {
                errors.Add(InvalidLongitude);
            }

            if (errors.Count > 0)
            {
                return Result.Invalid(errors.ToArray());
            }

            return Result.Success(new Location(trimmedAddress, trimmedCity, trimmedState, trimmedCountry, latitude, longitude));
        }

        /// <summary>
        /// Creates a Location from database values with minimal validation.
        /// Only checks for null/whitespace address, skips length and format validation assuming database integrity.
        /// </summary>
        public static Result<Location> FromDb(string address, string city, string state, string country, double? latitude, double? longitude)
        {
            if (string.IsNullOrWhiteSpace(address))
            {
                return Result.Invalid(AddressRequired);
            }

            return Result.Success(new Location(address, city, state, country, latitude, longitude));
        }

        public override string ToString() => $"{Address}, {City}, {State}, {Country}";
    }
}
