using System.ComponentModel;
using System.Text.Json;
using DotnetTest.Mcp.Models;
using DotnetTest.Mcp.Terminal;
using ModelContextProtocol.Server;

namespace DotnetTest.Mcp.Tools;

[McpServerToolType]
public sealed class RunAllTestsForProjectTool(
    ICommandRunner commandRunner,
    JsonSerializerOptions jsonOptions)
{
    private const int MaxFailingTests = 20;

    private readonly ICommandRunner _commandRunner = commandRunner.ValidateNotNull();
    private readonly JsonSerializerOptions _jsonOptions = jsonOptions.ValidateNotNull();

    [McpServerTool(UseStructuredContent = true)]
    [Description("Runs all tests for a single project.")]
    public async Task<Result> RunAllTestsForProject(
        [Description("Path to the project file (.csproj) to test.")] string projectPath,
        [Description("Output mode: summary or verbose.")] OutputMode outputMode =
            OutputMode.Summary,
        [Description("Failure detail level: None, TopLine, DiffSnippet, or Full.")]
        FailureDetailLevel failureDetailLevel = FailureDetailLevel.TopLine,
        [Description(
            "Include stack traces in failure details. Defaults to false in summary, true in verbose.")]
        bool? includeStackTrace = null,
        [Description("Maximum characters to include for failure message/trace.")]
        int? maxFailureChars = null,
        [Description("Maximum lines to include for failure message/trace.")] int? maxFailureLines =
            null,
        [Description("Maximum number of per-test failure details to return.")] int? maxFailures =
            null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(projectPath))
            throw new ArgumentException("Project path is required.", nameof(projectPath));

        var trimmedProjectPath = projectPath.Trim();

        var outputOptions = FailureFormatting.CreateOptions(
            outputMode,
            includeStackTrace,
            maxFailureChars,
            maxFailureLines,
            failureDetailLevel);
        var resolvedMaxFailures = FailureFormatting.ResolveMaxFailures(maxFailures);

        var runResult = await CtrfTestRun.ExecuteAsync(
            _commandRunner,
            _jsonOptions,
            new[] { "test", trimmedProjectPath },
            cancellationToken);

        if (runResult.Report is null)
        {
            if (runResult.ReportFileFound && runResult.ReadErrorMessage is not null)
            {
                var errorInfo = CtrfTestRun.BuildErrorInfo(
                    runResult.CommandResult,
                    runResult.ReadErrorMessage,
                    outputMode,
                    ErrorKind.ReadFailed);
                return CreateResult(
                    trimmedProjectPath,
                    TestOutcome.Error,
                    errorInfo.Summary,
                    runResult.CommandResult.ExitCode,
                    [],
                    false,
                    errorInfo);
            }

            if (!runResult.ReportFileFound && runResult.CommandResult.ExitCode == 8)
                return CreateResult(
                    trimmedProjectPath,
                    TestOutcome.NotFound,
                    "No tests found.",
                    runResult.CommandResult.ExitCode,
                    [],
                    false,
                    null);

            var errorKind = CtrfTestRun.ClassifyErrorKind(
                runResult.CommandResult,
                runResult.ReportFileFound,
                runResult.ReadErrorMessage);
            var error = CtrfTestRun.BuildErrorInfo(
                runResult.CommandResult,
                runResult.ReadErrorMessage,
                outputMode,
                errorKind);
            return CreateResult(
                trimmedProjectPath,
                TestOutcome.Error,
                error.Summary,
                runResult.CommandResult.ExitCode,
                [],
                false,
                error);
        }

        return CreateResultFromReport(
            trimmedProjectPath,
            runResult.Report,
            runResult.CommandResult,
            outputOptions,
            resolvedMaxFailures);
    }

    [Description("Result of running all tests for a project.")]
    public record Result(
        [property: Description("Scope used to run tests: project.")] string Scope,
        [property: Description("Project path used to scope the test run.")] string ProjectPath,
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
        string[] FailingTests,
        [property: Description("Structured details for up to maxFailures failed tests.")]
        TestFailure[] FailureDetails,
        [property: Description("True when more failed tests exist than returned.")]
        bool HasMoreFailures,
        [property: Description("Structured error details when Outcome is Error; otherwise null.")]
        ErrorInfo? Error);

    private Result CreateResult(
        string projectPath,
        TestOutcome outcome,
        string message,
        int exitCode,
        TestFailure[] failureDetails,
        bool hasMoreFailures,
        ErrorInfo? error)
        => new(
            "project",
            projectPath,
            outcome,
            message,
            exitCode,
            0,
            0,
            0,
            0,
            0,
            0,
            null,
            [],
            failureDetails,
            hasMoreFailures,
            error);

    private Result CreateResultFromReport(
        string projectPath,
        CtrfReport report,
        CommandResult commandResult,
        FailureFormatting.FailureOutputOptions outputOptions,
        int maxFailures)
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
        var failureDetails =
            ExtractFailureDetails(tests, outputOptions, maxFailures, out var hasMoreFailures);

        if (exitCode != 0 && exitCode != 8 && failed == 0)
        {
            var errorKind = CtrfTestRun.ClassifyErrorKind(commandResult, true, null);
            var error = CtrfTestRun.BuildErrorInfo(
                commandResult,
                null,
                outputOptions.OutputMode,
                errorKind);
            return new Result(
                "project",
                projectPath,
                TestOutcome.Error,
                error.Summary,
                exitCode,
                testCount,
                passed,
                failed,
                skipped,
                pending,
                other,
                durationMilliseconds,
                failingTests,
                failureDetails,
                hasMoreFailures,
                error);
        }

        if (testCount == 0 && tests.Count == 0)
            return new Result(
                "project",
                projectPath,
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
                failingTests,
                failureDetails,
                hasMoreFailures,
                null);

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
            "project",
            projectPath,
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
            failingTests,
            failureDetails,
            hasMoreFailures,
            null);
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

    private static TestFailure[] ExtractFailureDetails(
        IReadOnlyCollection<CtrfTest> tests,
        FailureFormatting.FailureOutputOptions outputOptions,
        int maxFailures,
        out bool hasMoreFailures)
    {
        var failures = tests.Where(test
                => string.Equals(test.Status, "failed", StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (failures.Count == 0 || maxFailures <= 0)
        {
            hasMoreFailures = failures.Count > 0;
            return [];
        }

        var details = failures
            .Take(maxFailures)
            .Select(test => FailureFormatting.BuildFailure(test, outputOptions))
            .Where(detail => detail is not null)
            .Select(detail => detail!)
            .ToArray();

        hasMoreFailures = failures.Count > details.Length;
        return details;
    }

    private static string AppendSummary(string message, string commandSummary)
        => string.IsNullOrEmpty(commandSummary) ? message : $"{message}{commandSummary}";
}