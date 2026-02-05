using System.ComponentModel;
using DotnetTest.Mcp.Models;
using DotnetTest.Mcp.Terminal;
using ModelContextProtocol.Server;

namespace DotnetTest.Mcp.Tools;

[McpServerToolType]
public sealed class ListTestsSummaryTool(ICommandRunner commandRunner)
{
    private const int DefaultTake = 50;
    private const int MaxTake = 200;

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
        [Description("When true, treat zero discovered tests as an error.")] bool requireTests =
            false,
        CancellationToken cancellationToken = default)
    {
        if (skip < 0)
            throw new ArgumentOutOfRangeException(nameof(skip));

        if (take <= 0)
            throw new ArgumentOutOfRangeException(nameof(take));

        if (take > MaxTake)
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
        var exitCode = commandResult.ExitCode;
        if (exitCode != 0)
        {
            var errorKind = CtrfTestRun.ClassifyErrorKind(commandResult, true, null);
            var error = CtrfTestRun.BuildErrorInfo(
                commandResult,
                null,
                OutputMode.Summary,
                errorKind);
            return new Result(
                scope,
                trimmedProjectPath,
                TestOutcome.Error,
                error.Summary,
                exitCode,
                testCount,
                0,
                false,
                [],
                error);
        }

        if (testCount == 0)
        {
            if (requireTests)
            {
                var error = CtrfTestRun.BuildErrorInfo(
                    commandResult,
                    "No tests discovered.",
                    OutputMode.Summary,
                    ErrorKind.DiscoveryFailed);
                return new Result(
                    scope,
                    trimmedProjectPath,
                    TestOutcome.Error,
                    error.Summary,
                    exitCode,
                    0,
                    0,
                    false,
                    [],
                    error);
            }

            return new Result(
                scope,
                trimmedProjectPath,
                TestOutcome.NotFound,
                "No tests discovered.",
                exitCode,
                0,
                0,
                false,
                [],
                null);
        }

        var hasMore = skip + take < testCount;
        var returnedTests = includeTests ? tests.Skip(skip).Take(take).ToArray() : [];
        var returnedCount = includeTests ? returnedTests.Length : 0;

        return new Result(
            scope,
            trimmedProjectPath,
            TestOutcome.Passed,
            $"Discovered {testCount} tests.",
            exitCode,
            testCount,
            returnedCount,
            hasMore,
            returnedTests,
            null);
    }

    [Description("Result containing a summarized test listing.")]
    public record Result(
        [property: Description("Scope used to list tests: solution or project.")] string Scope,
        [property: Description("Project path used when Scope is project; otherwise null.")]
        string? ProjectPath,
        [property: Description("Outcome of the discovery run.")] TestOutcome Outcome,
        [property: Description("Short summary (<= 1-2 lines).")] string Message,
        [property: Description("Process exit code from dotnet test.")] int ExitCode,
        [property: Description("Total discovered test count after filtering.")] int TestCount,
        [property: Description("Number of tests returned in this response.")] int ReturnedCount,
        [property: Description("True when more tests exist beyond the current page.")] bool HasMore,
        [property: Description("Optional page of fully qualified test names.")] string[] Tests,
        [property: Description("Structured error details when Outcome is Error; otherwise null.")]
        ErrorInfo? Error);
}
