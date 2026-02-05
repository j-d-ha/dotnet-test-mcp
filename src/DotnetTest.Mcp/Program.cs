using System.Text.Json;
using System.Text.Json.Serialization;
using DotnetTest.Mcp;
using DotnetTest.Mcp.Models;
using DotnetTest.Mcp.Terminal;
using DotnetTest.Mcp.Tools;
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

builder.Services.AddSingleton(jsonOptions);

builder.Services
    .AddMcpServer(options => options.ScopeRequests = true)
    .WithStdioServerTransport()
    .WithTools<GetWorkingDirectoryTool>(jsonOptions)
    .WithTools<ListTestProjectsTool>(jsonOptions)
    .WithTools<RunSingleTestTool>(jsonOptions);

await builder.Build().RunAsync();

[JsonSourceGenerationOptions(UseStringEnumConverter = true)]
[JsonSerializable(
    typeof(GetWorkingDirectoryTool.Result),
    TypeInfoPropertyName = "GetWorkingDirectoryToolResult")]
[JsonSerializable(
    typeof(ListTestProjectsTool.Result),
    TypeInfoPropertyName = "ListTestProjectsToolResult")]
[JsonSerializable(
    typeof(RunSingleTestTool.Result),
    TypeInfoPropertyName = "RunSingleTestToolResult")]
[JsonSerializable(typeof(CtrfReport), TypeInfoPropertyName = "CtrfReport")]
public partial class JsonContext : JsonSerializerContext;