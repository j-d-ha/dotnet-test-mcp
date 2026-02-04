using System.ComponentModel;
using DotnetTest.Mcp.Trx;
using ModelContextProtocol.Server;
using StackXML;

namespace DotnetTest.Mcp;

[McpServerToolType]
public sealed class TestTool
{
    private const string _workingDirectory = "/Users/jonasha/Repos/CSharp/dynamodb-efcore-provider";

    [McpServerTool]
    [Description("Gets the working directory of the server process.")]
    public string GetWorkingDirectory() => Directory.GetCurrentDirectory();

    [McpServerTool]
    [Description("Lists test projects in the solution.")]
    public string ListTestProjects()
    {
        const string Path = "";

        var xml = File.ReadAllText(Path);
        var testRun = XmlReadBuffer.ReadStatic<TrxTestRun>(xml.AsSpan());

        var testProjects = "test project list";
        return $"hello {testProjects}";
    }
}