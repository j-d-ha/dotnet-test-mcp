using Xunit;

namespace DotnetTest.Mcp.Tests;

public class EchoToolTests
{
    [Fact]
    public void Echo_ReturnsGreeting()
    {
        var tool = new EchoTool();

        var result = tool.Echo("world");

        Assert.Equal("hello world", result);
    }
}