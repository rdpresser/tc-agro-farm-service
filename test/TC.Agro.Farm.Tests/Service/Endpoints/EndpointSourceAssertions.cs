namespace TC.Agro.Farm.Tests.Service.Endpoints;

internal static class EndpointSourceAssertions
{
    public static string LoadEndpointSource(string endpointFolder, string endpointFileName)
    {
        var serviceRoot = FindServiceRoot();
        var endpointPath = Path.Combine(
            serviceRoot,
            "src",
            "Adapters",
            "Inbound",
            "TC.Agro.Farm.Service",
            "Endpoints",
            endpointFolder,
            endpointFileName);

        File.Exists(endpointPath)
            .ShouldBeTrue($"Expected endpoint source file to exist: {endpointPath}");

        return File.ReadAllText(endpointPath);
    }

    public static void AssertContains(string source, params string[] expectedSnippets)
    {
        foreach (var expectedSnippet in expectedSnippets)
        {
            source.Contains(expectedSnippet, StringComparison.Ordinal)
                .ShouldBeTrue($"Expected endpoint source to contain '{expectedSnippet}'.");
        }
    }

    private static string FindServiceRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);

        while (current is not null)
        {
            var markerFile = Path.Combine(current.FullName, "TC.Agro.Farm.Service.slnx");
            if (File.Exists(markerFile))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new InvalidOperationException("Could not locate farm-service repository root.");
    }
}
