namespace DotnetTest.Mcp.Terminal;

public interface ICommandRunner : IAsyncDisposable
{
    Task<CommandResult> RunAsync(
        CommandRequest request,
        CancellationToken cancellationToken = default);
}

public sealed record CommandRequest(string FileName, params string[] Arguments)
{
    public string? WorkingDirectory { get; init; }

    public IReadOnlyDictionary<string, string?>? Environment { get; init; }

    public TimeSpan? Timeout { get; init; }

    public bool ThrowOnNonZeroExitCode { get; init; } = true;

    public int? MaxOutputChars { get; init; }
}

public sealed record CommandResult(
    int ExitCode,
    string StandardOutput,
    string StandardError,
    string[] StandardOutputLines,
    string[] StandardErrorLines,
    TimeSpan Duration);
