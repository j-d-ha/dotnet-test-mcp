using System.Text.Json;
using DotnetTest.Mcp.Models;
using DotnetTest.Mcp.Terminal;

namespace DotnetTest.Mcp.Tools;

internal static class CtrfTestRun
{
    private const int DefaultErrorMaxChars = 8000;
    private const int DefaultErrorMaxLines = 200;

    internal sealed record Result(
        CommandResult CommandResult,
        CtrfReport? Report,
        bool ReportFileFound,
        string? ReadErrorMessage);

    internal static async Task<Result> ExecuteAsync(
        ICommandRunner commandRunner,
        JsonSerializerOptions jsonOptions,
        IReadOnlyList<string> arguments,
        CancellationToken cancellationToken)
    {
        var ctrfFileName = $"TestResults_{Guid.NewGuid():N}.ctrf";
        var tempPath = Path.GetTempPath();
        var fullCtrfPath = Path.Combine(tempPath, ctrfFileName);

        var argumentsWithCtrf = new List<string>(arguments)
        {
            "--report-ctrf",
            "--report-ctrf-filename",
            ctrfFileName,
            "--results-directory",
            tempPath,
            "--no-ansi",
            "--no-progress",
        };

        var commandResult = await commandRunner.RunAsync(
            new CommandRequest("dotnet", argumentsWithCtrf.ToArray())
            {
                ThrowOnNonZeroExitCode = false,
            },
            cancellationToken);

        CtrfReport? report = null;
        string? readErrorMessage = null;
        var reportFileFound = File.Exists(fullCtrfPath);

        try
        {
            if (reportFileFound)
            {
                var json = await File.ReadAllTextAsync(fullCtrfPath, cancellationToken);
#pragma warning disable IL2026, IL3050
                report = JsonSerializer.Deserialize<CtrfReport>(json, jsonOptions);
#pragma warning restore IL2026, IL3050
                if (report is null)
                    readErrorMessage = "Failed to read test results.";
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            readErrorMessage = "Failed to read test results.";
        }
        finally
        {
            TryDelete(fullCtrfPath);
        }

        return new Result(commandResult, report, reportFileFound, readErrorMessage);
    }

    internal static ErrorKind ClassifyErrorKind(
        CommandResult commandResult,
        bool reportFileFound,
        string? readErrorMessage)
    {
        if (!string.IsNullOrWhiteSpace(readErrorMessage))
            return ErrorKind.ReadFailed;

        var output = BuildOutputCombined(commandResult);
        if (ContainsErrorToken(
                output,
                "Specifying a project for 'dotnet test' should be via '--project'")
            || ContainsErrorToken(output, "Unrecognized command or argument")
            || ContainsErrorToken(output, "Unknown option"))
            return ErrorKind.InvocationError;

        if (ContainsErrorToken(output, "testhost")
            || ContainsErrorToken(output, "TypeLoadException")
            || ContainsErrorToken(output, "Unhandled exception"))
            return ErrorKind.TestHostCrashed;

        if (ContainsErrorToken(output, "Build FAILED")
            || ContainsErrorToken(output, "error CS")
            || ContainsErrorToken(output, "error MSB"))
            return ErrorKind.BuildFailed;

        if (ContainsErrorToken(output, "Discovered 0 tests")
            || ContainsErrorToken(output, "No tests available")
            || ContainsErrorToken(output, "No test is available")
            || ContainsErrorToken(output, "No tests to run"))
            return ErrorKind.NoTestsDiscovered;

        if (ContainsErrorToken(output, "Test discovery")
            || ContainsErrorToken(output, "Discovering tests")
            || ContainsErrorToken(output, "Failed to discover tests"))
            return ErrorKind.DiscoveryFailed;

        if (!reportFileFound)
            return ErrorKind.ResultFileMissing;

        return ErrorKind.Unknown;
    }

    internal static ErrorInfo BuildErrorInfo(
        CommandResult commandResult,
        string? readErrorMessage,
        ErrorKind errorKind)
    {
        var stderr = TextTruncation.Truncate(
            commandResult.StandardError,
            DefaultErrorMaxChars,
            DefaultErrorMaxLines);
        var stdout = TextTruncation.Truncate(
            commandResult.StandardOutput,
            DefaultErrorMaxChars,
            DefaultErrorMaxLines);

        var reason = readErrorMessage;
        if (string.IsNullOrWhiteSpace(reason))
        {
            if (!string.IsNullOrWhiteSpace(commandResult.StandardError))
                reason = commandResult.StandardError;
            else if (!string.IsNullOrWhiteSpace(commandResult.StandardOutput))
                reason = commandResult.StandardOutput;
            else
                reason = "Test execution failed.";
        }

        return new ErrorInfo(errorKind, reason, stdout, stderr);
    }

    internal static string BuildCommandSummary(CommandResult commandResult)
    {
        if (!string.IsNullOrWhiteSpace(commandResult.StandardError))
            return $" ExitCode={commandResult.ExitCode}. Stderr: {commandResult.StandardError}";

        if (!string.IsNullOrWhiteSpace(commandResult.StandardOutput))
            return $" ExitCode={commandResult.ExitCode}. Stdout: {commandResult.StandardOutput}";

        return commandResult.ExitCode == 0 ? string.Empty : $" ExitCode={commandResult.ExitCode}.";
    }

    internal static int? TryParseDurationMilliseconds(long? duration)
    {
        if (!duration.HasValue || duration.Value <= 0)
            return null;

        return duration.Value > int.MaxValue ? int.MaxValue : (int)duration.Value;
    }


    private static string BuildOutputCombined(CommandResult commandResult)
    {
        if (string.IsNullOrWhiteSpace(commandResult.StandardError))
            return commandResult.StandardOutput ?? string.Empty;

        if (string.IsNullOrWhiteSpace(commandResult.StandardOutput))
            return commandResult.StandardError ?? string.Empty;

        return string.Concat(
            commandResult.StandardError,
            Environment.NewLine,
            commandResult.StandardOutput);
    }

    private static bool ContainsErrorToken(string source, string token)
        => source.Contains(token, StringComparison.OrdinalIgnoreCase);

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
