namespace TC.Agro.Farm.Service.Options.OpenAi
{
    public sealed class OpenAiCropSuggestionOptions
    {
        public const string SectionName = "OpenAI";

        public bool Enabled { get; set; }
        public string BaseUrl { get; set; } = "https://api.openai.com";
        public string ApiKey { get; set; } = string.Empty;
        public string Model { get; set; } = "gpt-4o-mini";
        public double Temperature { get; set; } = 0.2;
        public int TimeoutSeconds { get; set; } = 20;
        public int MaxSuggestions { get; set; } = 15;
    }
}
