using System.Globalization;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Options;
using TC.Agro.Farm.Application.Abstractions.Ports;
using TC.Agro.Farm.Service.Options.OpenAi;

namespace TC.Agro.Farm.Service.Providers
{
    internal sealed class OpenAiCropTypeSuggestionProvider : ICropTypeSuggestionAiProvider
    {
        private const int MaxAllowedSuggestions = 30;

        private readonly HttpClient _httpClient;
        private readonly OpenAiCropSuggestionOptions _options;
        private readonly ILogger<OpenAiCropTypeSuggestionProvider> _logger;

        public OpenAiCropTypeSuggestionProvider(
            HttpClient httpClient,
            IOptions<OpenAiCropSuggestionOptions> options,
            ILogger<OpenAiCropTypeSuggestionProvider> logger)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<IReadOnlyList<CropTypeSuggestionDraft>> GenerateSuggestionsAsync(
            CropTypeSuggestionAiRequest request,
            CancellationToken cancellationToken = default)
        {
            if (request is null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            var desiredCount = ResolveSuggestionCount(request.SuggestionCount);

            if (!_options.Enabled)
            {
                return BuildFallbackSuggestions(request, desiredCount);
            }

            if (string.IsNullOrWhiteSpace(_options.ApiKey))
            {
                _logger.LogWarning("OpenAI is enabled but ApiKey is empty. Falling back to deterministic crop suggestions.");
                return BuildFallbackSuggestions(request, desiredCount);
            }

            try
            {
                var payload = BuildRequestPayload(request, desiredCount);
                using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "v1/chat/completions")
                {
                    Content = JsonContent.Create(payload)
                };

                httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);

                using var response = await _httpClient
                    .SendAsync(httpRequest, cancellationToken)
                    .ConfigureAwait(false);

                var responseContent = await response.Content
                    .ReadAsStringAsync(cancellationToken)
                    .ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning(
                        "OpenAI request failed with status {StatusCode}. Falling back to deterministic suggestions.",
                        (int)response.StatusCode);
                    return BuildFallbackSuggestions(request, desiredCount);
                }

                var suggestions = ParseSuggestions(responseContent, desiredCount);
                if (suggestions.Count == 0)
                {
                    _logger.LogWarning("OpenAI returned no valid crop suggestions. Falling back to deterministic suggestions.");
                    return BuildFallbackSuggestions(request, desiredCount);
                }

                return suggestions;
            }
            catch (TaskCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "OpenAI suggestion generation failed. Falling back to deterministic suggestions.");
                return BuildFallbackSuggestions(request, desiredCount);
            }
        }

        private object BuildRequestPayload(CropTypeSuggestionAiRequest request, int suggestionCount)
        {
            var systemPrompt =
                "You are an agronomy assistant for tropical and subtropical farming. " +
                "Return only JSON with an object containing a 'suggestions' array. " +
                "Each suggestion must include: cropType (string), confidenceScore (0-100), plantingWindow (string), " +
                "harvestCycleMonths (integer), suggestedIrrigationType (string), minSoilMoisture (0-100), " +
                "maxTemperature (-30 to 80), minHumidity (0-100), notes (string), suggestedImage (short unicode emoji, max 10 chars).";

            var userPrompt =
                $"Generate {suggestionCount} crop suggestions for a property located in {request.City}, {request.State}, {request.Country}. " +
                $"Coordinates: latitude {request.Latitude.ToString(CultureInfo.InvariantCulture)}, longitude {request.Longitude.ToString(CultureInfo.InvariantCulture)}. " +
                "Prioritize realistic crops for local climate and include practical threshold defaults.";

            return new
            {
                model = string.IsNullOrWhiteSpace(_options.Model) ? "gpt-4o-mini" : _options.Model,
                temperature = Math.Clamp(_options.Temperature, 0, 2),
                response_format = new { type = "json_object" },
                messages = new object[]
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user", content = userPrompt }
                }
            };
        }

        private static IReadOnlyList<CropTypeSuggestionDraft> ParseSuggestions(string content, int maxSuggestions)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                return [];
            }

            using var doc = JsonDocument.Parse(content);
            if (!doc.RootElement.TryGetProperty("choices", out var choicesElement) ||
                choicesElement.ValueKind != JsonValueKind.Array ||
                choicesElement.GetArrayLength() == 0)
            {
                return [];
            }

            var messageElement = choicesElement[0].GetProperty("message");
            if (!messageElement.TryGetProperty("content", out var contentElement))
            {
                return [];
            }

            var jsonPayload = NormalizeJsonString(contentElement.GetString());
            if (string.IsNullOrWhiteSpace(jsonPayload))
            {
                return [];
            }

            using var payloadDoc = JsonDocument.Parse(jsonPayload);

            JsonElement suggestionsElement;
            if (payloadDoc.RootElement.ValueKind == JsonValueKind.Object &&
                payloadDoc.RootElement.TryGetProperty("suggestions", out suggestionsElement))
            {
                if (suggestionsElement.ValueKind != JsonValueKind.Array)
                {
                    return [];
                }
            }
            else if (payloadDoc.RootElement.ValueKind == JsonValueKind.Array)
            {
                suggestionsElement = payloadDoc.RootElement;
            }
            else
            {
                return [];
            }

            var suggestions = new List<CropTypeSuggestionDraft>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var item in suggestionsElement.EnumerateArray())
            {
                var cropType = ReadString(item, "cropType");
                if (string.IsNullOrWhiteSpace(cropType))
                {
                    continue;
                }

                cropType = cropType.Trim();
                if (!seen.Add(cropType))
                {
                    continue;
                }

                suggestions.Add(new CropTypeSuggestionDraft(
                    CropType: cropType,
                    ConfidenceScore: ClampNullable(ReadNullableDouble(item, "confidenceScore"), 0, 100),
                    PlantingWindow: TrimNullable(ReadString(item, "plantingWindow"), 200),
                    HarvestCycleMonths: ClampNullableInt(ReadNullableInt(item, "harvestCycleMonths"), 1, 36),
                    SuggestedIrrigationType: TrimNullable(ReadString(item, "suggestedIrrigationType"), 100),
                    MinSoilMoisture: ClampNullable(ReadNullableDouble(item, "minSoilMoisture"), 0, 100),
                    MaxTemperature: ClampNullable(ReadNullableDouble(item, "maxTemperature"), -30, 80),
                    MinHumidity: ClampNullable(ReadNullableDouble(item, "minHumidity"), 0, 100),
                    Notes: TrimNullable(ReadString(item, "notes"), 500),
                    SuggestedImage: TrimNullable(ReadString(item, "suggestedImage"), 10)));

                if (suggestions.Count >= maxSuggestions)
                {
                    break;
                }
            }

            return suggestions;
        }

        private static IReadOnlyList<CropTypeSuggestionDraft> BuildFallbackSuggestions(
            CropTypeSuggestionAiRequest request,
            int maxSuggestions)
        {
            var absLatitude = Math.Abs(request.Latitude);

            var seeds = absLatitude switch
            {
                <= 15 => new[]
                {
                    ("Banana", "🍌", "Drip Irrigation", 45d, 34d, 60d, 12, "Warm and humid profile."),
                    ("Mango", "🥭", "Drip Irrigation", 35d, 36d, 55d, 8, "Suitable for tropical conditions."),
                    ("Rice", "🌾", "Flood/Furrow", 50d, 34d, 70d, 5, "Good option for wetter zones."),
                    ("Sugarcane", "🎋", "Flood/Furrow", 35d, 37d, 50d, 12, "Performs well with heat and water."),
                    ("Coffee", "☕", "Drip Irrigation", 40d, 30d, 60d, 8, "Viable with mild thermal variation.")
                },
                <= 30 => new[]
                {
                    ("Soy", "🫘", "Center Pivot", 32d, 35d, 45d, 5, "Common in subtropical commercial farms."),
                    ("Corn", "🌽", "Center Pivot", 30d, 35d, 45d, 5, "High adaptability and productivity."),
                    ("Beans", "🫘", "Sprinkler", 35d, 33d, 50d, 3, "Short cycle and diversified rotation."),
                    ("Cotton", "🧵", "Center Pivot", 28d, 38d, 40d, 6, "Performs in warmer and drier periods."),
                    ("Tomato", "🍅", "Drip Irrigation", 40d, 32d, 60d, 4, "Good value crop with managed irrigation.")
                },
                _ => new[]
                {
                    ("Wheat", "🌾", "Rainfed (No Irrigation)", 25d, 30d, 40d, 5, "Suitable for cooler seasonal windows."),
                    ("Potato", "🥔", "Sprinkler", 40d, 28d, 60d, 4, "Performs well in milder temperatures."),
                    ("Apple", "🍎", "Drip Irrigation", 32d, 32d, 45d, 10, "Temperate profile with perennial planning."),
                    ("Grape", "🍇", "Drip Irrigation", 30d, 34d, 45d, 6, "Requires canopy and irrigation management."),
                    ("Onion", "🧅", "Drip Irrigation", 35d, 30d, 50d, 4, "Suitable for cooler and stable cycles.")
                }
            };

            return seeds
                .Take(maxSuggestions)
                .Select((seed, index) => new CropTypeSuggestionDraft(
                    CropType: seed.Item1,
                    ConfidenceScore: Math.Max(60, 92 - (index * 4)),
                    PlantingWindow: "Based on local seasonal profile",
                    HarvestCycleMonths: seed.Item7,
                    SuggestedIrrigationType: seed.Item3,
                    MinSoilMoisture: seed.Item4,
                    MaxTemperature: seed.Item5,
                    MinHumidity: seed.Item6,
                    Notes: seed.Item8,
                    SuggestedImage: seed.Item2))
                .ToList();
        }

        private static int ResolveSuggestionCount(int requested)
            => Math.Clamp(requested <= 0 ? 15 : requested, 1, MaxAllowedSuggestions);

        private static string NormalizeJsonString(string? content)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                return string.Empty;
            }

            var normalized = content.Trim();

            if (normalized.StartsWith("```", StringComparison.Ordinal))
            {
                normalized = normalized.Replace("```json", string.Empty, StringComparison.OrdinalIgnoreCase)
                    .Replace("```", string.Empty, StringComparison.Ordinal)
                    .Trim();
            }

            return normalized;
        }

        private static string? ReadString(JsonElement element, string propertyName)
        {
            if (!element.TryGetProperty(propertyName, out var property) || property.ValueKind == JsonValueKind.Null)
            {
                return null;
            }

            return property.ValueKind switch
            {
                JsonValueKind.String => property.GetString(),
                JsonValueKind.Number => property.GetRawText(),
                JsonValueKind.True => "true",
                JsonValueKind.False => "false",
                _ => null
            };
        }

        private static double? ReadNullableDouble(JsonElement element, string propertyName)
        {
            if (!element.TryGetProperty(propertyName, out var property) || property.ValueKind == JsonValueKind.Null)
            {
                return null;
            }

            if (property.ValueKind == JsonValueKind.Number && property.TryGetDouble(out var numericValue))
            {
                return numericValue;
            }

            if (property.ValueKind == JsonValueKind.String &&
                double.TryParse(property.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var parsedValue))
            {
                return parsedValue;
            }

            return null;
        }

        private static int? ReadNullableInt(JsonElement element, string propertyName)
        {
            if (!element.TryGetProperty(propertyName, out var property) || property.ValueKind == JsonValueKind.Null)
            {
                return null;
            }

            if (property.ValueKind == JsonValueKind.Number && property.TryGetInt32(out var numericValue))
            {
                return numericValue;
            }

            if (property.ValueKind == JsonValueKind.String &&
                int.TryParse(property.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedValue))
            {
                return parsedValue;
            }

            return null;
        }

        private static string? TrimNullable(string? value, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            var trimmed = value.Trim();
            return trimmed.Length <= maxLength
                ? trimmed
                : trimmed[..maxLength];
        }

        private static double? ClampNullable(double? value, double min, double max)
            => value.HasValue ? Math.Clamp(value.Value, min, max) : null;

        private static int? ClampNullableInt(int? value, int min, int max)
            => value.HasValue ? Math.Clamp(value.Value, min, max) : null;
    }
}
