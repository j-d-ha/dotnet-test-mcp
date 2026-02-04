using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Server;

namespace DotnetTest.Mcp;

[McpServerToolType]
public sealed class ListTestProjectsTool(IOptions<McpOptions> options)
{
    private readonly McpOptions _options = options.Value;

    [McpServerTool(UseStructuredContent = true)]
    [Description("Lists test projects in the solution.")]
    public async Task<Result> ListTestProjects()
    {
        var baseDirectory = "_options.Value.TestBaseDirectory";
        var startInfo = new ProcessStartInfo("dotnet", $"sln \"{baseDirectory}\" list")
        {
            WorkingDirectory = baseDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        using var process = new Process { StartInfo = startInfo };
        var stdout = new List<string>();
        var stderr = new StringBuilder();

        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data is not null)
                stdout.Add(e.Data);
        };

        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data is not null)
                stderr.AppendLine(e.Data);
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        await process.WaitForExitAsync();
        if (process.ExitCode != 0)
            throw new InvalidOperationException(
                $"dotnet sln list failed ({process.ExitCode}): {stderr.ToString().Trim()}");

        if (stdout.FirstOrDefault() is not "Project(s)")
        {
            var output = string.Join(Environment.NewLine, stdout).Trim();
            throw new InvalidOperationException(
                $"dotnet sln list failed: Unexpected output: {output}");
        }

        return new Result(stdout.Skip(2).ToArray());
    }

    public record Result(string[] TestProjects);
}