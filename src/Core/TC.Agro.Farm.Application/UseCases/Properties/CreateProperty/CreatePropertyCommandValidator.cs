namespace TC.Agro.Farm.Application.UseCases.Properties.CreateProperty
{
    public sealed class CreatePropertyCommandValidator : Validator<CreatePropertyCommand>
    {
        public CreatePropertyCommandValidator()
        {
            #region Name | Validation Rules
            RuleFor(x => x.Name)
                .NotEmpty()
                    .WithMessage("Name is required.")
                    .WithErrorCode($"{nameof(CreatePropertyCommand.Name)}.Required")
                .MinimumLength(3)
                    .WithMessage("Name must be at least 3 characters long.")
                    .WithErrorCode($"{nameof(CreatePropertyCommand.Name)}.MinimumLength")
                .MaximumLength(200)
                    .WithMessage("Name must not exceed 200 characters.")
                    .WithErrorCode($"{nameof(CreatePropertyCommand.Name)}.MaximumLength");
            #endregion

            #region Address | Validation Rules
            RuleFor(x => x.Address)
                .NotEmpty()
                    .WithMessage("Address is required.")
                    .WithErrorCode($"{nameof(CreatePropertyCommand.Address)}.Required")
                .MaximumLength(500)
                    .WithMessage("Address must not exceed 500 characters.")
                    .WithErrorCode($"{nameof(CreatePropertyCommand.Address)}.MaximumLength");
            #endregion

            #region City | Validation Rules
            RuleFor(x => x.City)
                .NotEmpty()
                    .WithMessage("City is required.")
                    .WithErrorCode($"{nameof(CreatePropertyCommand.City)}.Required")
                .MaximumLength(100)
                    .WithMessage("City must not exceed 100 characters.")
                    .WithErrorCode($"{nameof(CreatePropertyCommand.City)}.MaximumLength");
            #endregion

            #region State | Validation Rules
            RuleFor(x => x.State)
                .NotEmpty()
                    .WithMessage("State is required.")
                    .WithErrorCode($"{nameof(CreatePropertyCommand.State)}.Required")
                .MaximumLength(100)
                    .WithMessage("State must not exceed 100 characters.")
                    .WithErrorCode($"{nameof(CreatePropertyCommand.State)}.MaximumLength");
            #endregion

            #region Country | Validation Rules
            RuleFor(x => x.Country)
                .NotEmpty()
                    .WithMessage("Country is required.")
                    .WithErrorCode($"{nameof(CreatePropertyCommand.Country)}.Required")
                .MaximumLength(100)
                    .WithMessage("Country must not exceed 100 characters.")
                    .WithErrorCode($"{nameof(CreatePropertyCommand.Country)}.MaximumLength");
            #endregion

            #region AreaHectares | Validation Rules
            RuleFor(x => x.AreaHectares)
                .GreaterThan(0)
                    .WithMessage("Area must be greater than zero.")
                    .WithErrorCode($"{nameof(CreatePropertyCommand.AreaHectares)}.GreaterThanZero")
                .LessThanOrEqualTo(1_000_000)
                    .WithMessage("Area must not exceed 1,000,000 hectares.")
                    .WithErrorCode($"{nameof(CreatePropertyCommand.AreaHectares)}.MaximumValue");
            #endregion

            #region Latitude | Validation Rules (Optional)
            RuleFor(x => x.Latitude)
                .InclusiveBetween(-90, 90)
                    .When(x => x.Latitude.HasValue)
                    .WithMessage("Latitude must be between -90 and 90.")
                    .WithErrorCode($"{nameof(CreatePropertyCommand.Latitude)}.Range");
            #endregion

            #region Longitude | Validation Rules (Optional)
            RuleFor(x => x.Longitude)
                .InclusiveBetween(-180, 180)
                    .When(x => x.Longitude.HasValue)
                    .WithMessage("Longitude must be between -180 and 180.")
                    .WithErrorCode($"{nameof(CreatePropertyCommand.Longitude)}.Range");
            #endregion
        }
    }
}
