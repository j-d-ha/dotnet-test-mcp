# dotnet-test-mcp

**An MCP server for running .NET tests**

[![NuGet Version](https://img.shields.io/nuget/vpre/DotnetTest.Mcp)](https://www.nuget.org/packages/DotnetTest.Mcp/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/DotnetTest.Mcp)](https://www.nuget.org/packages/DotnetTest.Mcp/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4)](https://dotnet.microsoft.com/download)

An MCP server that lets AI assistants discover and run .NET tests via `dotnet test`. Designed
exclusively for projects
using [Microsoft Testing Platform (MTP) v2](https://learn.microsoft.com/en-us/dotnet/core/testing/microsoft-testing-platform-overview) —
VSTest-based projects are not supported.

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
- Test projects
  using [Microsoft Testing Platform (MTP) v2](https://learn.microsoft.com/en-us/dotnet/core/testing/microsoft-testing-platform-overview) —
  VSTest-based projects are not supported. MTP v2 is supported by all major test frameworks (xUnit,
  NUnit, MSTest, TUnit).
- xUnit (required for CTRF report generation)

> **Note:** MTP v2 requires opting in via `global.json` at your repository or solution root:
> ```json
> {
>   "test": {
>     "runner": "Microsoft.Testing.Platform"
>   }
> }
> ```

## Installation

Install as a global .NET tool from NuGet:

```bash
dotnet tool install --global DotnetTest.Mcp
```

Or as a local tool in your project/solution:

```bash
dotnet tool install --local DotnetTest.Mcp
```

## Setup

Add to your MCP client config (e.g. `claude_desktop_config.json`):

**Using the installed NuGet tool (recommended):**

```json
{
  "mcpServers": {
    "dotnet-test-mcp": {
      "command": "dotnet-test-mcp"
    }
  }
}
```

**Using `dotnet run` from source:**

```json
{
  "mcpServers": {
    "dotnet-test-mcp": {
      "command": "dotnet",
      "args": ["run", "--project", "/path/to/dotnet-test-mcp/src/DotnetTest.Mcp", "-c", "Release"]
    }
  }
}
```

**Config file locations:**

- macOS: `~/Library/Application Support/Claude/claude_desktop_config.json`
- Windows: `%APPDATA%\Claude\claude_desktop_config.json`
- Linux: `~/.config/Claude/claude_desktop_config.json`

## Environment Variables

These are only needed in special cases. By default, the server uses the directory it is launched
from as the working directory, which most AI tools set automatically to the root of your open
project.

| Variable               | Description                                                    | Default           |
|------------------------|----------------------------------------------------------------|-------------------|
| `WORKING_DIRECTORY`    | Override the solution root (e.g. if the server runs elsewhere) | Current directory |
| `TESTS_DIRECTORY_NAME` | Override the folder name containing test projects              | `tests`           |
| `DISABLE_CTRF`         | Disable `--report-ctrf` arguments for hosts without CTRF support (e.g., some TUnit setups) | `false` |

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
