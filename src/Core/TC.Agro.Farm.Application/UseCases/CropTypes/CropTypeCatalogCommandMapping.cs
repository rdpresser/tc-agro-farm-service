using System.Globalization;

namespace TC.Agro.Farm.Application.UseCases.CropTypes
{
    internal static class CropTypeCatalogCommandMapping
    {
        private static readonly Dictionary<string, int> MonthTokenMap = BuildMonthTokenMap();

        public static (int? StartMonth, int? EndMonth) ParsePlantingWindow(string? plantingWindow)
        {
            if (string.IsNullOrWhiteSpace(plantingWindow))
            {
                return (null, null);
            }

            var candidate = plantingWindow.Trim();
            var separators = new[] { " to ", "-", "->", "/", "|" };

            foreach (var separator in separators)
            {
                var parts = candidate.Split(separator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (parts.Length != 2)
                {
                    continue;
                }

                var startMonth = ParseMonthToken(parts[0]);
                var endMonth = ParseMonthToken(parts[1]);

                if (startMonth.HasValue && endMonth.HasValue)
                {
                    return (startMonth.Value, endMonth.Value);
                }
            }

            var singleMonth = ParseMonthToken(candidate);
            return singleMonth.HasValue
                ? (singleMonth.Value, singleMonth.Value)
                : (null, null);
        }

        public static string? BuildPlantingWindow(int? startMonth, int? endMonth)
        {
            if (!startMonth.HasValue || !endMonth.HasValue)
            {
                return null;
            }

            var dateTimeFormat = CultureInfo.InvariantCulture.DateTimeFormat;
            var start = dateTimeFormat.GetAbbreviatedMonthName(startMonth.Value);
            var end = dateTimeFormat.GetAbbreviatedMonthName(endMonth.Value);

            return string.Equals(start, end, StringComparison.OrdinalIgnoreCase)
                ? start
                : $"{start} to {end}";
        }

        private static int? ParseMonthToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return null;
            }

            var normalized = token.Trim().Trim('.', ',', ';').ToLowerInvariant();

            if (int.TryParse(normalized, out var numericMonth) && numericMonth is >= 1 and <= 12)
            {
                return numericMonth;
            }

            return MonthTokenMap.TryGetValue(normalized, out var month)
                ? month
                : null;
        }

        private static Dictionary<string, int> BuildMonthTokenMap()
        {
            var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var format = CultureInfo.InvariantCulture.DateTimeFormat;

            for (var month = 1; month <= 12; month++)
            {
                var full = format.GetMonthName(month).ToLowerInvariant();
                var abbreviated = format.GetAbbreviatedMonthName(month).ToLowerInvariant();

                if (!string.IsNullOrWhiteSpace(full))
                {
                    map[full] = month;
                }

                if (!string.IsNullOrWhiteSpace(abbreviated))
                {
                    map[abbreviated] = month;
                }
            }

            return map;
        }
    }
}
