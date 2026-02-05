using System.ComponentModel;
using System.Text.Json;
using DotnetTest.Mcp.Models;
using DotnetTest.Mcp.Terminal;
using ModelContextProtocol.Server;

namespace DotnetTest.Mcp.Tools;

[McpServerToolType]
public sealed class RunAllTestsTool(ICommandRunner commandRunner, JsonSerializerOptions jsonOptions)
{
    private const int MaxFailingTests = 20;

    private readonly ICommandRunner _commandRunner = commandRunner.ValidateNotNull();
    private readonly JsonSerializerOptions _jsonOptions = jsonOptions.ValidateNotNull();

    [McpServerTool(UseStructuredContent = true)]
    [Description("Runs all tests in the solution.")]
    public async Task<Result> RunAllTests(CancellationToken cancellationToken = default)
    {
        var runResult = await CtrfTestRun.ExecuteAsync(
            _commandRunner,
            _jsonOptions,
            new[] { "test" },
            cancellationToken);

        if (runResult.Report is null)
        {
            var commandSummary = CtrfTestRun.BuildCommandSummary(runResult.CommandResult);
            if (runResult.ReportFileFound && runResult.ReadErrorMessage is not null)
                return CreateResult(
                    TestOutcome.Error,
                    AppendSummary(runResult.ReadErrorMessage, commandSummary),
                    runResult.CommandResult.ExitCode);

            if (!runResult.ReportFileFound && runResult.CommandResult.ExitCode == 8)
                return CreateResult(
                    TestOutcome.NotFound,
                    "No tests found.",
                    runResult.CommandResult.ExitCode);

            return CreateResult(
                TestOutcome.Error,
                string.IsNullOrEmpty(commandSummary)
                    ? "Test result file not found."
                    : $"Test result file not found.{commandSummary}",
                runResult.CommandResult.ExitCode);
        }

        return CreateResultFromReport(runResult.Report, runResult.CommandResult);
    }

    [Description("Result of running the entire test suite.")]
    public record Result(
        [property: Description("Scope used to run tests: solution.")] string Scope,
        [property: Description("Outcome of the test run.")] TestOutcome Outcome,
        [property: Description("Short summary (<= 1-2 lines).")] string Message,
        [property: Description("Process exit code from dotnet test.")] int ExitCode,
        [property: Description("Total number of tests when available.")] int TestCount,
        [property: Description("Count of passed tests when available.")] int Passed,
        [property: Description("Count of failed tests when available.")] int Failed,
        [property: Description("Count of skipped tests when available.")] int Skipped,
        [property: Description("Count of pending tests when available.")] int Pending,
        [property: Description("Count of other-status tests when available.")] int Other,
        [property: Description("Total duration in milliseconds when available; otherwise null.")]
        int? DurationMilliseconds,
        [property: Description("Up to the first 20 failing test names when available.")]
        string[] FailingTests);

    private Result CreateResult(TestOutcome outcome, string message, int exitCode)
        => new("solution", outcome, message, exitCode, 0, 0, 0, 0, 0, 0, null, []);

    private Result CreateResultFromReport(CtrfReport report, CommandResult commandResult)
    {
        var summary = report.Results.Summary;
        var tests = report.Results.Tests;

        var exitCode = commandResult.ExitCode;
        var testCount = summary.Tests > 0 ? summary.Tests : tests.Count;
        var passed = summary.Passed > 0 ? summary.Passed : CountByStatus(tests, "passed");
        var failed = summary.Failed > 0 ? summary.Failed : CountByStatus(tests, "failed");
        var skipped = summary.Skipped > 0 ? summary.Skipped : CountByStatus(tests, "skipped");
        var pending = summary.Pending > 0 ? summary.Pending : CountByStatus(tests, "pending");
        var other = summary.Other > 0 ? summary.Other : CountByStatus(tests, "other");

        var durationMilliseconds = CtrfTestRun.TryParseDurationMilliseconds(summary.Duration);
        var failingTests = ExtractFailingTests(tests);

        if (exitCode != 0 && exitCode != 8 && failed == 0)
        {
            var summaryText = CtrfTestRun.BuildCommandSummary(commandResult);
            return new Result(
                "solution",
                TestOutcome.Error,
                string.IsNullOrEmpty(summaryText)
                    ? "Test run reported no failures but exited with a non-zero code."
                    : $"Test run reported no failures but exited with a non-zero code.{summaryText}",
                exitCode,
                testCount,
                passed,
                failed,
                skipped,
                pending,
                other,
                durationMilliseconds,
                failingTests);
        }

        if (testCount == 0 && tests.Count == 0)
            return new Result(
                "solution",
                TestOutcome.NotFound,
                "No tests found.",
                exitCode,
                testCount,
                passed,
                failed,
                skipped,
                pending,
                other,
                durationMilliseconds,
                failingTests);

        var outcome =
            failed > 0 ? TestOutcome.Failed : passed > 0 ? TestOutcome.Passed : TestOutcome.Skipped;

        var message = outcome switch
        {
            TestOutcome.Failed => "Test run completed with failures.",
            TestOutcome.Passed => "All tests passed.",
            TestOutcome.Skipped => "Tests completed without failures.",
            _ => "Test run completed with an unknown outcome.",
        };

        return new Result(
            "solution",
            outcome,
            message,
            exitCode,
            testCount,
            passed,
            failed,
            skipped,
            pending,
            other,
            durationMilliseconds,
            failingTests);
    }

    private static int CountByStatus(IReadOnlyCollection<CtrfTest> tests, string status)
        => tests.Count(test
            => string.Equals(test.Status, status, StringComparison.OrdinalIgnoreCase));

    private static string[] ExtractFailingTests(IReadOnlyCollection<CtrfTest> tests)
        => tests
            .Where(test => string.Equals(test.Status, "failed", StringComparison.OrdinalIgnoreCase))
            .Select(test => test.Name)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.Ordinal)
            .Take(MaxFailingTests)
            .ToArray();

    private static string AppendSummary(string message, string commandSummary)
        => string.IsNullOrEmpty(commandSummary) ? message : $"{message}{commandSummary}";
}