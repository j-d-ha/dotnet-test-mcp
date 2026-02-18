namespace DotnetTest.Mcp.Models;

public sealed record ErrorInfo(
    ErrorKind Kind,
    string Reason,
    TruncatedText? Stdout,
    TruncatedText? Stderr);
