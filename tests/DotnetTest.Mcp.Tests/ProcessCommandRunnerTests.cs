using DotnetTest.Mcp.Terminal;
using Microsoft.Extensions.Options;
using Xunit;

namespace DotnetTest.Mcp.Tests;

public sealed class ProcessCommandRunnerTests
{
    [Fact]
    public async Task RunAsync_WhenProcessTimesOut_ReturnsCapturedStdoutAndStderr()
    {
        await using var runner = new ProcessCommandRunner(Options.Create(new McpOptions()));

        var result = await runner.RunAsync(
            new CommandRequest(
                "bash",
                "-lc",
                "echo raw-stdout; echo raw-stderr >&2; sleep 5")
            {
                Timeout = TimeSpan.FromMilliseconds(200),
                ThrowOnNonZeroExitCode = false,
            });

        Assert.NotEqual(0, result.ExitCode);
        Assert.Contains("raw-stdout", result.StandardOutput);
        Assert.Contains("raw-stderr", result.StandardError);
        Assert.Contains("timed out", result.StandardError);
    }
}
