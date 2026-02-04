using System.ComponentModel;
using ModelContextProtocol.Server;

namespace DotnetTest.Mcp;

[McpServerToolType]
public sealed class RunSingleTestTool
{
    [McpServerTool(UseStructuredContent = true)]
    [Description("Runs a single test.")]
    public async Task<Result> RunSingleTest(
        [Description(
            """
            Qualified name of the test to run, including namespace, class, and method.
            Example: MyNamespace.MyClass.MyMethod
            """)]
        string qualifiedTestName,
        CancellationToken cancellationToken)
    {
        // Generate timestamp and filename
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var trxFileName = $"TestResults_{timestamp}.trx";

        // Get temp path and combine with filename
        var tempPath = Path.GetTempPath();
        var fullTrxPath = Path.Combine(tempPath, trxFileName);

        throw new NotImplementedException();
    }

    [Description("Runs a single test.")]
    public record Result;
}