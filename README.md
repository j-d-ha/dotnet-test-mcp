# dotnet-test-mcp

An MCP server that lets AI assistants discover and run .NET tests via `dotnet test`.

## Tools

| Tool                    | Description                                            |
|-------------------------|--------------------------------------------------------|
| `ListTestProjects`      | List all test projects in the solution                 |
| `ListTestsSummary`      | Get test counts and names, with optional prefix filter |
| `RunSingleTest`         | Run a test by fully qualified method name              |
| `RunAllTests`           | Run all tests in the solution                          |
| `RunAllTestsForProject` | Run all tests in a specific project                    |
| `RunAllTestsInClass`    | Run all tests in a class                               |

## Requirements

- .NET 10.0 SDK
- xUnit test projects (required for CTRF report generation)

## Setup

Add to your MCP client config (e.g. `claude_desktop_config.json`):

```json
{
  "mcpServers": {
    "dotnet-test-mcp": {
      "command": "dotnet",
      "args": ["run", "--project", "/path/to/dotnet-test-mcp/src/DotnetTest.Mcp", "-c", "Release"],
      "env": {
        "WORKING_DIRECTORY": "/path/to/your/dotnet/solution",
        "TESTS_DIRECTORY_NAME": "tests"
      }
    }
  }
}
```

**Config file locations:**

- macOS: `~/Library/Application Support/Claude/claude_desktop_config.json`
- Windows: `%APPDATA%\Claude\claude_desktop_config.json`
- Linux: `~/.config/Claude/claude_desktop_config.json`

## Environment Variables

| Variable               | Description                     | Default           |
|------------------------|---------------------------------|-------------------|
| `WORKING_DIRECTORY`    | Path to your solution root      | Current directory |
| `TESTS_DIRECTORY_NAME` | Folder containing test projects | `tests`           |

## Building

```bash
git clone https://github.com/jonasha/dotnet-test-mcp.git
cd dotnet-test-mcp
dotnet build -c Release
```

## Troubleshooting

- **No test projects found**: Check `WORKING_DIRECTORY` and `TESTS_DIRECTORY_NAME` match your
  structure
- **No tests matched**: Use `ListTestsSummary` first to find the correct fully qualified name
- **CTRF errors**: Confirm you're using xUnit 3.0+ with CTRF support

## License

MIT — see [LICENSE](LICENSE)
