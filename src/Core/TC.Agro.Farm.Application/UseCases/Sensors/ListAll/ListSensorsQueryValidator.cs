namespace TC.Agro.Farm.Application.UseCases.Sensors.ListAll
{
    public sealed class ListSensorsQueryValidator : Validator<ListSensorsQuery>
    {
        private static readonly string[] ValidSortBy =
            ["label", "type", "status", "installedat", "createdat"];

        private static readonly string[] ValidSortDirection = ["asc", "desc"];

        public ListSensorsQueryValidator()
        {
            RuleFor(x => x.PageNumber)
                .GreaterThan(0)
                    .WithMessage("Page number must be greater than zero.")
                    .WithErrorCode($"{nameof(ListSensorsQuery.PageNumber)}.Invalid");

            RuleFor(x => x.PageSize)
                .GreaterThan(0)
                    .WithMessage("Page size must be greater than zero.")
                    .WithErrorCode($"{nameof(ListSensorsQuery.PageSize)}.Invalid")
                .LessThanOrEqualTo(100)
                    .WithMessage("Page size must be less than or equal to 100.")
                    .WithErrorCode($"{nameof(ListSensorsQuery.PageSize)}.Max");

            RuleFor(x => x.SortBy)
                .Must(sortBy => string.IsNullOrWhiteSpace(sortBy) || ValidSortBy.Contains(sortBy.Trim().ToLowerInvariant()))
                    .WithMessage($"SortBy must be one of: {string.Join(", ", ValidSortBy)}.")
                    .WithErrorCode($"{nameof(ListSensorsQuery.SortBy)}.Invalid");

            RuleFor(x => x.SortDirection)
                .Must(sortDirection =>
                    string.IsNullOrWhiteSpace(sortDirection) ||
                    ValidSortDirection.Contains(sortDirection.Trim().ToLowerInvariant()))
                    .WithMessage("SortDirection must be 'asc' or 'desc'.")
                    .WithErrorCode($"{nameof(ListSensorsQuery.SortDirection)}.Invalid");

            RuleFor(x => x.Filter)
                .MaximumLength(200)
                    .When(x => !string.IsNullOrWhiteSpace(x.Filter))
                    .WithMessage("Filter must not exceed 200 characters.")
                    .WithErrorCode($"{nameof(ListSensorsQuery.Filter)}.MaxLength");

            RuleFor(x => x.Type)
                .MaximumLength(100)
                    .When(x => !string.IsNullOrWhiteSpace(x.Type))
                    .WithMessage("Type must not exceed 100 characters.")
                    .WithErrorCode($"{nameof(ListSensorsQuery.Type)}.MaxLength");

            RuleFor(x => x.Status)
                .MaximumLength(100)
                    .When(x => !string.IsNullOrWhiteSpace(x.Status))
                    .WithMessage("Status must not exceed 100 characters.")
                    .WithErrorCode($"{nameof(ListSensorsQuery.Status)}.MaxLength");
        }
    }
}
