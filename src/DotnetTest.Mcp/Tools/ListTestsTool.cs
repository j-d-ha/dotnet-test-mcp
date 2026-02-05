using System.ComponentModel;
using DotnetTest.Mcp.Terminal;
using ModelContextProtocol.Server;

namespace DotnetTest.Mcp.Tools;

[McpServerToolType]
public sealed class ListTestsTool(ICommandRunner commandRunner)
{
    private readonly ICommandRunner _commandRunner = commandRunner.ValidateNotNull();

    [McpServerTool(UseStructuredContent = true)]
    [Description("Lists all discovered tests for the solution or a project.")]
    public async Task<Result> ListTests(
        [Description("Optional path to project file to scope listing.")] string? projectPath = null,
        CancellationToken cancellationToken = default)
    {
        var trimmedProjectPath = string.IsNullOrWhiteSpace(projectPath) ? null : projectPath.Trim();

        var arguments = new List<string> { "test" };

        string scope;
        if (trimmedProjectPath is null)
        {
            scope = "solution";
        }
        else
        {
            arguments.Add("--project");
            arguments.Add(trimmedProjectPath);
            scope = "project";
        }

        arguments.Add("--list-tests");
        arguments.Add("--no-ansi");
        arguments.Add("--no-progress");

        var commandResult = await _commandRunner.RunAsync(
            new CommandRequest("dotnet", arguments.ToArray()) { ThrowOnNonZeroExitCode = false },
            cancellationToken);

        var tests = TestListParser.ExtractTests(commandResult.StandardOutputLines);
        return new Result(scope, trimmedProjectPath, tests.Length, tests);
    }

    [Description("Result containing discovered test names.")]
    public record Result(
        [property: Description("Scope used to list tests: solution or project.")] string Scope,
        [property: Description("Project path used when Scope is project; otherwise null.")]
        string? ProjectPath,
        [property: Description("Number of discovered tests.")] int TestCount,
        [property: Description("Fully qualified test names returned by the test adapter.")]
        string[] Tests);
}
