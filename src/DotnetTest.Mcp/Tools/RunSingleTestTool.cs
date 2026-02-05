using System.ComponentModel;
using System.Text.Json;
using DotnetTest.Mcp.Models;
using DotnetTest.Mcp.Terminal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Server;

namespace DotnetTest.Mcp.Tools;

[McpServerToolType]
public sealed class RunSingleTestTool(
    ICommandRunner commandRunner,
    IOptions<McpOptions> options,
    JsonSerializerOptions jsonOptions,
    ILogger<RunSingleTestTool> logger)
{
    private readonly ICommandRunner _commandRunner = commandRunner.ValidateNotNull();
    private readonly McpOptions _options = options.ValidateNotNull().Value;
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
        [Description("Optional path to project file to scope the run.")] string? projectPath = null,
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
        var trimmedQualifiedName = qualifiedTestName.Trim();
        var trimmedProjectPath = string.IsNullOrWhiteSpace(projectPath) ? null : projectPath.Trim();

        if (trimmedProjectPath is null)
        {
            var candidates = await TestProjectDiscovery.ListAsync(
                _commandRunner,
                _options,
                cancellationToken);
            if (candidates.Length == 0)
                return CreateResult(
                    TestOutcome.Ambiguous,
                    "Specify projectPath; no test projects discovered.");

            var matches = new List<ProjectMatch>();
            var attempts = new List<ProjectAttempt>();

            foreach (var candidate in candidates)
            {
                var probe =
                    await ExecuteSingleTestAsync(
                        candidate,
                        trimmedQualifiedName,
                        cancellationToken);

                if (probe.ReportFileFound && probe.Report is not null)
                {
                    var matchingResults =
                        probe.Report
                            .Results
                            .Tests
                            .Where(test => string.Equals(
                                test.Name,
                                trimmedQualifiedName,
                                StringComparison.Ordinal))
                            .ToList();
                    if (matchingResults.Count > 0)
                    {
                        matches.Add(new ProjectMatch(candidate, probe));
                        if (matches.Count > 1)
                            break;
                        continue;
                    }
                }

                var errorKind = CtrfTestRun.ClassifyErrorKind(
                    probe.CommandResult,
                    probe.ReportFileFound,
                    probe.ReadErrorMessage);

                if (probe.CommandResult.ExitCode == 8 || errorKind == ErrorKind.NoTestsDiscovered)
                    continue;

                var error = CtrfTestRun.BuildErrorInfo(
                    probe.CommandResult,
                    probe.ReadErrorMessage,
                    OutputMode.Summary,
                    errorKind);
                attempts.Add(
                    new ProjectAttempt(
                        candidate,
                        TestOutcome.Error,
                        probe.CommandResult.ExitCode,
                        errorKind,
                        error.Summary));
            }

            if (matches.Count == 1)
            {
                var match = matches[0];
                return BuildResultFromRun(
                    trimmedQualifiedName,
                    match.ProjectPath,
                    match.Run,
                    outputMode,
                    includeStackTrace,
                    maxFailureChars,
                    maxFailureLines,
                    failureDetailLevel);
            }

            if (matches.Count > 1)
                return CreateResult(
                    TestOutcome.Ambiguous,
                    "Filter matched multiple projects; specify projectPath.",
                    candidateProjects: matches.Select(match => match.ProjectPath).ToArray());

            if (attempts.Count > 0)
                return CreateResult(
                    TestOutcome.Error,
                    "Failed to determine owning project.",
                    error: new ErrorInfo(
                        ErrorKind.Unknown,
                        "Failed to determine owning project.",
                        null,
                        null),
                    projectAttempts: attempts.ToArray());

            return CreateResult(TestOutcome.NotFound, "No tests matched the filter.");
        }

        var commandResult = await ExecuteSingleTestAsync(
            trimmedProjectPath,
            trimmedQualifiedName,
            cancellationToken);
        return BuildResultFromRun(
            trimmedQualifiedName,
            trimmedProjectPath,
            commandResult,
            outputMode,
            includeStackTrace,
            maxFailureChars,
            maxFailureLines,
            failureDetailLevel);

        Result CreateResult(
            TestOutcome outcome,
            string message,
            int? durationMilliseconds = null,
            string? failureMessage = null,
            string? failureStackTrace = null,
            TestFailure? failure = null,
            ErrorInfo? error = null,
            string? usedProjectPath = null,
            string[]? candidateProjects = null,
            ProjectAttempt[]? projectAttempts = null)
            => new(
                qualifiedTestName,
                usedProjectPath,
                candidateProjects,
                outcome,
                message,
                durationMilliseconds,
                failureMessage,
                failureStackTrace,
                failure,
                error,
                projectAttempts);

        async Task<CtrfTestRun.Result> ExecuteSingleTestAsync(
            string projectPathValue,
            string qualifiedName,
            CancellationToken token)
        {
            var arguments = new List<string>
            {
                "test",
                "--project",
                projectPathValue,
                "--filter-method",
                qualifiedName,
            };

            return await CtrfTestRun.ExecuteAsync(_commandRunner, _jsonOptions, arguments, token);
        }

        Result CreateResultFromTest(
            CtrfTest singleResult,
            FailureFormatting.FailureOutputOptions outputOptions,
            string usedProjectPath)
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
                failure,
                usedProjectPath: usedProjectPath);
        }

        Result BuildResultFromRun(
            string qualifiedName,
            string usedProjectPath,
            CtrfTestRun.Result run,
            OutputMode currentOutputMode,
            bool? stackTraceSetting,
            int? maxChars,
            int? maxLines,
            FailureDetailLevel detailLevel)
        {
            if (!run.ReportFileFound)
            {
                if (run.CommandResult.ExitCode == 8)
                    return CreateResult(
                        TestOutcome.NotFound,
                        "No tests matched the filter.",
                        usedProjectPath: usedProjectPath);

                var errorKind = CtrfTestRun.ClassifyErrorKind(
                    run.CommandResult,
                    run.ReportFileFound,
                    run.ReadErrorMessage);
                var error = CtrfTestRun.BuildErrorInfo(
                    run.CommandResult,
                    run.ReadErrorMessage,
                    currentOutputMode,
                    errorKind);
                return CreateResult(
                    TestOutcome.Error,
                    error.Summary,
                    error: error,
                    usedProjectPath: usedProjectPath);
            }

            if (run.Report is null)
            {
                var error = CtrfTestRun.BuildErrorInfo(
                    run.CommandResult,
                    run.ReadErrorMessage ?? "Failed to read test results.",
                    currentOutputMode,
                    ErrorKind.ReadFailed);
                return CreateResult(
                    TestOutcome.Error,
                    error.Summary,
                    error: error,
                    usedProjectPath: usedProjectPath);
            }

            var results = run.Report.Results.Tests;
            if (results.Count == 0)
                return CreateResult(
                    TestOutcome.NotFound,
                    "No tests matched the filter.",
                    usedProjectPath: usedProjectPath);

            var outputOptions = FailureFormatting.CreateOptions(
                currentOutputMode,
                stackTraceSetting,
                maxChars,
                maxLines,
                detailLevel);

            if (results.Count == 1)
                return CreateResultFromTest(results[0], outputOptions, usedProjectPath);

            var matchingResults = results.Where(test
                    => string.Equals(test.Name, qualifiedName, StringComparison.Ordinal))
                .ToList();

            if (matchingResults.Count == 0)
                return CreateResult(
                    TestOutcome.NotFound,
                    "No tests matched the filter.",
                    usedProjectPath: usedProjectPath);

            if (matchingResults.Count > 1)
                return CreateResult(
                    TestOutcome.Ambiguous,
                    "Filter matched multiple test cases; refine the qualified name.",
                    usedProjectPath: usedProjectPath);

            return CreateResultFromTest(matchingResults[0], outputOptions, usedProjectPath);
        }
    }

    [Description("Result of running one requested test method.")]
    public record Result(
        [property: Description("Fully qualified test name requested (namespace.class.method).")]
        string RequestedTestName,
        [property:
            Description("Project path used when the owning project is known; otherwise null.")]
        string? ProjectPath,
        [property:
            Description("Candidate project paths when Outcome is Ambiguous; otherwise null.")]
        string[]? CandidateProjects,
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
        ErrorInfo? Error,
        [property: Description("Per-project attempt summaries when resolving ownership.")]
        ProjectAttempt[]? ProjectAttempts);

    private sealed record ProjectMatch(string ProjectPath, CtrfTestRun.Result Run);

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
