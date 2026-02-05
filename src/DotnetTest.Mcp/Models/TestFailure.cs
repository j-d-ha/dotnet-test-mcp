namespace DotnetTest.Mcp.Models;

public sealed record TestFailure(
    string TestName,
    string? TopLine,
    bool TopLineTruncated,
    TruncatedText? Message,
    TruncatedText? StackTrace,
    string? DiffSummary,
    string? DiffContext);