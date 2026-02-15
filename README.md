# dotnet-test-mcp

A Model Context Protocol (MCP) server for .NET test discovery and execution. This server enables AI assistants and MCP clients to programmatically discover and run .NET tests through a standardized interface.

## Overview

dotnet-test-mcp exposes .NET test capabilities via the Model Context Protocol, allowing AI assistants like Claude to:

- Discover test projects and tests in a solution
- Run tests at various scopes (single test, class, project, or entire solution)
- Retrieve structured test results with detailed failure information
- Filter and search tests by fully qualified names

**Key Features:**

- **Test Discovery**: List test projects, discover tests with optional filtering, and get test summaries
- **Flexible Execution**: Run single tests, all tests in a class, all tests in a project, or solution-wide
- **Structured Results**: Uses CTRF (Common Test Result Format) for consistent, machine-readable test output
- **AOT Compiled**: Fast startup with ahead-of-time compilation
- **Stdio Transport**: Simple integration via standard input/output

## Table of Contents

- [Features](#features)
- [Installation](#installation)
- [Configuration](#configuration)
- [Usage](#usage)
- [MCP Tools Reference](#mcp-tools-reference)
- [Architecture](#architecture)
- [Development](#development)
- [Project Structure](#project-structure)
- [Contributing](#contributing)
- [Troubleshooting](#troubleshooting)
- [FAQ](#faq)
- [License](#license)
- [Acknowledgments](#acknowledgments)

## Features

### Test Discovery

- **List Test Projects**: Discover all test projects in a solution
- **Test Enumeration**: List tests with optional prefix filtering
- **Summary Views**: Get test counts with configurable detail levels (up to 200 test names)
- **Smart Handling**: Gracefully handle empty test projects and missing tests

### Test Execution

- **Single Test Execution**: Run a specific test by fully qualified method name
- **Class-Level Execution**: Run all tests in a given test class
- **Project-Level Execution**: Run all tests in a specific project
- **Solution-Wide Execution**: Run the entire test suite
- **Stack Trace Control**: Optionally include stack traces in failure details

### Results & Reporting

- **CTRF Format**: Uses Common Test Result Format for standardized JSON output
- **Structured Outcomes**: Clear test outcomes (Passed, Failed, Skipped, NotFound, Ambiguous, Error, Partial)
- **Duration Tracking**: Measures test execution time in milliseconds
- **Failure Diagnostics**: Detailed failure messages with optional stack traces
- **Smart Limits**: Returns up to 20 failing test names and detailed info for up to 3 failures

### Performance

- **AOT Compilation**: Compiled ahead-of-time for minimal startup latency
- **Efficient Communication**: Uses stdio-based transport for fast inter-process communication
- **Scoped Requests**: Isolated request handling for reliability

## Installation

### Prerequisites

- .NET 10.0 SDK or later
- Compatible MCP client (Claude Desktop, MCP Inspector, etc.)
- xUnit-based test projects (for CTRF support)

### From Source

```bash
git clone https://github.com/jonasha/dotnet-test-mcp.git
cd dotnet-test-mcp
dotnet build -c Release
```

### Future: NuGet Package

```bash
# When published to NuGet
dotnet tool install -g DotnetTest.Mcp
```

## Configuration

### MCP Client Setup

Add the following configuration to your MCP client's settings:

```json
{
  "mcpServers": {
    "dotnet-test-mcp": {
      "command": "dotnet",
      "args": [
        "run",
        "--project",
        "/path/to/dotnet-test-mcp/src/DotnetTest.Mcp",
        "-c",
        "Release"
      ],
      "env": {
        "WORKING_DIRECTORY": "/path/to/your/dotnet/solution",
        "TESTS_DIRECTORY_NAME": "tests"
      }
    }
  }
}
```

### Environment Variables

| Variable | Description | Default | Example |
|----------|-------------|---------|---------|
| `WORKING_DIRECTORY` | Base directory for dotnet commands | Current directory | `/Users/you/MyProject` |
| `TESTS_DIRECTORY_NAME` | Folder name containing test projects | `tests` | `tests` or `test` |

### Claude Desktop Configuration

Add the server to your Claude Desktop configuration file:

**macOS**: `~/Library/Application Support/Claude/claude_desktop_config.json`

**Windows**: `%APPDATA%\Claude\claude_desktop_config.json`

**Linux**: `~/.config/Claude/claude_desktop_config.json`

Example configuration:

```json
{
  "mcpServers": {
    "dotnet-test-mcp": {
      "command": "dotnet",
      "args": [
        "run",
        "--project",
        "/Users/you/repos/dotnet-test-mcp/src/DotnetTest.Mcp",
        "-c",
        "Release"
      ],
      "env": {
        "WORKING_DIRECTORY": "/Users/you/repos/MyDotnetApp",
        "TESTS_DIRECTORY_NAME": "tests"
      }
    }
  }
}
```

## Usage

### Quick Start

1. Configure the MCP server in your client (see [Configuration](#configuration))
2. Start your MCP client (e.g., Claude Desktop)
3. Use the available tools to discover and run tests

### Example Workflows

#### Discovering Tests

```
User: "What test projects are in this solution?"
→ Assistant uses: ListTestProjects()

User: "Show me all tests in the MyApp.Auth namespace"
→ Assistant uses: ListTestsSummary(prefix: "MyApp.Auth", includeTests: true)
```

#### Running Tests

```
User: "Run the LoginController tests"
→ Assistant uses: RunAllTestsInClass(className: "MyApp.Tests.LoginControllerTests")

User: "Run the test MyApp.Tests.LoginTests.ValidCredentials_ShouldSucceed"
→ Assistant uses: RunSingleTest(
    qualifiedMethodName: "MyApp.Tests.LoginTests.ValidCredentials_ShouldSucceed"
  )
```

#### Debugging Failures

```
User: "Run all tests and show me full stack traces for failures"
→ Assistant uses: RunAllTests(includeStackTrace: true)

User: "Run tests in the DataAccess project"
→ Assistant uses: RunAllTestsForProject(
    projectPath: "tests/MyApp.DataAccess.Tests/MyApp.DataAccess.Tests.csproj"
  )
```

## MCP Tools Reference

### ListTestProjects

**Description**: Lists all test projects in the solution.

**Parameters**: None

**Returns**:
- `TestProjects` (string[]): Array of project paths relative to solution root

**Example Response**:
```json
{
  "TestProjects": [
    "tests/MyApp.Tests/MyApp.Tests.csproj",
    "tests/MyApp.Integration.Tests/MyApp.Integration.Tests.csproj"
  ]
}
```

---

### ListTestsSummary

**Description**: Summarizes discovered tests with counts and an optional capped list.

**Parameters**:
- `prefix` (optional string): Filter by fully qualified test name prefix
- `includeTests` (optional bool): Include up to 200 test names; otherwise return counts only (default: `false`)
- `requireTests` (optional bool): Treat zero discovered tests as an error (default: `false`)

**Returns**:
- `Scope` (string): Always `"solution"`
- `Outcome` (TestOutcome): Discovery outcome
- `Message` (string): Short summary message
- `ExitCode` (int): Process exit code
- `TestCount` (int): Total discovered test count after filtering
- `ReturnedCount` (int): Number of tests returned in this response
- `HasMore` (bool): True when more tests exist beyond the 200-test cap
- `Tests` (string[]): Optional capped list of fully qualified test names
- `Warning` (optional string): Warning message when Outcome is Passed
- `ProjectResults` (ProjectDiscoveryResult[]): Per-project discovery results
- `Error` (optional ErrorInfo): Error details when Outcome is Error

**Example Response**:
```json
{
  "Scope": "solution",
  "Outcome": "Passed",
  "Message": "Discovered 142 tests.",
  "ExitCode": 0,
  "TestCount": 142,
  "ReturnedCount": 142,
  "HasMore": false,
  "Tests": [
    "MyApp.Tests.UserService.CreateUser_ValidInput_ReturnsUser",
    "MyApp.Tests.UserService.CreateUser_InvalidEmail_ThrowsException",
    "..."
  ],
  "Warning": null,
  "ProjectResults": [...],
  "Error": null
}
```

---

### RunSingleTest

**Description**: Runs a single dotnet test by fully qualified method name.

**Parameters**:
- `qualifiedMethodName` (required string): Fully qualified test method name (e.g., `MyNamespace.MyClass.MyMethod`)
- `includeStackTrace` (optional bool): Include stack trace in failure details (default: `false`)

**Returns**:
- `RequestedTestName` (string): The test name that was requested
- `Outcome` (TestOutcome): Test outcome (Passed, Failed, Skipped, NotFound, Ambiguous, or Error)
- `Message` (string): Short summary message
- `DurationMilliseconds` (optional int): Test execution time
- `FailureMessage` (optional string): Error message when Outcome is Failed
- `FailureStackTrace` (optional string): Stack trace when Outcome is Failed and requested
- `Error` (optional ErrorInfo): Structured error details when Outcome is Error

**Example Response** (Passed):
```json
{
  "RequestedTestName": "MyApp.Tests.LoginTests.ValidCredentials_ShouldSucceed",
  "Outcome": "Passed",
  "Message": "Test passed.",
  "DurationMilliseconds": 142,
  "FailureMessage": null,
  "FailureStackTrace": null,
  "Error": null
}
```

**Example Response** (Failed):
```json
{
  "RequestedTestName": "MyApp.Tests.LoginTests.InvalidPassword_ShouldFail",
  "Outcome": "Failed",
  "Message": "Test failed.",
  "DurationMilliseconds": 89,
  "FailureMessage": "Expected exception was not thrown",
  "FailureStackTrace": "   at MyApp.Tests.LoginTests.InvalidPassword_ShouldFail() in ...",
  "Error": null
}
```

---

### RunAllTests

**Description**: Runs all tests in the solution.

**Parameters**:
- `includeStackTrace` (optional bool): Include stack traces in failure details (default: `false`)

**Returns**:
- `Scope` (string): Always `"solution"`
- `Outcome` (TestOutcome): Test run outcome
- `Message` (string): Short summary message
- `ExitCode` (int): Process exit code from dotnet test
- `TestCount` (int): Total number of tests
- `Passed` (int): Count of passed tests
- `Failed` (int): Count of failed tests
- `Skipped` (int): Count of skipped tests
- `Pending` (int): Count of pending tests
- `Other` (int): Count of other-status tests
- `DurationMilliseconds` (optional int): Total duration
- `FailingTests` (string[]): Up to the first 20 failing test names
- `FailureDetails` (TestFailure[]): Structured details for up to 3 failed tests
- `HasMoreFailures` (bool): True when more failures exist
- `Error` (optional ErrorInfo): Error details when Outcome is Error

**Example Response**:
```json
{
  "Scope": "solution",
  "Outcome": "Failed",
  "Message": "Test run completed with failures.",
  "ExitCode": 1,
  "TestCount": 142,
  "Passed": 138,
  "Failed": 4,
  "Skipped": 0,
  "Pending": 0,
  "Other": 0,
  "DurationMilliseconds": 5234,
  "FailingTests": [
    "MyApp.Tests.UserService.CreateUser_DuplicateEmail_ThrowsException",
    "MyApp.Tests.OrderService.ProcessOrder_InsufficientStock_Fails",
    "MyApp.Tests.PaymentService.ChargeCard_InvalidCard_ThrowsException",
    "MyApp.Tests.EmailService.SendEmail_InvalidRecipient_Fails"
  ],
  "FailureDetails": [
    {
      "TestName": "MyApp.Tests.UserService.CreateUser_DuplicateEmail_ThrowsException",
      "Message": "Expected exception ArgumentException was not thrown",
      "StackTrace": "..."
    }
  ],
  "HasMoreFailures": true,
  "Error": null
}
```

---

### RunAllTestsForProject

**Description**: Runs all tests for a single project.

**Parameters**:
- `projectPath` (required string): Path to the project file (.csproj) to test
- `includeStackTrace` (optional bool): Include stack traces in failure details (default: `false`)

**Returns**: Same structure as RunAllTests, with these differences:
- `Scope` (string): Always `"project"`
- `ProjectPath` (string): The project path used for the test run

**Example Response**:
```json
{
  "Scope": "project",
  "ProjectPath": "tests/MyApp.Tests/MyApp.Tests.csproj",
  "Outcome": "Passed",
  "Message": "All tests passed.",
  "ExitCode": 0,
  "TestCount": 87,
  "Passed": 87,
  "Failed": 0,
  "Skipped": 0,
  "Pending": 0,
  "Other": 0,
  "DurationMilliseconds": 3421,
  "FailingTests": [],
  "FailureDetails": [],
  "HasMoreFailures": false,
  "Error": null
}
```

---

### RunAllTestsInClass

**Description**: Runs all tests in a given test class.

**Parameters**:
- `className` (required string): Fully qualified test class name
- `includeStackTrace` (optional bool): Include stack traces in failure details (default: `false`)

**Returns**: Same structure as RunAllTests, with these differences:
- `Scope` (string): Always `"class"`
- `ClassName` (string): The class name used to filter tests

**Example Response**:
```json
{
  "Scope": "class",
  "ClassName": "MyApp.Tests.UserServiceTests",
  "Outcome": "Passed",
  "Message": "All tests passed.",
  "ExitCode": 0,
  "TestCount": 12,
  "Passed": 12,
  "Failed": 0,
  "Skipped": 0,
  "Pending": 0,
  "Other": 0,
  "DurationMilliseconds": 567,
  "FailingTests": [],
  "FailureDetails": [],
  "HasMoreFailures": false,
  "Error": null
}
```

## Architecture

### High-Level Overview

```
┌─────────────────┐
│   MCP Client    │  (Claude Desktop, MCP Inspector, etc.)
│  (AI Assistant) │
└────────┬────────┘
         │ stdio (stdin/stdout)
         │
┌────────▼──────────────────────────┐
│  dotnet-test-mcp MCP Server       │
│  ┌─────────────────────────────┐  │
│  │ 6 MCP Tools                 │  │
│  │ - ListTestProjects          │  │
│  │ - ListTestsSummary          │  │
│  │ - RunSingleTest             │  │
│  │ - RunAllTests               │  │
│  │ - RunAllTestsForProject     │  │
│  │ - RunAllTestsInClass        │  │
│  └──────────┬──────────────────┘  │
│             │                      │
│  ┌──────────▼──────────────────┐  │
│  │ ProcessCommandRunner        │  │
│  │ (executes dotnet test)      │  │
│  └──────────┬──────────────────┘  │
│             │                      │
│  ┌──────────▼──────────────────┐  │
│  │ CTRF Parser                 │  │
│  │ (parses JSON test reports)  │  │
│  └─────────────────────────────┘  │
└───────────────────────────────────┘
         │
         ▼
  ┌──────────────┐
  │ dotnet test  │
  │ (executes    │
  │  .NET tests) │
  └──────────────┘
```

### Components

#### 1. MCP Server Layer
- Stdio transport for bidirectional communication with MCP clients
- Request scoping for isolation between concurrent requests
- Tool discovery and registration via ModelContextProtocol SDK

#### 2. Tool Implementations (`/Tools`)
- 6 MCP tools for test discovery and execution
- Parameter validation and type safety
- Result formatting and error handling

#### 3. Command Execution (`ProcessCommandRunner`)
- Executes `dotnet test` commands with appropriate arguments
- Captures stdout/stderr for parsing
- Manages process lifecycle with timeout support

#### 4. Test Parsing
- **TestProjectDiscovery**: Finds test projects using `dotnet sln list`
- **TestListParser**: Extracts test names from `dotnet test --list-tests` output
- **CtrfTestRun**: Executes tests with CTRF reporting and parses JSON results

#### 5. Models (`/Models`)
- **CtrfReport**: Complete CTRF JSON structure for test results
- **TestOutcome**: Enumeration of test states (Passed, Failed, Skipped, etc.)
- **ErrorInfo**: Structured error details with error kind classification
- **TestFailure**: Failure detail objects with messages and stack traces

### Key Design Decisions

- **AOT Compilation**: Fast startup and smaller deployment footprint
- **JSON Source Generation**: AOT-compatible serialization without reflection
- **CTRF Format**: Standard test result format for interoperability with other tools
- **Error Classification**: Smart exit code interpretation with structured error types
- **Scoped Requests**: Isolation between concurrent requests for reliability

## Development

### Building the Project

```bash
# Restore dependencies
dotnet restore DotnetTest.Mcp.slnx

# Build (Debug)
dotnet build DotnetTest.Mcp.slnx

# Build (Release with AOT)
dotnet build DotnetTest.Mcp.slnx -c Release
```

### Running the Server

```bash
# Development run
dotnet run --project src/DotnetTest.Mcp

# With environment variables
WORKING_DIRECTORY=/path/to/solution dotnet run --project src/DotnetTest.Mcp
```

### Testing

```bash
# Run all tests
dotnet test DotnetTest.Mcp.slnx -c Release

# Run specific test
dotnet test tests/DotnetTest.Mcp.Tests/DotnetTest.Mcp.Tests.csproj \
  --filter "FullyQualifiedName=DotnetTest.Mcp.Tests.ClassName.MethodName"

# Verbose output
dotnet test DotnetTest.Mcp.slnx -c Release -v normal
```

### Using MCP Inspector

The MCP Inspector provides an interactive web interface for testing the server:

```bash
# Using task runner (recommended)
task inspect

# Or manually
npx -y @modelcontextprotocol/inspector \
  dotnet run --project src/DotnetTest.Mcp -c Release
```

### Code Style

This project follows the conventions documented in `AGENTS.md`:

- **Formatting**: K&R bracing style, 4-space indentation
- **Namespaces**: File-scoped namespaces
- **Nullability**: Nullable reference types enabled
- **Strictness**: `TreatWarningsAsErrors=true` for all projects
- **Naming**: PascalCase for public members, camelCase for parameters/locals
- **Testing**: Test names follow `Method_Scenario_Expected` pattern

Use `dotnet format` for consistent code formatting.

## Project Structure

```
dotnet-test-mcp/
├── src/
│   └── DotnetTest.Mcp/              # Main MCP server
│       ├── Program.cs               # Entry point, DI configuration
│       ├── McpOptions.cs            # Configuration model
│       ├── JsonContext.cs           # JSON serialization context
│       ├── Tools/                   # MCP tool implementations
│       │   ├── ListTestProjectsTool.cs
│       │   ├── ListTestsSummaryTool.cs
│       │   ├── RunSingleTestTool.cs
│       │   ├── RunAllTestsTool.cs
│       │   ├── RunAllTestsForProjectTool.cs
│       │   ├── RunAllTestsInClassTool.cs
│       │   ├── CtrfTestRun.cs      # CTRF execution logic
│       │   ├── TestProjectDiscovery.cs
│       │   ├── TestListParser.cs
│       │   ├── FailureFormatting.cs
│       │   └── TextTruncation.cs
│       ├── Models/                  # Data models
│       │   ├── CtrfReport.cs       # CTRF format models
│       │   ├── TestOutcome.cs      # Outcome enumeration
│       │   ├── ErrorInfo.cs        # Error details
│       │   ├── ErrorKind.cs        # Error classification
│       │   ├── TestFailure.cs      # Failure details
│       │   ├── TruncatedText.cs    # Text truncation
│       │   └── ProjectDiscoveryResult.cs
│       ├── Terminal/                # Command execution
│       │   └── ProcessCommandRunner.cs
│       └── Extensions/
│           └── NotNullExtensions.cs
├── tests/
│   └── DotnetTest.Mcp.Tests/        # xUnit test project
├── .mcp/
│   └── server.json                  # MCP server manifest
├── docs/
│   └── DotnetTestCommands.md        # dotnet test reference
├── Taskfile.yml                     # Task automation
├── AGENTS.md                        # Development guidelines
├── LICENSE                          # MIT License
└── README.md                        # This file
```

## Contributing

Contributions are welcome! Please follow these guidelines:

1. **Read** `AGENTS.md` for code conventions and development patterns
2. **Keep builds green**: Both `dotnet build` and `dotnet test` must pass
3. **Minimize diff size**: Only change what's required for your feature/fix
4. **Follow existing patterns**: Use dependency injection, records, and async/await
5. **Add tests**: New functionality should include unit tests
6. **Update documentation**: Keep the README and code comments current

### Pull Request Process

1. Fork the repository
2. Create a feature branch from `main`
3. Make your changes following the code style guidelines
4. Ensure all tests pass locally
5. Update documentation as needed
6. Submit a PR with a clear description of your changes

## Troubleshooting

### "No test projects discovered"

**Cause**: The server cannot find test projects in the configured location.

**Solutions**:
- Verify `WORKING_DIRECTORY` points to your solution root directory
- Check that `TESTS_DIRECTORY_NAME` matches your project structure (e.g., "tests", "test", "Tests")
- Ensure test projects have `.csproj` files and are listed in the solution

### "No tests matched the filter"

**Cause**: The specified test name doesn't match any discovered tests.

**Solutions**:
- Use `ListTestsSummary` first to see available test names
- Verify the fully qualified name is correct (namespace + class + method)
- Check for typos in namespace, class, or method names
- Remember that test names are case-sensitive

### "CTRF report not found" or "ReadFailed" errors

**Cause**: The test runner couldn't generate or find the CTRF report.

**Solutions**:
- Ensure you're using xUnit (currently required for CTRF support)
- Verify xUnit version supports `--report-ctrf` flag (xUnit 3.0+)
- Check that test projects have the required xUnit NuGet packages

### Exit code 8

**Cause**: This is the standard `dotnet test` exit code when no tests match the filter.

**Note**: This is normal behavior, not an error. Use `requireTests: false` in `ListTestsSummary` if you want to avoid errors when no tests are found.

## FAQ

### Which test frameworks are supported?

Currently, xUnit is required because the server relies on CTRF report generation. Support for NUnit and MSTest may be added in future versions if they add CTRF support or alternative reporting mechanisms can be implemented.

### Can I use this with .NET 8 or 9?

The server itself targets .NET 10.0, but it can discover and run tests from projects targeting any .NET version (including .NET Framework, .NET Core, .NET 5+).

### How do I use this with Claude Desktop?

Add the server configuration to your Claude Desktop config file (see [Configuration](#configuration)). After restarting Claude Desktop, the tools will be available for Claude to use when discussing your .NET tests.

### Does this support remote test execution?

Not directly. The server runs `dotnet test` on the local machine specified by `WORKING_DIRECTORY`. For remote execution, you would need to configure the server to run on the remote machine and connect to it via your MCP client.

### What's the performance overhead?

Minimal. AOT compilation ensures fast server startup. Test execution time is the same as running `dotnet test` directly since that's exactly what the server does under the hood.

### Can I run multiple test executions in parallel?

The server supports scoped requests, so multiple tool invocations can be handled concurrently. However, `dotnet test` itself may have concurrency limitations depending on your test framework and project configuration.

## License

MIT License - see [LICENSE](LICENSE) file for details.

Copyright (c) 2026 Jonas Ha

## Acknowledgments

- Built with [ModelContextProtocol C# SDK](https://github.com/modelcontextprotocol/csharp-sdk)
- Uses [CTRF](https://ctrf.io/) (Common Test Result Format) for test results
- Powered by .NET 10.0 and Microsoft.Extensions.Hosting
- Inspired by the need for AI-assisted test exploration and execution

---

**Links & Resources**

- **Model Context Protocol**: https://modelcontextprotocol.io/
- **MCP Inspector**: https://github.com/modelcontextprotocol/inspector
- **Claude Desktop**: https://claude.ai/download
- **CTRF Specification**: https://ctrf.io/
- **GitHub Repository**: https://github.com/jonasha/dotnet-test-mcp
