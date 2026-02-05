using System.ComponentModel;
using DotnetTest.Mcp.Terminal;
using ModelContextProtocol.Server;

namespace DotnetTest.Mcp.Tools;

[McpServerToolType]
public sealed class ListTestsSummaryTool(ICommandRunner commandRunner)
{
    private const int DefaultTake = 50;

    private readonly ICommandRunner _commandRunner = commandRunner.ValidateNotNull();

    [McpServerTool(UseStructuredContent = true)]
    [Description("Summarizes discovered tests with counts and optional paging.")]
    public async Task<Result> ListTestsSummary(
        [Description("Optional path to project file to scope listing.")] string? projectPath = null,
        [Description("Optional prefix to filter fully qualified test names.")] string? prefix =
            null,
        [Description("Number of filtered tests to skip.")] int skip = 0,
        [Description("Number of filtered tests to return.")] int take = DefaultTake,
        [Description("When true, include the paged test names; otherwise return counts only.")]
        bool includeTests = false,
        CancellationToken cancellationToken = default)
    {
        if (skip < 0)
            throw new ArgumentOutOfRangeException(nameof(skip));

        if (take <= 0)
            throw new ArgumentOutOfRangeException(nameof(take));

        var trimmedProjectPath = string.IsNullOrWhiteSpace(projectPath) ? null : projectPath.Trim();
        var trimmedPrefix = string.IsNullOrWhiteSpace(prefix) ? null : prefix.Trim();

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

        var tests = TestListParser.ExtractTests(commandResult.StandardOutputLines);
        if (trimmedPrefix is not null)
            tests = tests.Where(test => test.StartsWith(trimmedPrefix, StringComparison.Ordinal))
                .ToArray();

        var testCount = tests.Length;
        var hasMore = skip + take < testCount;
        var returnedTests = includeTests ? tests.Skip(skip).Take(take).ToArray() : [];
        var returnedCount = includeTests ? returnedTests.Length : 0;

        return new Result(
            scope,
            trimmedProjectPath,
            testCount,
            returnedCount,
            hasMore,
            returnedTests);
    }

    [Description("Result containing a summarized test listing.")]
    public record Result(
        [property: Description("Scope used to list tests: solution or project.")] string Scope,
        [property: Description("Project path used when Scope is project; otherwise null.")]
        string? ProjectPath,
        [property: Description("Total discovered test count after filtering.")] int TestCount,
        [property: Description("Number of tests returned in this response.")] int ReturnedCount,
        [property: Description("True when more tests exist beyond the current page.")] bool HasMore,
        [property: Description("Optional page of fully qualified test names.")] string[] Tests);
}