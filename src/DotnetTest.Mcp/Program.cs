using System.Text.Json;
using System.Text.Json.Serialization;
using DotnetTest.Mcp;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<McpOptions>(builder.Configuration);

var jsonOptions = new JsonSerializerOptions { TypeInfoResolver = JsonContext.Default };

builder
    .Services.AddMcpServer()
    .WithStdioServerTransport()
    .WithTools<GetWorkingDirectoryTool>(jsonOptions)
    .WithTools<ListTestProjectsTool>(jsonOptions);

await builder.Build().RunAsync();

[JsonSerializable(
    typeof(GetWorkingDirectoryTool.Result),
    TypeInfoPropertyName = "GetWorkingDirectoryTool.Result")]
[JsonSerializable(
    typeof(ListTestProjectsTool.Result),
    TypeInfoPropertyName = "ListTestProjectsTool.Result")]
public partial class JsonContext : JsonSerializerContext;