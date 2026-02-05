using System.ComponentModel;
using System.Text.Json;
using DotnetTest.Mcp.Models;
using DotnetTest.Mcp.Terminal;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

namespace DotnetTest.Mcp;

[McpServerToolType]
public sealed class RunSingleTestTool(
    ICommandRunner commandRunner,
    JsonSerializerOptions jsonOptions,
    ILogger<RunSingleTestTool> logger)
{
    private const int MaxFailureTextLength = 4000;

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
        CancellationToken cancellationToken)
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
                var commandSummary = BuildCommandSummary(commandResult);
                return CreateResult(
                    TestOutcome.Error,
                    string.IsNullOrEmpty(commandSummary)
                        ? "Test result file not found."
                        : $"Test result file not found.{commandSummary}");
            }

            var json = await File.ReadAllTextAsync(fullCtrfPath, cancellationToken);
#pragma warning disable IL2026, IL3050
            var report = JsonSerializer.Deserialize<CtrfReport>(json, _jsonOptions);
#pragma warning restore IL2026, IL3050
            if (report is null)
                return CreateResult(TestOutcome.Error, "Failed to read test results.");

            var results = report.Results.Tests;
            if (results.Count == 0)
                return CreateResult(TestOutcome.NotFound, "No tests matched the filter.");

            if (results.Count == 1)
                return CreateResultFromTest(results[0]);

            var matchingResults = results.Where(test
                    => string.Equals(test.Name, qualifiedTestName, StringComparison.Ordinal))
                .ToList();

            if (matchingResults.Count == 0)
                return CreateResult(TestOutcome.NotFound, "No tests matched the filter.");

            if (matchingResults.Count > 1)
                return CreateResult(
                    TestOutcome.Ambiguous,
                    "Filter matched multiple test cases; refine the qualified name.");

            return CreateResultFromTest(matchingResults[0]);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            return CreateResult(TestOutcome.Error, "Failed to read test results.");
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
            string? failureStackTrace = null)
            => new(
                qualifiedTestName,
                outcome,
                message,
                durationMilliseconds,
                failureMessage,
                failureStackTrace);

        Result CreateResultFromTest(CtrfTest singleResult)
        {
            var outcome = MapOutcome(singleResult.Status);
            var durationMilliseconds = TryParseDurationMilliseconds(singleResult.Duration);
            var failureMessage = TrimToLimit(singleResult.Message, MaxFailureTextLength);
            var failureStackTrace = TrimToLimit(singleResult.Trace, MaxFailureTextLength);

            if (outcome != TestOutcome.Failed)
            {
                failureMessage = null;
                failureStackTrace = null;
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
        string? FailureStackTrace);

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

    private static string? TrimToLimit(string? value, int maxLength)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        if (value.Length <= maxLength)
            return value;

        return value.Substring(0, maxLength);
    }

    private static string BuildCommandSummary(CommandResult commandResult)
    {
        if (!string.IsNullOrWhiteSpace(commandResult.StandardError))
        {
            var output =
                TrimToLimit(commandResult.StandardError, MaxFailureTextLength) ?? string.Empty;
            return $" ExitCode={commandResult.ExitCode}. Stderr: {output}";
        }

        if (!string.IsNullOrWhiteSpace(commandResult.StandardOutput))
        {
            var output = TrimToLimit(commandResult.StandardOutput, MaxFailureTextLength)
                ?? string.Empty;
            return $" ExitCode={commandResult.ExitCode}. Stdout: {output}";
        }

        return commandResult.ExitCode == 0 ? string.Empty : $" ExitCode={commandResult.ExitCode}.";
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