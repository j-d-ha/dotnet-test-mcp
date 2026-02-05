using System.ComponentModel;
using System.Text.Json;
using DotnetTest.Mcp.Models;
using DotnetTest.Mcp.Terminal;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

namespace DotnetTest.Mcp.Tools;

[McpServerToolType]
public sealed class RunSingleTestTool(
    ICommandRunner commandRunner,
    JsonSerializerOptions jsonOptions,
    ILogger<RunSingleTestTool> logger)
{
    private readonly ICommandRunner _commandRunner = commandRunner.ValidateNotNull();
    private readonly JsonSerializerOptions _jsonOptions = jsonOptions.ValidateNotNull();
    private readonly ILogger<RunSingleTestTool> _logger = logger.ValidateNotNull();

    [McpServerTool(UseStructuredContent = true)]
    [Description("Runs a single dotnet test.")]
    public async Task<Result> RunSingleTest(
        [Description(
            """
            Qualified name of the test to run, including namespace, class, and method.
            Example: MyNamespace.MyClass.MyMethod
            """)]
        string qualifiedTestName,
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
        CancellationToken cancellationToken = default)
    {
        var ctrfFileName = $"TestResults_{Guid.NewGuid():N}.ctrf";

        var tempPath = Path.GetTempPath();
        var fullCtrfPath = Path.Combine(tempPath, ctrfFileName);

        var commandResult = await _commandRunner.RunAsync(
            new CommandRequest(
                "dotnet",
                "test",
                "--report-ctrf",
                "--filter-method",
                qualifiedTestName.Trim(),
                "--report-ctrf-filename",
                ctrfFileName,
                "--results-directory",
                tempPath,
                "--no-ansi",
                "--no-progress") { ThrowOnNonZeroExitCode = false },
            cancellationToken);

        try
        {
            if (!File.Exists(fullCtrfPath))
            {
                if (commandResult.ExitCode == 8)
                    return CreateResult(TestOutcome.NotFound, "No tests matched the filter.");

                var errorKind = CtrfTestRun.ClassifyErrorKind(commandResult, false, null);
                var error = CtrfTestRun.BuildErrorInfo(commandResult, null, outputMode, errorKind);
                return CreateResult(TestOutcome.Error, error.Summary, error: error);
            }

            var json = await File.ReadAllTextAsync(fullCtrfPath, cancellationToken);
#pragma warning disable IL2026, IL3050
            var report = JsonSerializer.Deserialize<CtrfReport>(json, _jsonOptions);
#pragma warning restore IL2026, IL3050
            if (report is null)
            {
                var error = CtrfTestRun.BuildErrorInfo(
                    commandResult,
                    "Failed to read test results.",
                    outputMode,
                    ErrorKind.ReadFailed);
                return CreateResult(TestOutcome.Error, error.Summary, error: error);
            }

            var results = report.Results.Tests;
            if (results.Count == 0)
                return CreateResult(TestOutcome.NotFound, "No tests matched the filter.");

            var outputOptions = FailureFormatting.CreateOptions(
                outputMode,
                includeStackTrace,
                maxFailureChars,
                maxFailureLines,
                failureDetailLevel);

            if (results.Count == 1)
                return CreateResultFromTest(results[0], outputOptions);

            var matchingResults = results.Where(test
                    => string.Equals(test.Name, qualifiedTestName, StringComparison.Ordinal))
                .ToList();

            if (matchingResults.Count == 0)
                return CreateResult(TestOutcome.NotFound, "No tests matched the filter.");

            if (matchingResults.Count > 1)
                return CreateResult(
                    TestOutcome.Ambiguous,
                    "Filter matched multiple test cases; refine the qualified name.");

            return CreateResultFromTest(matchingResults[0], outputOptions);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            var error = CtrfTestRun.BuildErrorInfo(
                commandResult,
                "Failed to read test results.",
                outputMode,
                ErrorKind.ReadFailed);
            return CreateResult(TestOutcome.Error, error.Summary, error: error);
        }
        finally
        {
            TryDelete(fullCtrfPath);
        }

        Result CreateResult(
            TestOutcome outcome,
            string message,
            int? durationMilliseconds = null,
            string? failureMessage = null,
            string? failureStackTrace = null,
            TestFailure? failure = null,
            ErrorInfo? error = null)
            => new(
                qualifiedTestName,
                outcome,
                message,
                durationMilliseconds,
                failureMessage,
                failureStackTrace,
                failure,
                error);

        Result CreateResultFromTest(
            CtrfTest singleResult,
            FailureFormatting.FailureOutputOptions outputOptions)
        {
            var outcome = MapOutcome(singleResult.Status);
            var durationMilliseconds = TryParseDurationMilliseconds(singleResult.Duration);
            TestFailure? failure = null;
            string? failureMessage = null;
            string? failureStackTrace = null;

            if (outcome != TestOutcome.Failed)
            {
                failureMessage = null;
                failureStackTrace = null;
                failure = null;
            }
            else
            {
                failure = FailureFormatting.BuildFailure(singleResult, outputOptions);
                if (failure is not null)
                {
                    failureMessage = outputOptions.DetailLevel == FailureDetailLevel.Full
                        ? failure.Message?.Text
                        : failure.TopLine;
                    failureStackTrace = failure.StackTrace?.Text;
                }
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
                failureStackTrace,
                failure);
        }
    }

    [Description("Result of running one requested test method.")]
    public record Result(
        [property: Description("Fully qualified test name requested (namespace.class.method).")]
        string RequestedTestName,
        [property: Description("Outcome of the requested single-test run.")] TestOutcome Outcome,
        [property: Description("Short summary (<= 1-2 lines).")] string Message,
        [property: Description("Test duration in milliseconds when available; otherwise null.")]
        int? DurationMilliseconds,
        [property: Description("Failure message when Outcome is Failed; otherwise null.")]
        string? FailureMessage,
        [property:
            Description(
                "Failure stack trace when Outcome is Failed; otherwise null. May be truncated.")]
        string? FailureStackTrace,
        [property:
            Description("Structured failure details when Outcome is Failed; otherwise null.")]
        TestFailure? Failure,
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

    private static void TryDelete(string path)
    {
        if (!File.Exists(path))
            return;

        try
        {
            File.Delete(path);
        }
        catch
        {
            // Ignored.
        }
    }
}