using System.ComponentModel;
using DotnetTest.Mcp.Terminal;
using ModelContextProtocol.Server;

namespace DotnetTest.Mcp.Tools;

[McpServerToolType]
public sealed class ListTestsTool(ICommandRunner commandRunner)
{
    private readonly ICommandRunner _commandRunner = commandRunner.ValidateNotNull();

    [McpServerTool(UseStructuredContent = true)]
    [Description("Lists all discovered tests for the solution or a project.")]
    public async Task<Result> ListTests(
        [Description("Optional path to project file to scope listing.")] string? projectPath = null,
        CancellationToken cancellationToken = default)
    {
        var trimmedProjectPath = string.IsNullOrWhiteSpace(projectPath) ? null : projectPath.Trim();

        var arguments = new List<string> { "test" };

        string scope;
        if (trimmedProjectPath is null)
        {
            scope = "solution";
        }
        else
        {
            arguments.Add("--project");
            arguments.Add(trimmedProjectPath);
            scope = "project";
        }

        arguments.Add("--list-tests");
        arguments.Add("--no-ansi");
        arguments.Add("--no-progress");

        var commandResult = await _commandRunner.RunAsync(
            new CommandRequest("dotnet", arguments.ToArray()) { ThrowOnNonZeroExitCode = false },
            cancellationToken);

        var tests = ExtractTests(commandResult.StandardOutputLines);
        return new Result(scope, trimmedProjectPath, tests.Length, tests);
    }

    [Description("Result containing discovered test names.")]
    public record Result(
        [property: Description("Scope used to list tests: solution or project.")] string Scope,
        [property: Description("Project path used when Scope is project; otherwise null.")]
        string? ProjectPath,
        [property: Description("Number of discovered tests.")] int TestCount,
        [property: Description("Fully qualified test names returned by the test adapter.")]
        string[] Tests);

    private static string[] ExtractTests(string[] lines)
    {
        if (lines.Length == 0)
            return Array.Empty<string>();

        var startIndex = FindListStartIndex(lines);
        var candidates = startIndex >= 0 ? lines.Skip(startIndex + 1) : lines.AsEnumerable();

        var tests = new List<string>();
        foreach (var line in candidates)
        {
            var trimmed = line.Trim();
            if (IsNoiseLine(trimmed))
                continue;

            if (!LooksLikeTestName(trimmed))
                continue;

            tests.Add(trimmed);
        }

        return tests.Distinct(StringComparer.Ordinal)
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToArray();
    }

    private static int FindListStartIndex(string[] lines)
    {
        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            if (line.Contains("Tests are available", StringComparison.OrdinalIgnoreCase)
                || line.Contains("Available Tests", StringComparison.OrdinalIgnoreCase))
                return i;
        }

        return -1;
    }

    private static bool IsNoiseLine(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
            return true;

        if (line.StartsWith("Discovering tests from", StringComparison.OrdinalIgnoreCase))
            return true;

        if (line.StartsWith("Discovered ", StringComparison.OrdinalIgnoreCase))
            return true;

        if (line.StartsWith("Exit code:", StringComparison.OrdinalIgnoreCase))
            return true;

        if (line.StartsWith("Test discovery completed", StringComparison.OrdinalIgnoreCase))
            return true;

        if (line.StartsWith(
            "The following Tests are available",
            StringComparison.OrdinalIgnoreCase))
            return true;

        if (line.StartsWith("Test run for", StringComparison.OrdinalIgnoreCase))
            return true;

        if (line.StartsWith("Starting test execution", StringComparison.OrdinalIgnoreCase))
            return true;

        if (line.StartsWith("Total tests", StringComparison.OrdinalIgnoreCase))
            return true;

        if (line.StartsWith("Passed", StringComparison.OrdinalIgnoreCase))
            return true;

        if (line.StartsWith("Failed", StringComparison.OrdinalIgnoreCase))
            return true;

        if (line.StartsWith("Skipped", StringComparison.OrdinalIgnoreCase))
            return true;

        if (line.StartsWith("Build ", StringComparison.OrdinalIgnoreCase))
            return true;

        if (line.StartsWith("Test Run", StringComparison.OrdinalIgnoreCase))
            return true;

        if (line.StartsWith("Results File:", StringComparison.OrdinalIgnoreCase))
            return true;

        if (line.StartsWith("A total of", StringComparison.OrdinalIgnoreCase))
            return true;

        return false;
    }

    private static bool LooksLikeTestName(string line)
        => line.Contains('.', StringComparison.Ordinal)
            && !line.Contains(' ', StringComparison.Ordinal);
}