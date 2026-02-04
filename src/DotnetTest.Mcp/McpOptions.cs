namespace DotnetTest.Mcp;

public sealed class McpOptions
{
    /// <summary>Base directory used by tools that run dotnet commands. Environment variable: MCP_WORKING_DIRECTORY.</summary>
    public string WorkingDirectory { get; set; } = Directory.GetCurrentDirectory();

    /// <summary>Relative folder name under <see cref="WorkingDirectory" /> that contains tests. Environment variable: MCP_TESTS_DIRECTORY_NAME.</summary>
    public string TestsDirectoryName { get; set; } = "tests";

    /// <summary>Full path to the tests directory, derived from <see cref="WorkingDirectory" /> and <see cref="TestsDirectoryName" />.</summary>
    public string TestsDirectoryPath => Path.Combine(WorkingDirectory, TestsDirectoryName);
}