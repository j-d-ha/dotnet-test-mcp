using System.ComponentModel;
using System.Text.Json;
using DotnetTest.Mcp.Models;
using DotnetTest.Mcp.Terminal;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Server;

namespace DotnetTest.Mcp.Tools;

[McpServerToolType]
public sealed class RunSingleTestTool(
    ICommandRunner commandRunner,
    JsonSerializerOptions jsonOptions,
    IOptions<McpOptions> options)
{
    private readonly ICommandRunner _commandRunner = commandRunner.ValidateNotNull();
    private readonly JsonSerializerOptions _jsonOptions = jsonOptions.ValidateNotNull();
    private readonly McpOptions _options = options.Value.ValidateNotNull();

    [McpServerTool(UseStructuredContent = true)]
    [Description("Runs a single dotnet test by fully qualified method name.")]
    public async Task<Result> RunSingleTest(
        [Description(
            """
            Fully qualified test method name.
            Example: MyNamespace.MyClass.MyMethod
            """)]
        string qualifiedMethodName,
        [Description("Include stack trace in failure details. Default is false.")]
        bool includeStackTrace = false,
        [Description("Optional project path to scope the method run to a single test project.")]
        string? project = null,
        [Description("Optional working directory to run dotnet commands from (useful for git worktrees).")]
        string? workingDirectory = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(qualifiedMethodName))
            throw new ArgumentException(
                "Qualified method name is required.",
                nameof(qualifiedMethodName));

        var trimmedQualifiedName = qualifiedMethodName.Trim();
        var trimmedProject = string.IsNullOrWhiteSpace(project) ? null : project.Trim();
        var trimmedWorkingDirectory = string.IsNullOrWhiteSpace(workingDirectory) ? null : workingDirectory.Trim();
        var dialect = TestRunnerDialectDetector.Detect(trimmedProject, _options, trimmedWorkingDirectory);
        var supportsCtrf = dialect != TestRunnerDialect.TUnit;

        var runResult = await CtrfTestRun.ExecuteAsync(
            _commandRunner,
            _jsonOptions,
            TestCommandBuilder.BuildSingleTestRun(dialect, trimmedQualifiedName, trimmedProject),
            _options.DisableCtrf,
            supportsCtrf,
            trimmedWorkingDirectory,
            cancellationToken);

        if (runResult.Report is null)
        {
            if (!supportsCtrf && runResult.CommandResult.ExitCode is 0 or 1)
            {
                var summary = ConsoleTestSummaryParser.Parse(runResult.CommandResult);
                var testOutcome = ConsoleTestSummaryParser.GetOutcome(runResult.CommandResult, summary);
                return CreateResult(
                    testOutcome,
                    testOutcome == TestOutcome.Passed ? "Test passed." : ConsoleTestSummaryParser.GetMessage(testOutcome),
                    summary.DurationMilliseconds,
                    error: testOutcome == TestOutcome.Error
                        ? CtrfTestRun.BuildErrorInfo(
                            runResult.CommandResult,
                            null,
                            CtrfTestRun.ClassifyErrorKind(runResult.CommandResult, false, null))
                        : null);
            }

            if (runResult is { ReportFileFound: false, CommandResult.ExitCode: 8 })
                return CreateResult(TestOutcome.NotFound, "No tests matched the method filter.");

            var errorKind = CtrfTestRun.ClassifyErrorKind(
                runResult.CommandResult,
                runResult.ReportFileFound,
                runResult.ReadErrorMessage);
            var error = CtrfTestRun.BuildErrorInfo(
                runResult.CommandResult,
                runResult.ReadErrorMessage,
                errorKind);
            return CreateResult(TestOutcome.Error, error.Reason, error: error);
        }

        var tests = runResult.Report.Results.Tests;
        if (tests.Count == 0)
            return CreateResult(TestOutcome.NotFound, "No tests matched the method filter.");

        var exactMatches = tests.Where(test
                => string.Equals(test.Name, trimmedQualifiedName, StringComparison.Ordinal))
            .ToList();

        if (exactMatches.Count == 0)
            return CreateResult(TestOutcome.NotFound, "No tests matched the method filter.");

        if (exactMatches.Count > 1)
            return CreateResult(
                TestOutcome.Ambiguous,
                "Filter matched multiple test cases; refine the qualified method name.");

        var singleResult = exactMatches[0];
        var outcome = MapOutcome(singleResult.Status);
        var durationMilliseconds = TryParseDurationMilliseconds(singleResult.Duration);

        string? failureMessage = null;
        string? failureStackTrace = null;

        if (outcome == TestOutcome.Failed)
        {
            failureMessage = string.IsNullOrWhiteSpace(singleResult.Message)
                ? null
                : singleResult.Message;
            failureStackTrace = includeStackTrace && !string.IsNullOrWhiteSpace(singleResult.Trace)
                ? singleResult.Trace
                : null;
        }

        var message = outcome switch
        {
            TestOutcome.Passed => "Test passed.",
            TestOutcome.Failed => "Test failed.",
            TestOutcome.Skipped => "Test skipped.",
            _ => "Test finished with an unknown outcome.",
        };

        return CreateResult(
            outcome,
            message,
            durationMilliseconds,
            failureMessage,
            failureStackTrace);

        Result CreateResult(
            TestOutcome resultOutcome,
            string resultMessage,
            int? durationMs = null,
            string? resultFailureMessage = null,
            string? resultFailureStackTrace = null,
            ErrorInfo? error = null)
            => new(
                trimmedQualifiedName,
                resultOutcome,
                resultMessage,
                durationMs,
                resultFailureMessage,
                resultFailureStackTrace,
                error);
    }

    [Description("Result of running one requested test method.")]
    public record Result(
        [property: Description("Fully qualified test method name requested.")]
        string RequestedTestName,
        [property: Description("Outcome of the requested single-test run.")] TestOutcome Outcome,
        [property: Description("Short summary (<= 1-2 lines).")] string Message,
        [property: Description("Test duration in milliseconds when available; otherwise null.")]
        int? DurationMilliseconds,
        [property: Description("Failure message when Outcome is Failed; otherwise null.")]
        string? FailureMessage,
        [property: Description("Failure stack trace when Outcome is Failed; otherwise null.")]
        string? FailureStackTrace,
        [property: Description("Structured error details when Outcome is Error; otherwise null.")]
        ErrorInfo? Error);

    private static TestOutcome MapOutcome(string? status)
        => status?.ToLowerInvariant() switch
        {
            "passed" => TestOutcome.Passed,
            "failed" => TestOutcome.Failed,
            "skipped" => TestOutcome.Skipped,
            "pending" => TestOutcome.Skipped,
            _ => TestOutcome.Error,
        };

    private static int? TryParseDurationMilliseconds(long duration)
    {
        if (duration <= 0)
            return null;

        return duration > int.MaxValue ? int.MaxValue : (int)duration;
    }
}
