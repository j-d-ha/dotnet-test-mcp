using DotnetTest.Mcp.Terminal;

namespace DotnetTest.Mcp.Tools;

internal static class TestProjectDiscovery
{
    internal static async Task<string[]> ListAsync(
        ICommandRunner commandRunner,
        McpOptions options,
        string? workingDirectory,
        CancellationToken cancellationToken)
    {
        var result = await commandRunner.RunAsync(
            new CommandRequest("dotnet", "sln", "list")
            {
                WorkingDirectory = workingDirectory,
            },
            cancellationToken);

        if (result.StandardOutputLines.FirstOrDefault() is not "Project(s)")
            throw new InvalidOperationException(
                $"dotnet sln list failed: Unexpected output: {result.StandardOutput.Trim()}");

        return result.StandardOutputLines
            .Skip(2)
            .Where(x => x.StartsWith(options.TestsDirectoryName, StringComparison.Ordinal))
            .ToArray();
    }
}