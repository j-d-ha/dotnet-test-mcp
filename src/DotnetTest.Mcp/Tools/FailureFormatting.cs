using DotnetTest.Mcp.Models;

namespace DotnetTest.Mcp.Tools;

internal static class FailureFormatting
{
    internal const int DefaultMaxFailureChars = 1500;
    internal const int DefaultMaxFailureLines = 40;
    internal const int DefaultTopLineChars = 200;
    internal const int DefaultMaxFailures = 1;
    internal const int DefaultErrorMaxCharsSummary = 400;
    internal const int DefaultErrorMaxCharsVerbose = 2000;
    internal const int DefaultErrorMaxLinesSummary = 12;
    internal const int DefaultErrorMaxLinesVerbose = 40;
    internal const int DefaultErrorSummaryChars = 200;

    internal sealed record FailureOutputOptions(
        OutputMode OutputMode,
        bool IncludeStackTrace,
        FailureDetailLevel DetailLevel,
        int MaxFailureChars,
        int MaxFailureLines,
        int TopLineMaxChars);

    internal static FailureOutputOptions CreateOptions(
        OutputMode outputMode,
        bool? includeStackTrace,
        int? maxFailureChars,
        int? maxFailureLines,
        FailureDetailLevel failureDetailLevel)
    {
        if (maxFailureChars is <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxFailureChars));

        if (maxFailureLines is <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxFailureLines));

        var resolvedMaxChars = maxFailureChars ?? DefaultMaxFailureChars;
        var resolvedMaxLines = maxFailureLines ?? DefaultMaxFailureLines;
        var resolvedIncludeStackTrace =
            includeStackTrace ?? failureDetailLevel == FailureDetailLevel.Full;
        var topLineMaxChars = Math.Min(DefaultTopLineChars, resolvedMaxChars);

        return new FailureOutputOptions(
            outputMode,
            resolvedIncludeStackTrace,
            failureDetailLevel,
            resolvedMaxChars,
            resolvedMaxLines,
            topLineMaxChars);
    }

    internal static int ResolveMaxFailures(int? maxFailures)
    {
        if (maxFailures is < 0)
            throw new ArgumentOutOfRangeException(nameof(maxFailures));

        return maxFailures ?? DefaultMaxFailures;
    }

    internal static TestFailure? BuildFailure(CtrfTest test, FailureOutputOptions options)
    {
        if (options.DetailLevel == FailureDetailLevel.None)
            return null;

        var testName = string.IsNullOrWhiteSpace(test.Name) ? test.Id ?? string.Empty : test.Name;

        var (topLine, topLineTruncated) = GetTopLine(
            test.Message,
            test.Trace,
            options.TopLineMaxChars);

        TruncatedText? message = null;
        if (options.DetailLevel == FailureDetailLevel.Full)
            message = TextTruncation.Truncate(
                test.Message,
                options.MaxFailureChars,
                options.MaxFailureLines);

        TruncatedText? stackTrace = null;
        if (options.DetailLevel == FailureDetailLevel.Full && options.IncludeStackTrace)
            stackTrace = TextTruncation.Truncate(
                test.Trace,
                options.MaxFailureChars,
                options.MaxFailureLines);

        string? diffSummary = null;
        string? diffContext = null;
        if (options.DetailLevel is FailureDetailLevel.DiffSnippet or FailureDetailLevel.Full)
            (diffSummary, diffContext) = ExtractDiff(
                test.Message,
                test.Snippet,
                options.TopLineMaxChars,
                options.MaxFailureChars,
                options.MaxFailureLines);

        return new TestFailure(
            testName,
            topLine,
            topLineTruncated,
            message,
            stackTrace,
            diffSummary,
            diffContext);
    }

    internal static (string? TopLine, bool Truncated) GetTopLine(
        string? message,
        string? trace,
        int maxChars)
    {
        var line = FirstNonEmptyLine(message) ?? FirstNonEmptyLine(trace);
        if (string.IsNullOrWhiteSpace(line))
            return (null, false);

        if (line.Length <= maxChars)
            return (line, false);

        return (line.Substring(0, maxChars), true);
    }

    private static string? FirstNonEmptyLine(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;

        using var reader = new StringReader(text);
        string? line;
        while ((line = reader.ReadLine()) is not null)
            if (!string.IsNullOrWhiteSpace(line))
                return line.Trim();

        return null;
    }

    private static (string? Summary, string? Context) ExtractDiff(
        string? message,
        string? snippet,
        int maxSummaryChars,
        int maxContextChars,
        int maxContextLines)
    {
        string? diffContext = null;
        if (!string.IsNullOrWhiteSpace(snippet))
            diffContext = TextTruncation.Truncate(snippet, maxContextChars, maxContextLines)?.Text;

        if (string.IsNullOrWhiteSpace(message))
            return (null, diffContext);

        string? expected = null;
        string? actual = null;
        var diffLines = new List<string>();
        var captureDiff = false;

        using var reader = new StringReader(message);
        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            var trimmed = line.Trim();
            if (expected is null
                && trimmed.StartsWith("Expected:", StringComparison.OrdinalIgnoreCase))
                expected = trimmed.Substring("Expected:".Length).Trim();

            if (actual is null && trimmed.StartsWith("Actual:", StringComparison.OrdinalIgnoreCase))
                actual = trimmed.Substring("Actual:".Length).Trim();

            if (trimmed.StartsWith("Diff:", StringComparison.OrdinalIgnoreCase))
            {
                captureDiff = true;
                var remainder = trimmed.Substring("Diff:".Length).Trim();
                if (!string.IsNullOrEmpty(remainder))
                    diffLines.Add(remainder);
                continue;
            }

            if (captureDiff)
            {
                if (string.IsNullOrWhiteSpace(trimmed) || diffLines.Count >= 6)
                    break;

                diffLines.Add(trimmed);
            }
        }

        string? summary = null;
        if (expected is not null || actual is not null)
        {
            var summaryText =
                expected is not null && actual is not null
                    ?
                    $"Expected: {expected}; Actual: {actual}"
                    : expected is not null
                        ? $"Expected: {expected}"
                        : $"Actual: {actual}";

            summary = TrimToMax(summaryText, maxSummaryChars);
        }
        else if (diffLines.Count > 0)
        {
            summary = TrimToMax($"Diff: {diffLines[0]}", maxSummaryChars);
        }

        if (diffContext is null && diffLines.Count > 0)
        {
            var diffText = string.Join("\n", diffLines);
            diffContext = TextTruncation.Truncate(diffText, maxContextChars, maxContextLines)?.Text;
        }

        return (summary, diffContext);
    }

    private static string TrimToMax(string value, int maxChars)
        => value.Length <= maxChars ? value : value.Substring(0, maxChars);
}