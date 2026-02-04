using System.ComponentModel;
using DotnetTest.Mcp.Terminal;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Server;

namespace DotnetTest.Mcp;

[McpServerToolType]
public sealed class ListTestProjectsTool(IOptions<McpOptions> options, ICommandRunner commandRunner)
{
    private readonly McpOptions _options = options.Value;

    [McpServerTool(UseStructuredContent = true)]
    [Description("Lists test projects in the solution.")]
    public async Task<Result> ListTestProjects(CancellationToken cancellationToken)
    {
        var result = await commandRunner.RunAsync(
            new CommandRequest("dotnet", "sln", "list")
            {
                WorkingDirectory = _options.WorkingDirectory, ThrowOnNonZeroExitCode = true,
            },
            cancellationToken);

        if (result.StandardOutputLines.FirstOrDefault() is not "Project(s)")
            throw new InvalidOperationException(
                $"dotnet sln list failed: Unexpected output: {result.StandardOutput.Trim()}");

        return new Result(
            result.StandardOutputLines
                .Skip(2)
                .Where(x => x.StartsWith(_options.TestsDirectoryName, StringComparison.Ordinal))
                .ToArray());
    }

    [Description("Result containing discovered test project paths.")]
    public record Result(
        [property: Description("Test project paths (relative to the solution root).")]
        string[] TestProjects);
}
