using System.ComponentModel;

namespace DotnetTest.Mcp.Models;

[Description("Per-project attempt summary for locating a test.")]
public sealed record ProjectAttempt(
    [property: Description("Project path that was probed.")] string ProjectPath,
    [property: Description("Outcome for this attempt.")] TestOutcome Outcome,
    [property: Description("Process exit code from dotnet test for this project.")] int ExitCode,
    [property: Description("Error kind for this attempt when Outcome is Error.")]
    ErrorKind? ErrorKind,
    [property: Description("Short summary for this attempt.")] string Summary);