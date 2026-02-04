using System.Text.Json;
using System.Text.Json.Serialization;
using DotnetTest.Mcp;
using DotnetTest.Mcp.Terminal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = Host.CreateApplicationBuilder(args);

builder.Logging.AddConsole(consoleLogOptions =>
{
    consoleLogOptions.LogToStandardErrorThreshold = LogLevel.Trace;
});

builder.Services.Configure<McpOptions>(builder.Configuration);
builder.Services.AddScoped<ICommandRunner, ProcessCommandRunner>();

var jsonOptions = new JsonSerializerOptions { TypeInfoResolver = JsonContext.Default };

builder.Services
    .AddMcpServer(options => options.ScopeRequests = true)
    .WithStdioServerTransport()
    .WithTools<GetWorkingDirectoryTool>(jsonOptions)
    .WithTools<ListTestProjectsTool>(jsonOptions);

await builder.Build().RunAsync();

[JsonSerializable(
    typeof(GetWorkingDirectoryTool.Result),
    TypeInfoPropertyName = "GetWorkingDirectoryToolResult")]
[JsonSerializable(
    typeof(ListTestProjectsTool.Result),
    TypeInfoPropertyName = "ListTestProjectsToolResult")]
[JsonSerializable(
    typeof(RunSingleTestTool.Result),
    TypeInfoPropertyName = "RunSingleTestToolResult")]
public partial class JsonContext : JsonSerializerContext;
