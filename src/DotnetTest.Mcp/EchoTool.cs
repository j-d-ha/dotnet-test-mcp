using System.ComponentModel;
using ModelContextProtocol.Server;

namespace DotnetTest.Mcp;

[McpServerToolType]
public sealed class EchoTool
{
    [McpServerTool]
    [Description("Echoes the message back to the client.")]
    public string Echo(string message) => $"hello {message}";
}