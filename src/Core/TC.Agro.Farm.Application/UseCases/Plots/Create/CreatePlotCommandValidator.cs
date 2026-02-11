namespace TC.Agro.Farm.Application.UseCases.Plots.Create
{
    public sealed class CreatePlotCommandValidator : Validator<CreatePlotCommand>
    {
        public CreatePlotCommandValidator()
        {
            #region PropertyId | Validation Rules
            RuleFor(x => x.PropertyId)
                .NotEmpty()
                    .WithMessage("Property Id is required.")
                    .WithErrorCode($"{nameof(CreatePlotCommand.PropertyId)}.Required");
            #endregion

            #region Name | Validation Rules
            RuleFor(x => x.Name)
                .NotEmpty()
                    .WithMessage("Name is required.")
                    .WithErrorCode($"{nameof(CreatePlotCommand.Name)}.Required")
                .MinimumLength(3)
                    .WithMessage("Name must be at least 3 characters long.")
                    .WithErrorCode($"{nameof(CreatePlotCommand.Name)}.MinimumLength")
                .MaximumLength(200)
                    .WithMessage("Name must not exceed 200 characters.")
                    .WithErrorCode($"{nameof(CreatePlotCommand.Name)}.MaximumLength");
            #endregion

            #region CropType | Validation Rules (MANDATORY per hackathon requirement)
            RuleFor(x => x.CropType)
                .NotEmpty()
                    .WithMessage("Crop type is mandatory.")
                    .WithErrorCode($"{nameof(CreatePlotCommand.CropType)}.Required")
                .MinimumLength(2)
                    .WithMessage("Crop type must be at least 2 characters long.")
                    .WithErrorCode($"{nameof(CreatePlotCommand.CropType)}.MinimumLength")
                .MaximumLength(100)
                    .WithMessage("Crop type must not exceed 100 characters.")
                    .WithErrorCode($"{nameof(CreatePlotCommand.CropType)}.MaximumLength");
            #endregion

            #region AreaHectares | Validation Rules
            RuleFor(x => x.AreaHectares)
                .GreaterThan(0)
                    .WithMessage("Area must be greater than zero.")
                    .WithErrorCode($"{nameof(CreatePlotCommand.AreaHectares)}.GreaterThanZero")
                .LessThanOrEqualTo(100_000)
                    .WithMessage("Area must not exceed 100,000 hectares for a single plot.")
                    .WithErrorCode($"{nameof(CreatePlotCommand.AreaHectares)}.MaximumValue");
            #endregion
        }
    }
}
