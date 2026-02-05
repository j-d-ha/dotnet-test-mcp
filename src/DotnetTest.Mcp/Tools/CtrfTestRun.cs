using System.Text.Json;
using DotnetTest.Mcp.Models;
using DotnetTest.Mcp.Terminal;

namespace DotnetTest.Mcp.Tools;

internal static class CtrfTestRun
{
    internal const int MaxFailureTextLength = 4000;

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

    internal static string BuildCommandSummary(CommandResult commandResult)
    {
        if (!string.IsNullOrWhiteSpace(commandResult.StandardError))
        {
            var output = TrimToLimit(commandResult.StandardError, MaxFailureTextLength)
                ?? string.Empty;
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