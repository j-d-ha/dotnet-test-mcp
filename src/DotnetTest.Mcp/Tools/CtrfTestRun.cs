using System.Text.Json;
using DotnetTest.Mcp.Models;
using DotnetTest.Mcp.Terminal;

namespace DotnetTest.Mcp.Tools;

internal static class CtrfTestRun
{
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
        if (ContainsErrorToken(output, "testhost")
            || ContainsErrorToken(output, "TypeLoadException")
            || ContainsErrorToken(output, "Unhandled exception"))
            return ErrorKind.TestHostCrashed;

        if (ContainsErrorToken(output, "Build FAILED")
            || ContainsErrorToken(output, "error CS")
            || ContainsErrorToken(output, "error MSB"))
            return ErrorKind.BuildFailed;

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
        OutputMode outputMode,
        ErrorKind errorKind)
    {
        var maxChars = outputMode == OutputMode.Verbose
            ? FailureFormatting.DefaultErrorMaxCharsVerbose
            : FailureFormatting.DefaultErrorMaxCharsSummary;
        var maxLines = outputMode == OutputMode.Verbose
            ? FailureFormatting.DefaultErrorMaxLinesVerbose
            : FailureFormatting.DefaultErrorMaxLinesSummary;

        var stderr = TextTruncation.Truncate(commandResult.StandardError, maxChars, maxLines);
        var stdout = TextTruncation.Truncate(commandResult.StandardOutput, maxChars, maxLines);

        var summary = readErrorMessage;
        if (string.IsNullOrWhiteSpace(summary))
        {
            var (topLine, _) = FailureFormatting.GetTopLine(
                commandResult.StandardError,
                commandResult.StandardOutput,
                FailureFormatting.DefaultErrorSummaryChars);
            summary = string.IsNullOrWhiteSpace(topLine) ? "Test execution failed." : topLine;
        }

        return new ErrorInfo(errorKind, summary, stdout, stderr);
    }

    internal static string BuildCommandSummary(CommandResult commandResult)
    {
        if (!string.IsNullOrWhiteSpace(commandResult.StandardError))
        {
            var output = TrimToLimit(
                    commandResult.StandardError,
                    FailureFormatting.DefaultMaxFailureChars)
                ?? string.Empty;
            return $" ExitCode={commandResult.ExitCode}. Stderr: {output}";
        }

        if (!string.IsNullOrWhiteSpace(commandResult.StandardOutput))
        {
            var output = TrimToLimit(
                    commandResult.StandardOutput,
                    FailureFormatting.DefaultMaxFailureChars)
                ?? string.Empty;
            return $" ExitCode={commandResult.ExitCode}. Stdout: {output}";
        }

        return commandResult.ExitCode == 0 ? string.Empty : $" ExitCode={commandResult.ExitCode}.";
    }

    internal static int? TryParseDurationMilliseconds(long? duration)
    {
        if (!duration.HasValue || duration.Value <= 0)
            return null;

        return duration.Value > int.MaxValue ? int.MaxValue : (int)duration.Value;
    }

    private static string? TrimToLimit(string? value, int maxLength)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        if (value.Length <= maxLength)
            return value;

        return value.Substring(0, maxLength);
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