using System.ComponentModel;
using DotnetTest.Mcp.Models;
using DotnetTest.Mcp.Terminal;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Server;

namespace DotnetTest.Mcp.Tools;

[McpServerToolType]
public sealed class ListTestsTool(IOptions<McpOptions> options, ICommandRunner commandRunner)
{
    private const int DefaultTake = 200;
    private const int MaxTake = 200;

    private readonly McpOptions _options = options.ValidateNotNull().Value;
    private readonly ICommandRunner _commandRunner = commandRunner.ValidateNotNull();

    [McpServerTool(UseStructuredContent = true)]
    [Description("Lists discovered tests for the solution or a project with paging.")]
    public async Task<Result> ListTests(
        [Description("Optional path to project file to scope listing.")] string? projectPath = null,
        [Description("Optional prefix to filter fully qualified test names.")] string? prefix =
            null,
        [Description("Number of filtered tests to skip.")] int skip = 0,
        [Description("Number of filtered tests to return.")] int take = DefaultTake,
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

        return trimmedProjectPath is null
            ? await ListSolutionTests(trimmedPrefix, skip, take, requireTests, cancellationToken)
            : await ListProjectTests(
                trimmedProjectPath,
                trimmedPrefix,
                skip,
                take,
                requireTests,
                cancellationToken);
    }

    private async Task<Result> ListProjectTests(
        string projectPath,
        string? prefix,
        int skip,
        int take,
        bool requireTests,
        CancellationToken cancellationToken)
    {
        var discovery = await DiscoverProjectTests(
            projectPath,
            prefix,
            requireTests,
            cancellationToken);

        var tests = discovery.Tests;
        var testCount = tests.Length;
        var hasMore = skip + take < testCount;
        var returnedTests = tests.Skip(skip).Take(take).ToArray();
        var returnedCount = returnedTests.Length;
        var projectResult = discovery.ProjectResult;

        return new Result(
            "project",
            projectPath,
            projectResult.Outcome,
            projectResult.Message,
            projectResult.ExitCode,
            testCount,
            returnedCount,
            hasMore,
            returnedTests,
            projectResult.Warning,
            [projectResult],
            projectResult.Error);
    }

    private async Task<Result> ListSolutionTests(
        string? prefix,
        int skip,
        int take,
        bool requireTests,
        CancellationToken cancellationToken)
    {
        var testProjects = await TestProjectDiscovery.ListAsync(
            _commandRunner,
            _options,
            cancellationToken);

        if (testProjects.Length == 0)
        {
            if (requireTests)
            {
                var noTestsError = new ErrorInfo(
                    ErrorKind.NoTestsDiscovered,
                    "No tests discovered.",
                    null,
                    null);
                return new Result(
                    "solution",
                    null,
                    TestOutcome.Error,
                    noTestsError.Summary,
                    0,
                    0,
                    0,
                    false,
                    [],
                    null,
                    [],
                    noTestsError);
            }

            return new Result(
                "solution",
                null,
                TestOutcome.Passed,
                "No test projects discovered.",
                0,
                0,
                0,
                false,
                [],
                "No test projects discovered.",
                [],
                null);
        }

        var projectResults = new List<ProjectDiscoveryResult>(testProjects.Length);
        var allTests = new List<string>();

        foreach (var projectPath in testProjects)
        {
            var discovery = await DiscoverProjectTests(
                projectPath,
                prefix,
                requireTests,
                cancellationToken);
            projectResults.Add(discovery.ProjectResult);
            if (discovery.Tests.Length > 0)
                allTests.AddRange(discovery.Tests);
        }

        var tests = allTests.Distinct(StringComparer.Ordinal)
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToArray();
        var testCount = tests.Length;
        var hasMore = skip + take < testCount;
        var returnedTests = tests.Skip(skip).Take(take).ToArray();
        var returnedCount = returnedTests.Length;

        var anyError = projectResults.Any(result => result.Outcome == TestOutcome.Error);
        var anySuccess = projectResults.Any(result => result.Outcome == TestOutcome.Passed);

        string message;
        string? warning = null;
        ErrorInfo? error = null;
        TestOutcome outcome;

        if (requireTests && testCount == 0)
        {
            error = new ErrorInfo(ErrorKind.NoTestsDiscovered, "No tests discovered.", null, null);
            outcome = TestOutcome.Error;
            message = error.Summary;
        }
        else if (anyError && anySuccess)
        {
            outcome = TestOutcome.Partial;
            message = "Discovery partially succeeded. See ProjectResults for details.";
        }
        else if (anyError)
        {
            outcome = TestOutcome.Error;
            message = "Discovery failed for all projects.";
            error = new ErrorInfo(ErrorKind.DiscoveryFailed, message, null, null);
        }
        else if (testCount == 0)
        {
            outcome = TestOutcome.Passed;
            warning = "No tests discovered.";
            message = warning;
        }
        else
        {
            outcome = TestOutcome.Passed;
            message = $"Discovered {testCount} tests.";
        }

        return new Result(
            "solution",
            null,
            outcome,
            message,
            0,
            testCount,
            returnedCount,
            hasMore,
            returnedTests,
            warning,
            projectResults.ToArray(),
            error);
    }

    private async Task<ProjectDiscovery> DiscoverProjectTests(
        string projectPath,
        string? prefix,
        bool requireTests,
        CancellationToken cancellationToken)
    {
        var arguments = new List<string>
        {
            "test",
            "--project",
            projectPath,
            "--list-tests",
            "--no-ansi",
            "--no-progress",
        };

        var commandResult = await _commandRunner.RunAsync(
            new CommandRequest("dotnet", arguments.ToArray()) { ThrowOnNonZeroExitCode = false },
            cancellationToken);

        var tests = TestListParser.ExtractTests(commandResult.StandardOutputLines);
        if (prefix is not null)
            tests = tests.Where(test => test.StartsWith(prefix, StringComparison.Ordinal))
                .ToArray();

        var testCount = tests.Length;
        var exitCode = commandResult.ExitCode;
        if (exitCode != 0)
        {
            var errorKind = CtrfTestRun.ClassifyErrorKind(commandResult, true, null);
            if (errorKind == ErrorKind.NoTestsDiscovered && !requireTests)
            {
                var warning = "No tests discovered.";
                return new ProjectDiscovery(
                    new ProjectDiscoveryResult(
                        projectPath,
                        TestOutcome.Passed,
                        warning,
                        exitCode,
                        0,
                        warning,
                        null),
                    tests);
            }

            var error = CtrfTestRun.BuildErrorInfo(
                commandResult,
                null,
                OutputMode.Summary,
                errorKind);
            return new ProjectDiscovery(
                new ProjectDiscoveryResult(
                    projectPath,
                    TestOutcome.Error,
                    error.Summary,
                    exitCode,
                    testCount,
                    null,
                    error),
                tests);
        }

        if (testCount == 0)
        {
            if (requireTests)
            {
                var error = CtrfTestRun.BuildErrorInfo(
                    commandResult,
                    "No tests discovered.",
                    OutputMode.Summary,
                    ErrorKind.NoTestsDiscovered);
                return new ProjectDiscovery(
                    new ProjectDiscoveryResult(
                        projectPath,
                        TestOutcome.Error,
                        error.Summary,
                        exitCode,
                        0,
                        null,
                        error),
                    tests);
            }

            var warning = "No tests discovered.";
            return new ProjectDiscovery(
                new ProjectDiscoveryResult(
                    projectPath,
                    TestOutcome.Passed,
                    warning,
                    exitCode,
                    0,
                    warning,
                    null),
                tests);
        }

        return new ProjectDiscovery(
            new ProjectDiscoveryResult(
                projectPath,
                TestOutcome.Passed,
                $"Discovered {testCount} tests.",
                exitCode,
                testCount,
                null,
                null),
            tests);
    }

    private sealed record ProjectDiscovery(ProjectDiscoveryResult ProjectResult, string[] Tests);

    [Description("Result containing discovered test names.")]
    public record Result(
        [property: Description("Scope used to list tests: solution or project.")] string Scope,
        [property: Description("Project path used when Scope is project; otherwise null.")]
        string? ProjectPath,
        [property: Description("Outcome of the discovery run.")] TestOutcome Outcome,
        [property: Description("Short summary (<= 1-2 lines).")] string Message,
        [property: Description("Process exit code from dotnet test when available.")] int ExitCode,
        [property: Description("Total discovered test count after filtering.")] int TestCount,
        [property: Description("Number of tests returned in this response.")] int ReturnedCount,
        [property: Description("True when more tests exist beyond the current page.")] bool HasMore,
        [property: Description("Fully qualified test names returned for this page.")]
        string[] Tests,
        [property: Description("Optional warning message when Outcome is Passed.")] string? Warning,
        [property: Description("Per-project discovery results when Scope is solution.")]
        ProjectDiscoveryResult[] ProjectResults,
        [property: Description("Structured error details when Outcome is Error; otherwise null.")]
        ErrorInfo? Error);
}
