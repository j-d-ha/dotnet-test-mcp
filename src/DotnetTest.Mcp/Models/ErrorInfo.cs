namespace DotnetTest.Mcp.Models;

public sealed record ErrorInfo(
    ErrorKind Kind,
    string Summary,
    TruncatedText? Stdout,
    TruncatedText? Stderr);