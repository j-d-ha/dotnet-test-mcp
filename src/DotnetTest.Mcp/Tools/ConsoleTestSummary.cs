using DotnetTest.Mcp.Models;
using DotnetTest.Mcp.Terminal;

namespace DotnetTest.Mcp.Tools;

internal sealed record ConsoleTestSummary(
    int TestCount,
    int Passed,
    int Failed,
    int Skipped,
    int Pending,
    int Other,
    int? DurationMilliseconds);

internal static class ConsoleTestSummaryParser
{
    internal static ConsoleTestSummary Parse(CommandResult commandResult)
    {
        var output = string.Join(
            Environment.NewLine,
            commandResult.StandardOutput,
            commandResult.StandardError);

        var passed = FindInt(output, "Passed");
        var failed = FindInt(output, "Failed");
        var skipped = FindInt(output, "Skipped");
        var total = FindInt(output, "Total");
        var duration = FindDurationMilliseconds(output);

        return new ConsoleTestSummary(
            total > 0 ? total : passed + failed + skipped,
            passed,
            failed,
            skipped,
            0,
            0,
            duration);
    }

    internal static TestOutcome GetOutcome(CommandResult commandResult, ConsoleTestSummary summary)
    {
        if (commandResult.ExitCode != 0 && summary.Failed == 0)
            return TestOutcome.Error;

        if (summary.TestCount == 0)
            return TestOutcome.NotFound;

        return summary.Failed > 0
            ? TestOutcome.Failed
            : summary.Passed > 0
                ? TestOutcome.Passed
                : TestOutcome.Skipped;
    }

    internal static string GetMessage(TestOutcome outcome)
        => outcome switch
        {
            TestOutcome.Passed => "All tests passed.",
            TestOutcome.Failed => "Test run completed with failures.",
            TestOutcome.Skipped => "Tests completed without failures.",
            TestOutcome.NotFound => "No tests matched the filter.",
            _ => "Test execution failed.",
        };

    private static int FindInt(string output, string label)
    {
        var index = output.IndexOf(label, StringComparison.OrdinalIgnoreCase);
        if (index < 0)
            return 0;

        index += label.Length;
        while (index < output.Length && !char.IsDigit(output[index]))
            index++;

        var start = index;
        while (index < output.Length && char.IsDigit(output[index]))
            index++;

        return int.TryParse(output[start..index], out var value) ? value : 0;
    }

    private static int? FindDurationMilliseconds(string output)
    {
        var index = output.IndexOf("Duration", StringComparison.OrdinalIgnoreCase);
        if (index < 0)
            return null;

        index += "Duration".Length;
        while (index < output.Length && !char.IsDigit(output[index]))
            index++;

        var start = index;
        while (index < output.Length && char.IsDigit(output[index]))
            index++;

        return int.TryParse(output[start..index], out var value) ? value : null;
    }
}
