using System.Text.Json;
using System.Text.Json.Serialization;

namespace DotnetTest.Mcp.Models;

public sealed record CtrfReport
{
    [JsonPropertyName("reportFormat")]
    public string ReportFormat { get; init; } = string.Empty;

    [JsonPropertyName("specVersion")]
    public string SpecVersion { get; init; } = string.Empty;

    [JsonPropertyName("reportId")]
    public string ReportId { get; init; } = string.Empty;

    [JsonPropertyName("timestamp")]
    public DateTimeOffset Timestamp { get; init; }

    [JsonPropertyName("results")]
    public CtrfResults Results { get; init; } = new();
}

public sealed record CtrfResults
{
    [JsonPropertyName("tool")]
    public CtrfTool Tool { get; init; } = new();

    [JsonPropertyName("environment")]
    public CtrfEnvironment Environment { get; init; } = new();

    [JsonPropertyName("extra")]
    public CtrfResultsExtra Extra { get; init; } = new();

    [JsonPropertyName("summary")]
    public CtrfSummary Summary { get; init; } = new();

    [JsonPropertyName("tests")]
    public IReadOnlyList<CtrfTest> Tests { get; init; } = Array.Empty<CtrfTest>();
}

public sealed record CtrfTool
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("version")]
    public string Version { get; init; } = string.Empty;
}

public sealed record CtrfEnvironment
{
    [JsonPropertyName("osPlatform")]
    public string OsPlatform { get; init; } = string.Empty;

    [JsonPropertyName("osRelease")]
    public string OsRelease { get; init; } = string.Empty;
}

public sealed record CtrfResultsExtra
{
    [JsonPropertyName("user")]
    public string? User { get; init; }

    [JsonPropertyName("suites")]
    public IReadOnlyList<CtrfSuite> Suites { get; init; } = Array.Empty<CtrfSuite>();

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtensionData { get; init; }
}

public sealed record CtrfSuite
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("filePath")]
    public string FilePath { get; init; } = string.Empty;

    [JsonPropertyName("environment")]
    public string Environment { get; init; } = string.Empty;

    [JsonPropertyName("testFramework")]
    public string TestFramework { get; init; } = string.Empty;

    [JsonPropertyName("targetFramework")]
    public string TargetFramework { get; init; } = string.Empty;

    [JsonPropertyName("start")]
    public long Start { get; init; }

    [JsonPropertyName("stop")]
    public long Stop { get; init; }

    [JsonPropertyName("duration")]
    public long Duration { get; init; }

    [JsonPropertyName("collections")]
    public IReadOnlyList<CtrfCollection> Collections { get; init; } = Array.Empty<CtrfCollection>();

    [JsonPropertyName("errors")]
    public IReadOnlyList<CtrfSuiteError> Errors { get; init; } = Array.Empty<CtrfSuiteError>();
}

public sealed record CtrfCollection
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;
}

public sealed record CtrfSuiteError
{
    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtensionData { get; init; }
}

public sealed record CtrfSummary
{
    [JsonPropertyName("tests")]
    public int Tests { get; init; }

    [JsonPropertyName("passed")]
    public int Passed { get; init; }

    [JsonPropertyName("failed")]
    public int Failed { get; init; }

    [JsonPropertyName("pending")]
    public int Pending { get; init; }

    [JsonPropertyName("skipped")]
    public int Skipped { get; init; }

    [JsonPropertyName("other")]
    public int Other { get; init; }

    [JsonPropertyName("suites")]
    public int Suites { get; init; }

    [JsonPropertyName("start")]
    public long Start { get; init; }

    [JsonPropertyName("stop")]
    public long Stop { get; init; }
}

public sealed record CtrfTest
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; init; } = string.Empty;

    [JsonPropertyName("duration")]
    public long Duration { get; init; }

    [JsonPropertyName("suite")]
    public string Suite { get; init; } = string.Empty;

    [JsonPropertyName("filePath")]
    public string? FilePath { get; init; }

    [JsonPropertyName("line")]
    public int? Line { get; init; }

    [JsonPropertyName("message")]
    public string? Message { get; init; }

    [JsonPropertyName("trace")]
    public string? Trace { get; init; }

    [JsonPropertyName("extra")]
    public CtrfTestExtra Extra { get; init; } = new();
}

public sealed record CtrfTestExtra
{
    [JsonPropertyName("id")]
    public string? Id { get; init; }

    [JsonPropertyName("collection")]
    public string? Collection { get; init; }

    [JsonPropertyName("type")]
    public string? Type { get; init; }

    [JsonPropertyName("method")]
    public string? Method { get; init; }

    [JsonPropertyName("exception")]
    public string? Exception { get; init; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtensionData { get; init; }
}