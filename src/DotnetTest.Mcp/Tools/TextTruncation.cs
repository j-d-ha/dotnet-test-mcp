using System.Text;
using DotnetTest.Mcp.Models;

namespace DotnetTest.Mcp.Tools;

internal static class TextTruncation
{
    internal static TruncatedText? Truncate(string? value, int maxChars, int maxLines)
    {
        if (string.IsNullOrEmpty(value))
            return null;

        var originalCharCount = value.Length;
        var originalLineCount = CountLines(value);
        var truncated = false;
        var text = value;

        if (maxLines > 0)
        {
            text = TruncateLines(text, maxLines, out var linesTruncated);
            truncated |= linesTruncated;
        }

        if (maxChars > 0 && text.Length > maxChars)
        {
            text = text.Substring(0, maxChars);
            truncated = true;
        }

        return new TruncatedText(text, truncated, originalCharCount, originalLineCount);
    }

    private static int CountLines(string value)
    {
        if (value.Length == 0)
            return 0;

        var count = 1;
        foreach (var character in value)
            if (character == '\n')
                count++;

        return count;
    }

    private static string TruncateLines(string value, int maxLines, out bool truncated)
    {
        truncated = false;

        using var reader = new StringReader(value);
        var builder = new StringBuilder();
        string? line;
        var lineCount = 0;

        while ((line = reader.ReadLine()) is not null)
        {
            if (lineCount >= maxLines)
            {
                truncated = true;
                break;
            }

            if (lineCount > 0)
                builder.Append('\n');

            builder.Append(line);
            lineCount++;
        }

        return truncated ? builder.ToString() : value;
    }
}