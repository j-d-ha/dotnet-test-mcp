using DotnetTest.Mcp.Tools;
using Xunit;

namespace DotnetTest.Mcp.Tests;

public sealed class TestCommandBuilderTests
{
    [Fact]
    public void BuildSingleTestRun_ForTUnit_UsesMicrosoftTestingPlatformTreeNodeFilter()
    {
        var arguments = TestCommandBuilder.BuildSingleTestRun(
            TestRunnerDialect.TUnit,
            "Reaparr.Application.UnitTests.AddOrUpdatePlexServerCommandUnitTests.ShouldAddServer",
            "tests/Reaparr.Application.UnitTests/Reaparr.Application.UnitTests.csproj");

        string[] expected =
        [
            "run",
            "--project",
            "tests/Reaparr.Application.UnitTests/Reaparr.Application.UnitTests.csproj",
            "--",
            "--no-ansi",
            "--disable-logo",
            "--treenode-filter",
            "/*/*/AddOrUpdatePlexServerCommandUnitTests/ShouldAddServer",
        ];

        Assert.Equal(expected, arguments);
        Assert.DoesNotContain("--filter-method", arguments);
    }

    [Fact]
    public void BuildClassRun_ForTUnit_MapsQualifiedClassNameToMicrosoftTestingPlatformTreeNodeFilter()
    {
        var arguments = TestCommandBuilder.BuildClassRun(
            TestRunnerDialect.TUnit,
            "Reaparr.Application.UnitTests.AddOrUpdatePlexServerCommandUnitTests",
            "tests/Reaparr.Application.UnitTests/Reaparr.Application.UnitTests.csproj");

        Assert.Equal("--treenode-filter", arguments[^2]);
        Assert.Equal("/*/*/AddOrUpdatePlexServerCommandUnitTests/*", arguments[^1]);
        Assert.DoesNotContain("--filter-class", arguments);
    }

    [Fact]
    public void BuildProjectRun_ForTUnit_UsesDotnetRunProjectWithRunnerArgumentsAfterSeparator()
    {
        var arguments = TestCommandBuilder.BuildProjectRun(
            TestRunnerDialect.TUnit,
            "tests/Reaparr.Application.UnitTests/Reaparr.Application.UnitTests.csproj");

        string[] expected =
        [
            "run",
            "--project",
            "tests/Reaparr.Application.UnitTests/Reaparr.Application.UnitTests.csproj",
            "--",
            "--no-ansi",
            "--disable-logo",
        ];

        Assert.Equal(expected, arguments);
    }

    [Fact]
    public void BuildProjectRun_ForVSTest_UsesProjectAsPositionalArgument()
    {
        var arguments = TestCommandBuilder.BuildProjectRun(
            TestRunnerDialect.VSTest,
            "tests/DotnetTest.Mcp.Tests/DotnetTest.Mcp.Tests.csproj");

        string[] expected =
        [
            "test",
            "tests/DotnetTest.Mcp.Tests/DotnetTest.Mcp.Tests.csproj",
        ];

        Assert.Equal(expected, arguments);
        Assert.DoesNotContain("--project", arguments);
    }

    [Fact]
    public void BuildSingleTestRun_ForVSTest_UsesProjectAsPositionalArgument()
    {
        var arguments = TestCommandBuilder.BuildSingleTestRun(
            TestRunnerDialect.VSTest,
            "DotnetTest.Mcp.Tests.TestCommandBuilderTests.BuildSingleTestRun_ForVSTest_UsesProjectAsPositionalArgument",
            "tests/DotnetTest.Mcp.Tests/DotnetTest.Mcp.Tests.csproj");

        string[] expected =
        [
            "test",
            "tests/DotnetTest.Mcp.Tests/DotnetTest.Mcp.Tests.csproj",
            "--filter-method",
            "DotnetTest.Mcp.Tests.TestCommandBuilderTests.BuildSingleTestRun_ForVSTest_UsesProjectAsPositionalArgument",
        ];

        Assert.Equal(expected, arguments);
        Assert.DoesNotContain("--project", arguments);
    }

    [Fact]
    public void BuildClassRun_ForVSTest_UsesProjectAsPositionalArgument()
    {
        var arguments = TestCommandBuilder.BuildClassRun(
            TestRunnerDialect.VSTest,
            "DotnetTest.Mcp.Tests.TestCommandBuilderTests",
            "tests/DotnetTest.Mcp.Tests/DotnetTest.Mcp.Tests.csproj");

        string[] expected =
        [
            "test",
            "tests/DotnetTest.Mcp.Tests/DotnetTest.Mcp.Tests.csproj",
            "--filter-class",
            "DotnetTest.Mcp.Tests.TestCommandBuilderTests",
        ];

        Assert.Equal(expected, arguments);
        Assert.DoesNotContain("--project", arguments);
    }

    [Fact]
    public void CreateRunOptions_UsesConfiguredTimeoutAndPreservesRawOutputByDefault()
    {
        var options = new McpOptions
        {
            TestRunTimeoutSeconds = 180,
        };

        var runOptions = CtrfTestRun.CreateRunOptions(options);

        Assert.Equal(TimeSpan.FromSeconds(180), runOptions.Timeout);
        Assert.Null(runOptions.MaxOutputChars);
    }

}
