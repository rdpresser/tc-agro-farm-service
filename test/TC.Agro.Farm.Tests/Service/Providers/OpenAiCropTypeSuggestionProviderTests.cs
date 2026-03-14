using System.Net;
using System.Text;
using System.Text.Json;
using FakeItEasy;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TC.Agro.Farm.Application.Abstractions.Ports;
using TC.Agro.Farm.Service.Options.OpenAi;
using TC.Agro.Farm.Service.Providers;

namespace TC.Agro.Farm.Tests.Service.Providers;

public sealed class OpenAiCropTypeSuggestionProviderTests
{
    private readonly ILogger<OpenAiCropTypeSuggestionProvider> _logger =
        A.Fake<ILogger<OpenAiCropTypeSuggestionProvider>>();

    private static CropTypeSuggestionAiRequest CreateRequest(double latitude = -23.5, double longitude = -46.6) =>
        new(
            PropertyId: Guid.NewGuid(),
            OwnerId: Guid.NewGuid(),
            City: "São Paulo",
            State: "SP",
            Country: "Brazil",
            Latitude: latitude,
            Longitude: longitude,
            SuggestionCount: 3);

    private static OpenAiCropSuggestionOptions CreateOptions(
        bool enabled = true,
        string apiKey = "test-api-key",
        int timeoutSeconds = 60) =>
        new()
        {
            Enabled = enabled,
            BaseUrl = "https://api.openai.com/",
            ApiKey = apiKey,
            Model = "gpt-4o-mini",
            Temperature = 0.2,
            TimeoutSeconds = timeoutSeconds,
            MaxSuggestions = 15
        };

    private OpenAiCropTypeSuggestionProvider CreateProvider(
        HttpMessageHandler handler,
        OpenAiCropSuggestionOptions? options = null)
    {
        var opts = options ?? CreateOptions();
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.openai.com/"),
            Timeout = TimeSpan.FromSeconds(opts.TimeoutSeconds)
        };
        return new OpenAiCropTypeSuggestionProvider(
            httpClient,
            Options.Create(opts),
            _logger);
    }

    [Fact]
    public async Task GenerateSuggestionsAsync_WhenDisabled_ReturnsFallbackWithoutCallingApi()
    {
        var handler = A.Fake<HttpMessageHandler>();
        var provider = CreateProvider(handler, CreateOptions(enabled: false));

        var result = await provider.GenerateSuggestionsAsync(CreateRequest(), CancellationToken.None);

        result.ShouldNotBeEmpty();
        result.Count.ShouldBe(3);
        A.CallTo(handler).WithReturnType<Task<HttpResponseMessage>>().MustNotHaveHappened();
    }

    [Fact]
    public async Task GenerateSuggestionsAsync_WhenApiKeyEmpty_ReturnsFallback()
    {
        var handler = A.Fake<HttpMessageHandler>();
        var provider = CreateProvider(handler, CreateOptions(apiKey: ""));

        var result = await provider.GenerateSuggestionsAsync(CreateRequest(), CancellationToken.None);

        result.ShouldNotBeEmpty();
    }

    [Fact]
    public async Task GenerateSuggestionsAsync_WhenApiReturnsValidResponse_ReturnsParsedSuggestions()
    {
        var openAiResponse = new
        {
            choices = new[]
            {
                new
                {
                    message = new
                    {
                        content = JsonSerializer.Serialize(new
                        {
                            suggestions = new[]
                            {
                                new
                                {
                                    cropType = "Soy",
                                    confidenceScore = 88,
                                    plantingWindow = "Oct-Nov",
                                    harvestCycleMonths = 5,
                                    suggestedIrrigationType = "Center Pivot",
                                    minSoilMoisture = 30,
                                    maxTemperature = 35,
                                    minHumidity = 45,
                                    notes = "Excellent for subtropical region",
                                    suggestedImage = "🫘"
                                },
                                new
                                {
                                    cropType = "Corn",
                                    confidenceScore = 85,
                                    plantingWindow = "Sep-Oct",
                                    harvestCycleMonths = 5,
                                    suggestedIrrigationType = "Center Pivot",
                                    minSoilMoisture = 28,
                                    maxTemperature = 36,
                                    minHumidity = 40,
                                    notes = "High productivity",
                                    suggestedImage = "🌽"
                                }
                            }
                        })
                    }
                }
            }
        };

        var handler = new FakeHttpMessageHandler(
            HttpStatusCode.OK,
            JsonSerializer.Serialize(openAiResponse));

        var provider = CreateProvider(handler);

        var result = await provider.GenerateSuggestionsAsync(CreateRequest(), CancellationToken.None);

        result.Count.ShouldBe(2);
        result[0].CropType.ShouldBe("Soy");
        result[0].ConfidenceScore.ShouldBe(88);
        result[1].CropType.ShouldBe("Corn");
    }

    [Fact]
    public async Task GenerateSuggestionsAsync_WhenApiReturnsNonSuccess_ReturnsFallback()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.TooManyRequests, "rate limited");
        var provider = CreateProvider(handler);

        var result = await provider.GenerateSuggestionsAsync(CreateRequest(), CancellationToken.None);

        result.ShouldNotBeEmpty();
    }

    [Fact]
    public async Task GenerateSuggestionsAsync_WhenApiTimesOut_ReturnsFallbackAndLogsWarning()
    {
        var handler = new FakeHttpMessageHandler(TimeSpan.FromSeconds(5));
        var opts = CreateOptions(timeoutSeconds: 1);
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.openai.com/"),
            Timeout = TimeSpan.FromSeconds(opts.TimeoutSeconds)
        };
        var provider = new OpenAiCropTypeSuggestionProvider(
            httpClient,
            Options.Create(opts),
            _logger);

        var result = await provider.GenerateSuggestionsAsync(CreateRequest(), CancellationToken.None);

        result.ShouldNotBeEmpty();
        A.CallTo(_logger)
            .Where(call => call.Method.Name == "Log" &&
                           call.GetArgument<LogLevel>(0) == LogLevel.Warning)
            .MustHaveHappenedOnceOrMore();
    }

    [Fact]
    public async Task GenerateSuggestionsAsync_WhenCallerCancels_ThrowsOperationCanceledException()
    {
        using var cts = new CancellationTokenSource();
        var handler = new FakeHttpMessageHandler(TimeSpan.FromSeconds(5));
        var provider = CreateProvider(handler);

        await cts.CancelAsync();

        await Should.ThrowAsync<OperationCanceledException>(
            () => provider.GenerateSuggestionsAsync(CreateRequest(), cts.Token));
    }

    [Theory]
    [InlineData(-10.0, 3)]  // tropical zone (<=15)
    [InlineData(-20.0, 3)]  // subtropical zone (<=30)
    [InlineData(-40.0, 3)]  // temperate zone (>30)
    public async Task GenerateSuggestionsAsync_WhenDisabled_FallbackRespectsZoneAndCount(
        double latitude, int count)
    {
        var handler = A.Fake<HttpMessageHandler>();
        var provider = CreateProvider(handler, CreateOptions(enabled: false));

        var result = await provider.GenerateSuggestionsAsync(
            CreateRequest(latitude: latitude), CancellationToken.None);

        result.Count.ShouldBe(count);
        result.ShouldAllBe(s => !string.IsNullOrWhiteSpace(s.CropType));
    }

    [Fact]
    public async Task GenerateSuggestionsAsync_WhenApiReturnsEmptySuggestions_ReturnsFallback()
    {
        var openAiResponse = new
        {
            choices = new[]
            {
                new
                {
                    message = new
                    {
                        content = JsonSerializer.Serialize(new { suggestions = Array.Empty<object>() })
                    }
                }
            }
        };

        var handler = new FakeHttpMessageHandler(
            HttpStatusCode.OK,
            JsonSerializer.Serialize(openAiResponse));

        var provider = CreateProvider(handler);

        var result = await provider.GenerateSuggestionsAsync(CreateRequest(), CancellationToken.None);

        result.ShouldNotBeEmpty();
    }

    [Fact]
    public async Task GenerateSuggestionsAsync_WhenRequestIsNull_ThrowsArgumentNullException()
    {
        var handler = A.Fake<HttpMessageHandler>();
        var provider = CreateProvider(handler);

        await Should.ThrowAsync<ArgumentNullException>(
            () => provider.GenerateSuggestionsAsync(null!, CancellationToken.None));
    }

    // ---------------------------------------------------------------------------
    // Test helpers
    // ---------------------------------------------------------------------------

    private sealed class FakeHttpMessageHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _statusCode;
        private readonly string _content;
        private readonly TimeSpan _delay;

        public FakeHttpMessageHandler(HttpStatusCode statusCode, string content)
        {
            _statusCode = statusCode;
            _content = content;
            _delay = TimeSpan.Zero;
        }

        public FakeHttpMessageHandler(TimeSpan delay)
        {
            _statusCode = HttpStatusCode.OK;
            _content = string.Empty;
            _delay = delay;
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            if (_delay > TimeSpan.Zero)
            {
                await Task.Delay(_delay, cancellationToken);
            }

            return new HttpResponseMessage(_statusCode)
            {
                Content = new StringContent(_content, Encoding.UTF8, "application/json")
            };
        }
    }
}
